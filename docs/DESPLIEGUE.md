# Despliegue en Azure

Cómo poner Rumbo en producción, paso a paso. Está escrito para ejecutarse de arriba abajo la
primera vez; después solo hará falta la sección 9.

> **Nota sobre los nombres.** Se usa `rumbo-` como prefijo y la región `eastus2`, que es la más
> cercana a República Dominicana con todos los servicios disponibles. Los nombres de Key Vault y
> de la cuenta de almacenamiento deben ser **únicos en todo Azure**, así que llevan un sufijo
> que tendrás que cambiar por otro. Si cambias algún nombre, cámbialo también en
> `appsettings.Production.json` y en `.github/workflows/backend-deploy.yml`.

## 0. Antes de empezar

Necesitas una suscripción de Azure y la CLI instalada:

```bash
az login
az account set --subscription "<id-de-tu-suscripcion>"
az account show --output table
```

Variables que se usan en todo el documento. Ejecuta esto una vez en la misma sesión de terminal:

```bash
GRUPO=rumbo-rg
REGION=eastus2
APP=rumbo-api
PLAN=rumbo-plan
SQL_SERVIDOR=rumbo-sql-0001        # cambia el sufijo
SQL_BASE=Rumbo
ALMACEN=rumboclaves0001            # cambia el sufijo, solo minusculas y numeros
BOVEDA=rumbo-secretos-0001         # cambia el sufijo
INSIGHTS=rumbo-insights
```

## 1. Grupo de recursos

Todo junto en un grupo: se borra de una vez si hay que empezar de cero, y la factura se lee
agrupada.

```bash
az group create --name $GRUPO --location $REGION
```

## 2. Azure SQL

```bash
# Servidor logico. La contrasena de administrador se usa UNA vez y despues no hace falta:
# la aplicacion entra con identidad administrada, no con usuario y contrasena.
az sql server create \
  --name $SQL_SERVIDOR \
  --resource-group $GRUPO \
  --location $REGION \
  --admin-user rumboadmin \
  --admin-password "<una-contrasena-larga-y-aleatoria>"

# Base de datos. Basic es suficiente para un hogar: 2 GB y 5 DTU.
az sql db create \
  --resource-group $GRUPO \
  --server $SQL_SERVIDOR \
  --name $SQL_BASE \
  --service-objective Basic \
  --backup-storage-redundancy Local

# Permitir que los servicios de Azure se conecten (App Service entre ellos).
az sql server firewall-rule create \
  --resource-group $GRUPO \
  --server $SQL_SERVIDOR \
  --name PermitirServiciosAzure \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Tu IP actual, para poder aplicar la migracion desde esta maquina.
MI_IP=$(curl -s https://api.ipify.org)
az sql server firewall-rule create \
  --resource-group $GRUPO \
  --server $SQL_SERVIDOR \
  --name MiMaquina \
  --start-ip-address $MI_IP \
  --end-ip-address $MI_IP
```

> **Por qué Basic y no Serverless.** Serverless se pausa cuando no hay actividad y la primera
> petición después tarda entre 30 y 60 segundos. En una aplicación que se abre una vez al día
> desde el móvil, eso significa que **casi todas** las veces se abre lenta. Basic cuesta unos
> 5 USD al mes y no se pausa nunca. Es el riesgo R10 del plan.

## 3. Cuenta de almacenamiento para las claves de cifrado

**Esta es la parte crítica de toda la fase.** Sin ella, las contraseñas SMTP guardadas dejan de
poder descifrarse en cuanto App Service reinicia, y los enlaces de «olvidé mi clave» caducan
solos.

```bash
az storage account create \
  --name $ALMACEN \
  --resource-group $GRUPO \
  --location $REGION \
  --sku Standard_LRS \
  --kind StorageV2 \
  --min-tls-version TLS1_2 \
  --allow-blob-public-access false

az storage container create \
  --name dataprotection \
  --account-name $ALMACEN \
  --auth-mode login
```

## 4. Key Vault

```bash
az keyvault create \
  --name $BOVEDA \
  --resource-group $GRUPO \
  --location $REGION \
  --enable-rbac-authorization true \
  --retention-days 90 \
  --enable-purge-protection true
```

`--enable-purge-protection` impide borrar la bóveda de forma definitiva durante 90 días. Es
molesto mientras se practica y es exactamente lo que quieres el día que alguien se equivoca de
comando.

Crea la clave con la que se cifrará el fichero de claves de Data Protection:

```bash
az keyvault key create \
  --vault-name $BOVEDA \
  --name clave-proteccion-datos \
  --kty RSA \
  --size 2048
```

## 5. Application Insights

```bash
az monitor app-insights component create \
  --app $INSIGHTS \
  --location $REGION \
  --resource-group $GRUPO \
  --application-type web \
  --retention-time 90
```

## 6. App Service

```bash
# B1: el plan mas barato que permite Always On. F1 (gratuito) NO lo permite, y sin Always On
# la aplicacion se descarga tras 20 minutos sin trafico: la siguiente peticion tarda medio
# minuto en responder. Es el riesgo R11 del plan.
az appservice plan create \
  --name $PLAN \
  --resource-group $GRUPO \
  --location $REGION \
  --sku B1 \
  --is-linux false

az webapp create \
  --name $APP \
  --resource-group $GRUPO \
  --plan $PLAN \
  --runtime "dotnet:10"

az webapp config set \
  --name $APP \
  --resource-group $GRUPO \
  --always-on true \
  --http20-enabled true \
  --min-tls-version 1.2 \
  --ftps-state Disabled \
  --health-check-path "/salud"

# HTTPS obligatorio. Sin esto, la API acepta HTTP y el token viaja en claro hasta que la
# redireccion actua, que ya es tarde.
az webapp update \
  --name $APP \
  --resource-group $GRUPO \
  --https-only true

# Ranura de preparacion, para desplegar sin cortar el servicio.
az webapp deployment slot create \
  --name $APP \
  --resource-group $GRUPO \
  --slot preparacion
```

> **`--health-check-path "/salud"` y no `/salud/preparada`.** Son dos cosas distintas.
> `/salud` dice si el proceso vive; `/salud/preparada` además consulta la base de datos. Si
> Azure usara la segunda, una caída momentánea de SQL provocaría que reiniciara la aplicación,
> que no arregla nada y encima tira las sesiones. El despliegue sí usa `/salud/preparada`,
> porque ahí lo que se pregunta es otra cosa: si la instancia nueva puede recibir tráfico.

## 7. Identidad administrada y permisos

Aquí es donde desaparecen los secretos de la configuración.

```bash
# Se activa la identidad en la aplicacion y en su ranura.
az webapp identity assign --name $APP --resource-group $GRUPO
az webapp identity assign --name $APP --resource-group $GRUPO --slot preparacion

PRINCIPAL=$(az webapp identity show --name $APP --resource-group $GRUPO --query principalId -o tsv)
PRINCIPAL_PREP=$(az webapp identity show --name $APP --resource-group $GRUPO --slot preparacion --query principalId -o tsv)
SUSCRIPCION=$(az account show --query id -o tsv)
```

Permisos sobre Key Vault, el almacenamiento y la clave de cifrado. Se conceden **los mínimos**:
leer secretos, no escribirlos; envolver y desenvolver con la clave, no exportarla.

```bash
for ID in $PRINCIPAL $PRINCIPAL_PREP; do
  # Leer secretos de Key Vault.
  az role assignment create \
    --assignee $ID \
    --role "Key Vault Secrets User" \
    --scope "/subscriptions/$SUSCRIPCION/resourceGroups/$GRUPO/providers/Microsoft.KeyVault/vaults/$BOVEDA"

  # Usar la clave de cifrado (envolver/desenvolver), sin poder leerla ni exportarla.
  az role assignment create \
    --assignee $ID \
    --role "Key Vault Crypto User" \
    --scope "/subscriptions/$SUSCRIPCION/resourceGroups/$GRUPO/providers/Microsoft.KeyVault/vaults/$BOVEDA"

  # Leer y escribir el fichero de claves en el contenedor.
  az role assignment create \
    --assignee $ID \
    --role "Storage Blob Data Contributor" \
    --scope "/subscriptions/$SUSCRIPCION/resourceGroups/$GRUPO/providers/Microsoft.Storage/storageAccounts/$ALMACEN/blobServices/default/containers/dataprotection"
done
```

Acceso a Azure SQL sin contraseña:

```bash
# Te pones como administrador de Entra ID en el servidor SQL.
MI_CORREO=$(az ad signed-in-user show --query userPrincipalName -o tsv)
MI_OBJETO=$(az ad signed-in-user show --query id -o tsv)

az sql server ad-admin create \
  --resource-group $GRUPO \
  --server-name $SQL_SERVIDOR \
  --display-name "$MI_CORREO" \
  --object-id $MI_OBJETO
```

Y después, conectado a la base `Rumbo` (por ejemplo desde Azure Data Studio o el editor de
consultas del portal), se crea el usuario de la aplicación:

```sql
-- El nombre del usuario es el de la aplicacion en App Service.
CREATE USER [rumbo-api] FROM EXTERNAL PROVIDER;
CREATE USER [rumbo-api/slots/preparacion] FROM EXTERNAL PROVIDER;

-- Minimo privilegio: leer y escribir datos, nada de cambiar el esquema. Las migraciones se
-- aplican aparte, con una identidad que si puede.
ALTER ROLE db_datareader ADD MEMBER [rumbo-api];
ALTER ROLE db_datawriter ADD MEMBER [rumbo-api];
ALTER ROLE db_datareader ADD MEMBER [rumbo-api/slots/preparacion];
ALTER ROLE db_datawriter ADD MEMBER [rumbo-api/slots/preparacion];
```

> **Por qué la aplicación no puede cambiar el esquema.** Si pudiera, una vulnerabilidad de
> inyección —hoy no hay ninguna, pero mañana existe código nuevo— podría borrar tablas en vez
> de solo leer filas. Las migraciones las aplica una persona, con un script revisado.

## 8. Secretos en Key Vault

Los nombres usan **dos guiones** donde la configuración usa dos puntos, porque Key Vault no
admite `:`. La aplicación lee `Jwt--ClaveFirma` como `Jwt:ClaveFirma`.

```bash
# Clave de firma de los tokens. 64 bytes aleatorios en base64.
CLAVE_JWT=$(openssl rand -base64 64 | tr -d '\n')

az keyvault secret set --vault-name $BOVEDA --name "Jwt--ClaveFirma" --value "$CLAVE_JWT"

# Cadena de conexion SIN contrasena: entra con la identidad administrada.
az keyvault secret set \
  --vault-name $BOVEDA \
  --name "ConnectionStrings--Rumbo" \
  --value "Server=tcp:$SQL_SERVIDOR.database.windows.net,1433;Database=$SQL_BASE;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30"

# Primer administrador de plataforma. Sin el, nadie puede emitir la primera invitacion y el
# sistema queda inaccesible: Rumbo no tiene registro publico.
az keyvault secret set --vault-name $BOVEDA --name "Rumbo--AdministradorInicial--Correo" --value "tu-correo@ejemplo.com"
az keyvault secret set --vault-name $BOVEDA --name "Rumbo--AdministradorInicial--Clave" --value "<una-contrasena-larga>"

# Correo de plataforma. Usa una CLAVE DE APLICACION del proveedor, nunca la contrasena
# principal de la cuenta: si se filtra, se revoca sola sin perder el correo.
az keyvault secret set --vault-name $BOVEDA --name "Correo--Host" --value "smtp.tuproveedor.com"
az keyvault secret set --vault-name $BOVEDA --name "Correo--Usuario" --value "no-responder@tudominio.com"
az keyvault secret set --vault-name $BOVEDA --name "Correo--Clave" --value "<clave-de-aplicacion>"
az keyvault secret set --vault-name $BOVEDA --name "Correo--RemitenteCorreo" --value "no-responder@tudominio.com"
```

Cambia la contraseña del administrador inicial en cuanto entres por primera vez.

### Configuración de la aplicación

Estos valores **no son secretos** —son direcciones de recursos— y van en la configuración de la
aplicación, no en la bóveda:

```bash
CADENA_INSIGHTS=$(az monitor app-insights component show \
  --app $INSIGHTS --resource-group $GRUPO --query connectionString -o tsv)

for RANURA in "" "--slot preparacion"; do
  az webapp config appsettings set \
    --name $APP --resource-group $GRUPO $RANURA \
    --settings \
      ASPNETCORE_ENVIRONMENT=Production \
      Azure__UriKeyVault="https://$BOVEDA.vault.azure.net/" \
      Azure__UriContenedorClaves="https://$ALMACEN.blob.core.windows.net/dataprotection" \
      Azure__UriClaveCifrado="https://$BOVEDA.vault.azure.net/keys/clave-proteccion-datos" \
      ApplicationInsights__ConnectionString="$CADENA_INSIGHTS"
done
```

> **Dos guiones bajos, no dos puntos.** En la configuración de App Service, `__` es el separador
> de secciones. `Azure__UriKeyVault` llega a la aplicación como `Azure:UriKeyVault`.

Marca los ajustes que **no** deben intercambiarse con la ranura, si algún día quieres que la de
preparación apunte a otra base:

```bash
az webapp config appsettings set \
  --name $APP --resource-group $GRUPO \
  --slot-settings ASPNETCORE_ENVIRONMENT=Production
```

## 9. Aplicar las migraciones

Se hace **aparte del despliegue** y con un script revisado. Migrar al arrancar la aplicación
parece cómodo, pero con varias instancias dos procesos pueden migrar a la vez, y un despliegue
fallido deja la base a medias sin que nadie lo haya aprobado.

```bash
dotnet ef migrations script \
  --idempotent \
  --project src/Rumbo.Infraestructura \
  --startup-project src/Rumbo.Api \
  --output migracion.sql
```

`--idempotent` genera un script que comprueba cada migración antes de aplicarla: se puede
ejecutar dos veces sin romper nada.

**Léelo antes de ejecutarlo.** Busca `DROP` y `ALTER COLUMN`: son las dos instrucciones que
pierden datos. Después aplícalo con Azure Data Studio, `sqlcmd` o el editor de consultas del
portal, conectado con tu cuenta de Entra ID.

## 10. Primer despliegue

Desde tu máquina, para la primera vez:

```bash
dotnet publish src/Rumbo.Api -c Release -o ./publicacion
cd publicacion && zip -r ../rumbo.zip . && cd ..

az webapp deploy \
  --resource-group $GRUPO \
  --name $APP \
  --src-path rumbo.zip \
  --type zip
```

Comprueba que responde:

```bash
curl https://$APP.azurewebsites.net/salud
curl https://$APP.azurewebsites.net/salud/preparada
```

El segundo debe devolver `{"estado":"Preparada","baseDeDatos":"Accesible"}`. Si devuelve 503, la
aplicación arrancó pero no llega a la base: revisa el cortafuegos de SQL y el usuario externo de
la sección 7.

## 11. Despliegues siguientes, desde GitHub Actions

El workflow `.github/workflows/backend-deploy.yml` despliega a la ranura de preparación,
comprueba que responde y solo entonces la intercambia con producción. Si algo va mal, el
intercambio se deshace en segundos:

```bash
az webapp deployment slot swap \
  --resource-group $GRUPO --name $APP \
  --slot preparacion --target-slot production
```

(El mismo comando vuelve a intercambiar, dejando la versión anterior en producción.)

### Federación de identidades: GitHub sin secretos de Azure

En lugar de guardar una credencial de Azure en GitHub, se configura una confianza: GitHub pide
un token de corta duración en cada ejecución.

```bash
# Aplicacion de Entra ID que representa al workflow.
az ad app create --display-name "rumbo-despliegue"
APP_ID=$(az ad app list --display-name "rumbo-despliegue" --query "[0].appId" -o tsv)
az ad sp create --id $APP_ID
SP_ID=$(az ad sp show --id $APP_ID --query id -o tsv)

# Permiso para desplegar, limitado al grupo de recursos y nada mas.
az role assignment create \
  --assignee $APP_ID \
  --role "Contributor" \
  --scope "/subscriptions/$SUSCRIPCION/resourceGroups/$GRUPO"

# La confianza: solo el entorno «produccion» de TU repositorio puede pedir el token.
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "github-produccion",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:StalynPersonal/rumbo:environment:produccion",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

En GitHub → *Settings* → *Secrets and variables* → *Actions*, añade:

| Secreto | De dónde sale |
|---|---|
| `AZURE_CLIENT_ID` | El `$APP_ID` de arriba |
| `AZURE_TENANT_ID` | `az account show --query tenantId -o tsv` |
| `AZURE_SUBSCRIPTION_ID` | `az account show --query id -o tsv` |

Y en *Settings* → *Environments*, crea el entorno **`produccion`**. Puedes exigir tu aprobación
manual antes de cada despliegue: una casilla más entre un `git push` y el dinero de tu familia.

## 12. Correo: SPF y DKIM

Si el remitente usa dominio propio, sin esto los correos de invitación acaban en spam y nadie
puede entrar (riesgo R9 del plan). Añade en el DNS del dominio:

- **SPF**: un registro `TXT` autorizando al servidor SMTP, por ejemplo
  `v=spf1 include:_spf.tuproveedor.com ~all`.
- **DKIM**: el registro `CNAME` o `TXT` que te dé tu proveedor de correo.
- **DMARC**: `TXT` en `_dmarc`, empezando por `v=DMARC1; p=none; rua=mailto:tu@dominio.com`
  para observar antes de endurecer.

Mientras tanto, el administrador de plataforma puede copiar el enlace de invitación desde el
panel y enviarlo por otro medio.

## 13. Coste estimado

| Recurso | Nivel | USD/mes aproximado |
|---|---|---|
| App Service | B1 con Always On | ~13 |
| Azure SQL | Basic, 2 GB | ~5 |
| Storage | Standard_LRS, unos pocos KB | <1 |
| Key Vault | Estándar, pocas operaciones | <1 |
| Application Insights | 90 días, volumen bajo | <1 (dentro del nivel gratuito) |
| **Total** | | **~20** |

Ojo con Application Insights: se factura por volumen ingerido. Con el nivel de registro de
`appsettings.Production.json` (`Warning` por defecto) no debería pasar del nivel gratuito, pero
si algún día subes todo a `Information` la factura lo nota.

## 14. Qué revisar después del despliegue

- [ ] `GET /salud` devuelve 200.
- [ ] `GET /salud/preparada` devuelve 200 con `baseDeDatos: "Accesible"`.
- [ ] `GET /swagger` devuelve **404**. Swagger solo se publica fuera de producción; si
      responde, el entorno no es `Production`.
- [ ] Una respuesta de error **no** trae traza de pila ni `mensajeTecnico`.
- [ ] Las cabeceras de seguridad están: `curl -I https://$APP.azurewebsites.net/salud`.
- [ ] Seis intentos seguidos de inicio de sesión devuelven `429`.
- [ ] Puedes iniciar sesión con el administrador inicial, y **cambias su contraseña**.
- [ ] Llega el correo de una invitación de prueba.
- [ ] **Reinicia la aplicación** (`az webapp restart`) y comprueba que sigues pudiendo leer una
      configuración SMTP guardada. Es la prueba de que las claves de Data Protection
      sobreviven, y es lo único de esta lista que no se detecta hasta que ya duele.

## 15. Copias de seguridad

Azure SQL hace copias automáticas. Con el nivel Basic se conservan **7 días** y permiten
restaurar a cualquier punto en el tiempo dentro de esa ventana:

```bash
az sql db restore \
  --resource-group $GRUPO \
  --server $SQL_SERVIDOR \
  --name $SQL_BASE \
  --dest-name Rumbo-Restaurada \
  --time "2026-09-20T14:30:00"
```

Restaura siempre a una base **nueva**, nunca encima de la que está en uso.

Siete días es poco para un error que se descubre tarde. Si quieres más, exporta un `.bacpac`
periódicamente:

```bash
az sql db export \
  --resource-group $GRUPO --server $SQL_SERVIDOR --name $SQL_BASE \
  --admin-user rumboadmin --admin-password "<contrasena>" \
  --storage-key-type StorageAccessKey \
  --storage-key "<clave>" \
  --storage-uri "https://$ALMACEN.blob.core.windows.net/respaldos/rumbo-$(date +%Y%m%d).bacpac"
```

> **No olvides el contenedor `dataprotection`.** La base de datos sin las claves de Data
> Protection es una base de datos con las contraseñas SMTP ilegibles. Si algún día restauras
> desde cero, necesitas los dos.

## 16. Si algo va mal

| Síntoma | Causa probable |
|---|---|
| 500 al arrancar | Falta un secreto en Key Vault, o la identidad no tiene permiso para leerlo. Mira el registro: `az webapp log tail --name $APP --resource-group $GRUPO` |
| `/salud` responde y `/salud/preparada` devuelve 503 | La aplicación vive pero no llega a SQL: cortafuegos, o falta el usuario externo de la sección 7 |
| «Falta la clave de firma de los tokens» al arrancar | `Jwt--ClaveFirma` no está en la bóveda o tiene menos de 32 caracteres. La aplicación falla al arrancar a propósito: una clave corta haría los tokens falsificables |
| Los enlaces de «olvidé mi clave» dejan de valer tras un reinicio | Las claves de Data Protection no se están persistiendo. Revisa `Azure__UriContenedorClaves` y el permiso sobre el contenedor |
| Una contraseña SMTP guardada ya no se puede descifrar | Lo mismo que lo anterior, y ya ocurrió. Hay que volver a introducirla |
| La primera petición del día tarda medio minuto | Always On desactivado, o SQL en Serverless con auto-pausa |
| Los correos no llegan | SPF/DKIM sin configurar, o el proveedor bloquea el envío desde Azure |
