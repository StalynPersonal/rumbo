# Seguridad de Rumbo

Rumbo guarda las finanzas de un hogar. Todo dato de este sistema es sensible, y el orden de
prioridades del proyecto lo refleja: **seguridad primero, aislamiento entre espacios después,
integridad financiera a continuación**, y solo entonces el resto.

## 1. Autenticación

### Contraseñas

Las gestiona **ASP.NET Core Identity**, no código propio. El hash es PBKDF2 con sal por
usuario. Escribir criptografía a mano habría sido asumir un riesgo sin ninguna ventaja.

| Regla | Valor | Por qué |
|---|---|---|
| Longitud mínima | 12 caracteres | La longitud protege más que la complejidad: una frase larga resiste mejor que `Abc123!` y es más fácil de recordar |
| Mayúscula, minúscula y número | Obligatorios | |
| Símbolo | **No** obligatorio | Exigirlo empuja hacia contraseñas cortas y rebuscadas que acaban anotadas en un papel |
| Bloqueo | 5 intentos → 15 minutos | Hace inviable probar contraseñas por fuerza bruta |

### Tokens

Dos tokens con propósitos distintos:

| Token | Duración | Revocable | Dónde vive |
|---|---|---|---|
| **Acceso** (JWT) | 15 minutos | No | En memoria de la app |
| **Renovación** | 30 días | Sí | SecureStorage; en la base de datos solo su hash |

Un JWT no se puede revocar: una vez firmado, vale hasta que caduca. De ahí que dure poco. El
token de renovación sí se puede revocar porque está en la base de datos, y por eso puede
durar mucho más sin asumir el mismo riesgo.

### Rotación y detección de robo

Cada token de renovación **sirve una sola vez**. Al usarlo se revoca y se emite otro.

Si llega un token ya revocado, significa que existen **dos copias circulando**: alguien lo
interceptó. Como no hay forma de saber cuál de las dos partes es la legítima, el sistema
**revoca la cadena entera** y ambas deben volver a iniciar sesión. Es molesto, y es lo
correcto: la alternativa es dejar al atacante dentro.

### Qué no se revela

| Situación | Respuesta | Por qué |
|---|---|---|
| Correo no registrado | `401` «El correo o la contraseña no son correctos» | Si dijera «ese correo no existe», probar direcciones permitiría averiguar quién usa Rumbo |
| Contraseña incorrecta | **El mismo** `401` y **el mismo** mensaje | |
| «Olvidé mi clave» de un correo inexistente | `204`, igual que si existiera | Mismo motivo |
| Recurso de otro espacio | `404`, no `403` | Un `403` confirmaría que el registro existe |

## 2. Alta cerrada por invitación

**No existe registro público.** Toda alta nace de una invitación:

```
AdministradorPlataforma
  └─ invita a un futuro propietario (por correo)
        └─ al aceptar, se crea SU espacio y queda como Propietario
              └─ el propietario invita a su pareja o familia
                    └─ acceso solo a ESE espacio, con el rol indicado
```

Protecciones de los códigos:

- 256 bits de aleatoriedad criptográfica (`RandomNumberGenerator`), no un `Guid`: los `Guid`
  no están pensados para ser impredecibles.
- En la base de datos solo se guarda el **hash SHA-256**. Quien leyera la tabla no podría
  usar las invitaciones pendientes.
- **Ligado a un correo concreto**: reenviar el código a otra persona no le sirve.
- **Un solo uso** y caducidad de 7 días.
- Máximo 20 pendientes por espacio, para que una cuenta comprometida no pueda usarse para
  enviar correo masivo desde nuestro servidor SMTP.

## 3. Aislamiento entre espacios

Cinco capas independientes. El diseño asume que cualquiera puede fallar.

| # | Capa | Qué protege |
|---|---|---|
| 1 | `IContextoEspacio` | El espacio sale del **token firmado**, nunca del cuerpo ni de una cabecera |
| 2 | Filtros globales de EF Core | Toda consulta filtra por espacio, aunque el programador lo olvide |
| 3 | `InterceptorEspacio` | Asigna el espacio al insertar y **aborta** cualquier escritura hacia otro |
| 4 | Validación de referencias | Una cuenta o categoría ajena da **404**, no 403 |
| 5 | Autorización por permiso | `[RequierePermiso(...)]` según el rol dentro de ese espacio |

Además, el middleware **verifica la membresía contra la base de datos en cada petición**, con
una caché de 30 segundos. El token es de fiar porque está firmado, pero refleja la situación
del momento en que se emitió: si se expulsa a alguien del hogar, su token seguiría diciendo
que pertenece a él durante quince minutos.

**La caché se invalida explícitamente** cuando cambia el rol o el estado de una membresía, así
que suspender o expulsar a alguien tiene efecto en su **siguiente petición**, no medio minuto
después. Lo verifica `SuspenderAUnMiembroLeCortaElAccesoDeInmediato`.

*Limitación conocida:* la caché es de memoria, por instancia. Con varias instancias en Azure,
la invalidación solo alcanza a la que atendió la petición, y en las demás el cambio tardaría
los 30 segundos. Con una sola instancia, que es el escenario previsto, no aplica. Si se escala
horizontalmente habrá que pasar a una caché distribuida.

### El administrador de plataforma

`AdministradorPlataforma` es un rol de Identity, **no** un rol de espacio.

- **Es invisible en cada espacio de forma estructural**, no por un filtro: no tiene ninguna
  fila en `MembresiasEspacio`, y el listado de miembros se construye desde esa tabla. Un
  filtro se puede olvidar en un endpoint nuevo; la ausencia de fila no.
- **No puede leer datos financieros.** Gestiona espacios, usuarios e invitaciones. La
  autorización de los controladores de datos exige un rol *dentro* del espacio, que él no
  tiene, así que recibe `403` igual que cualquier desconocido.

## 4. Autorización por permisos

Los controladores declaran **qué permiso** hace falta, no qué rol:

```csharp
[RequierePermiso(Permisos.Movimientos.Escribir)]
```

`MapaPermisos` es el único sitio donde se decide quién puede qué. Añadir mañana un rol nuevo
—un «Observador» que solo consulta, o un perfil para un adolescente de la casa— es tocar un
solo fichero. Con `[Authorize(Roles = "...")]` habría que repasar todos los controladores, y
bastaría olvidar uno para abrir un agujero.

| Rol | Puede |
|---|---|
| `Miembro` | Registrar y consultar movimientos; ver cuentas, presupuestos, metas y viajes |
| `Administrador` | Lo anterior + gestionar cuentas, presupuestos, metas, miembros e invitaciones |
| `Propietario` | Lo anterior + eliminar el espacio |

## 5. Gestión de secretos

**Ningún secreto está en el repositorio.**

| Secreto | Desarrollo | Producción |
|---|---|---|
| Clave de firma JWT | `dotnet user-secrets` | Azure Key Vault |
| Contraseña SMTP | `dotnet user-secrets` | Azure Key Vault |
| Cadena de conexión | Autenticación de Windows, sin contraseña | Identidad administrada, sin contraseña |
| Contraseña del administrador inicial | `dotnet user-secrets` | Azure Key Vault |

La aplicación **no arranca** si falta la clave de firma o tiene menos de 32 caracteres. Se
falla al arrancar y no en el primer inicio de sesión: una clave débil haría los tokens
falsificables, y eso no puede depender de que alguien lo note probando.

```bash
cd src/Rumbo.Api
dotnet user-secrets set "Jwt:ClaveFirma" "<48 bytes aleatorios en base64>"
dotnet user-secrets set "Rumbo:AdministradorInicial:Correo" "tu@correo.com"
dotnet user-secrets set "Rumbo:AdministradorInicial:Clave" "<contraseña fuerte>"
```

Para SMTP, usa siempre una **contraseña de aplicación** dedicada, nunca la principal de la
cuenta de correo: si se filtra, se revoca sin tocar nada más.

## 6. Servidores de correo

Hay **dos niveles**, y ambos se configuran desde la aplicación:

| Nivel | Quién lo configura | Qué envía |
|---|---|---|
| **Plataforma** | `AdministradorPlataforma`, en `/api/v1/administracion/correo` | Invitación a un futuro propietario y restablecimiento de contraseña |
| **Espacio** | El **propietario**, en `/api/v1/espacios/actual/correo` | Invitaciones a miembros y avisos de ese hogar |

**Por qué hacen falta los dos.** Hay dos correos que no tienen espacio del que sacar
credenciales: la invitación a un propietario se envía *antes* de que su espacio exista, y el
restablecimiento de contraseña pertenece a la persona, que puede estar en varios hogares.

Orden de preferencia al enviar: servidor del espacio → servidor de plataforma → `appsettings`
(solo para el arranque inicial, antes de que haya nada en la base de datos). Si un espacio no
tiene servidor propio, su correo sale por el de plataforma: es preferible que llegue desde una
dirección genérica a que no llegue.

### Protección de las credenciales

Aquí no se guarda una credencial del sistema, sino **la contraseña del correo personal de
alguien**. En texto plano, quien leyera la base de datos se llevaría una cuenta de correo
ajena, no solo información financiera.

| Medida | Detalle |
|---|---|
| Cifrado en reposo | Data Protection con un propósito aislado (`Rumbo.Correo.CredencialesSmtp.v1`) |
| Cifrado, no hash | A diferencia de las contraseñas de acceso, esta hay que **recuperarla** para autenticarse contra el servidor. Hashearla la inutilizaría |
| Nunca se devuelve | Ningún endpoint la expone, ni siquiera a quien la puso. Solo se informa de `ClaveConfigurada: true/false` |
| Guardar sin contraseña la conserva | Permite cambiar el puerto sin reescribirla, algo imposible si la API no la devuelve |
| Fuera de los registros | Se registra el destinatario, el asunto y de qué servidor salió; nunca el cuerpo ni la credencial |
| Permiso propio | `espacio.configurar_correo`, concedido **solo al Propietario**. Ni un Administrador del hogar puede tocarlo |

Lo verifica `LaContrasenaSmtpNuncaSeDevuelvePorLaApi`, que revisa el **JSON en crudo** y no el
objeto deserializado: si la contraseña se colara en un campo que el DTO no declara, el objeto
no la mostraría pero la respuesta sí la llevaría.

Se recomienda usar siempre una **contraseña de aplicación** dedicada, no la principal de la
cuenta: si se filtra, se revoca sin tocar nada más. El endpoint `POST .../correo/probar`
comprueba la conexión antes de confiar en ella, y distingue el fallo de autenticación —que casi
siempre significa haber usado la contraseña normal en vez de una de aplicación— del resto.

## 7. Registro de auditoría

`InterceptorAuditoria` escribe el historial automáticamente al guardar cambios. Se hace en un
interceptor y no en cada servicio a propósito: si dependiera de que alguien se acuerde de
registrar la acción, el historial tendría huecos justo en las operaciones menos habituales,
que son las que más interesa auditar.

**Nunca se registran** contraseñas, hashes de token ni códigos de invitación. La lista de
campos redactados está en `PropiedadesSensibles`.

## 8. Errores

Todos salen como `ProblemDetails` (RFC 9457) con un `traceId`.

En producción **no se expone** ninguna traza de pila ni el mensaje interno de una excepción
inesperada: revelarían rutas de ficheros, nombres de tablas y versiones de librerías. Se
devuelve un mensaje genérico y el detalle queda solo en los registros del servidor.

Las excepciones de dominio sí muestran su mensaje, porque están escritas para que las lea la
persona: «El código de invitación ha caducado».

## 9. Límite de peticiones

Implementado en la Fase 9. Un **limitador global que decide por ruta**, no políticas sueltas
endpoint a endpoint.

| Ámbito | Cupo | Por qué |
|---|---|---|
| `/api/v1/autenticacion/*` | 5 por minuto | Frena probar contraseñas en serie y el abuso de «olvidé mi clave» para bombardear a alguien con correos |
| Resto de `/api/*` | 100 por minuto | Uso normal holgado; corta el raspado masivo |
| `/salud` y Swagger | Sin límite | Azure consulta `/salud` cada pocos segundos; si se cortara, la plataforma creería que la API está caída y la reiniciaría |

El cupo se reparte **por usuario** cuando hay sesión y **por dirección IP** cuando no la hay. El
identificador de usuario sale del token ya validado, nunca de una cabecera: si se tomara de algo
que el cliente controla, bastaría con cambiarlo en cada petición para saltarse el límite.

El rechazo es `429` con `Retry-After` y cuerpo `ProblemDetails`, igual que cualquier otro error
de la API. Sin esa cabecera, un cliente honesto solo puede reintentar a ciegas, que es justo lo
que empeora la situación.

> **Por qué un limitador global y no atributos.** La primera versión usaba
> `[EnableRateLimiting]` en el controlador de autenticación más `RequireRateLimiting` en bloque
> sobre todos los controladores. El segundo **pisaba** al primero y el límite estricto se perdía
> sin que nada avisara. Lo detectó la prueba, no la revisión de código. Con un único limitador
> que mira la ruta, el reparto está en un sitio y se lee de un vistazo.

**Esto es una capa más, no la única.** Una dirección IP se cambia. Detrás siguen estando el
bloqueo de cuenta por intentos, las claves hasheadas y el registro cerrado por invitación.

### Límite de invitaciones

Dos topes, y hacen falta los dos:

- **20 invitaciones pendientes** por espacio: acota cuántas puertas quedan abiertas a la vez.
- **10 invitaciones por hora** por espacio: acota cuántos correos salen del servidor SMTP.

Sin el segundo, bastaría con anular las veinte pendientes y volver a crearlas en bucle para
mandar correo sin tope y arruinar la reputación del dominio remitente. Se cuentan **todas** las
creadas en la última hora, sea cual sea su estado: anular una invitación no deshace el correo
que ya salió.

## 10. Cabeceras de seguridad

| Cabecera | Valor | Qué evita |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | Que el navegador adivine el tipo y ejecute como HTML un JSON con texto elegido por un atacante |
| `X-Frame-Options` | `DENY` | Secuestro de clics |
| `Referrer-Policy` | `no-referrer` | Que una ruta como `/api/v1/metas/{id}` viaje a otro sitio |
| `Content-Security-Policy` | `default-src 'none'; frame-ancestors 'none'; base-uri 'none'` | Carga de cualquier recurso externo |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` | Acceso a dispositivos |
| `Cache-Control` | `no-store, no-cache, must-revalidate` | Copias de datos financieros en caché compartida o en disco |
| `Strict-Transport-Security` | 1 año, con subdominios | Que el navegador vuelva a intentar HTTP siquiera una vez |

Se quitan además `Server` y `X-Powered-By`. No es seguridad de verdad —nadie se detiene por no
saber la versión—, pero tampoco hay razón para regalar el dato.

Van **antes** del manejador de excepciones en la tubería, así que las llevan también las
respuestas de error. HSTS **no** se activa en desarrollo: se queda pegado en el navegador
durante un año y el certificado local no vale fuera de la máquina.

La CSP estricta se exceptúa en `/swagger` y `/openapi`, que son páginas de verdad y necesitan su
script. Relajar la política para toda la API con tal de que Swagger funcione sería pagar en
producción una comodidad de desarrollo.

## 11. Tamaño de las peticiones

El cuerpo máximo es **256 KB**. Ningún cuerpo legítimo de Rumbo se acerca: son movimientos y
presupuestos, no ficheros. Sin tope, una petición enorme obliga al servidor a reservar memoria
antes de poder rechazarla.

## 12. Revisión OWASP Top 10 (2021)

Revisión de la Fase 9. Se indica lo que está hecho y lo que **no**, sin maquillar.

| # | Riesgo | Estado | Detalle |
|---|---|---|---|
| **A01** | Control de acceso roto | ✅ | Cinco capas independientes (§3), permisos por acción (§4), 404 en lugar de 403 para no revelar existencia. Pruebas de aislamiento sobre el pipeline HTTP real |
| **A02** | Fallos criptográficos | ⚠️ | PBKDF2 de Identity, HTTPS obligatorio, HSTS, contraseñas SMTP cifradas con Data Protection, tokens de renovación guardados solo como hash. **Pendiente:** persistir las claves de Data Protection fuera de la máquina (Fase 10) |
| **A03** | Inyección | ✅ | EF Core parametriza todo. **Cero** `FromSql` o `ExecuteSql` en el código: verificado por búsqueda, no por confianza. Los enums llegan como texto y se traducen con `Enum.TryParse`, nunca se concatenan |
| **A04** | Diseño inseguro | ✅ | Registro cerrado por invitación; el administrador de plataforma no puede leer datos financieros; ninguna recomendación mueve dinero |
| **A05** | Configuración insegura | ✅ | Cabeceras (§10), Swagger solo fuera de producción, sin trazas de pila en producción, `TreatWarningsAsErrors`, secretos fuera del repositorio |
| **A06** | Componentes vulnerables | ✅ | `dotnet list package --vulnerable --include-transitive`: **ninguna vulnerabilidad conocida** en los ocho proyectos. Gestión central de paquetes: una sola versión por paquete |
| **A07** | Fallos de identificación | ✅ | Bloqueo por intentos, límite de peticiones (§9), tokens de 15 minutos, rotación con detección de reuso, mensajes indistinguibles en el login y en «olvidé mi clave» |
| **A08** | Fallos de integridad | ⚠️ | `rowversion` en cuentas y metas, borrado lógico, saldos movidos solo dentro de transacciones. **Pendiente:** firmar los artefactos del despliegue (Fase 10) |
| **A09** | Fallos de registro | ✅ | `RegistroAuditoria` escrito por interceptor, no a mano; `traceId` en cada error. **Verificado en esta fase:** ningún registro contiene contraseñas, tokens, códigos de invitación ni importes. Se registra el identificador del usuario, nunca el dato |
| **A10** | SSRF | ⚠️ → ✅ | **Encontrado en esta revisión.** El servidor SMTP de cada espacio lo elige su propietario y la API se conecta a donde le digan. Se acotó: solo puertos 25, 465, 587 y 2525, y se rechazan `localhost` y las direcciones privadas, incluido `169.254.169.254` (metadatos de la nube). **No se resuelve el nombre por DNS a propósito**: un nombre puede resolver a una dirección pública al guardarlo y a una interna al usarlo, así que comprobarlo daría una falsa sensación de seguridad a cambio de una llamada de red en cada guardado. Esto filtra lo evidente; el aislamiento de red de Azure hace el resto |

### Lo que esta revisión encontró

1. **SSRF por el SMTP configurable (A10).** El hallazgo real de la fase. El campo existía desde
   la Fase 3 y nadie lo había mirado con esta lente.
2. **El límite estricto de autenticación no se aplicaba**, porque el atributo del controlador
   quedaba pisado por la política aplicada en bloque a todos los controladores. Lo detectó la
   prueba, no la revisión de código.
3. **El límite de invitaciones por hora** estaba en el plan de la Fase 3 y nunca se implementó.
   Solo existía el tope de pendientes.

## 13. Cobertura de pruebas de seguridad

De las 235 pruebas automáticas, estas verifican seguridad directamente:

| Prueba | Verifica |
|---|---|
| `PruebasAislamientoEspacio` | Filtros de EF Core: lectura, agregaciones, escritura cruzada, borrado lógico |
| `PruebasAislamientoEnLaApi` | La cadena completa por HTTP: token, middleware, permisos, controladores |
| `PruebasAislamientoFinanciero` | Cuentas, movimientos y saldos no cruzan de espacio |
| `PruebasSesion` | Mensajes indistinguibles, rotación, detección de robo, cierre de sesión |
| `PruebasFlujoDeAlta` | Sin invitación no se entra; código de un solo uso y ligado a un correo |
| `PruebasCoberturaDeAislamiento` | Ninguna entidad de negocio se queda sin aislamiento |
| `PruebasGestionDeEspacio` | Reglas que impiden dejar un hogar sin propietario o bloqueado |
| `PruebasConfiguracionCorreo` | La contraseña SMTP nunca sale por la API; aislamiento entre espacios |
| `PruebasCabecerasSeguridad` | Las cabeceras están en toda respuesta, también en las de error |
| `PruebasLimiteDePeticiones` | El límite corta de verdad, devuelve `Retry-After`, y `/salud` queda libre |
| `PruebasLimiteDeInvitaciones` | El tope por hora frena el envío masivo |
| `PruebasServidorSmtpPermitido` | Puertos y direcciones internas rechazados; los servidores legítimos siguen funcionando |
| `PruebasSaltoDeFiltros` | Ningún `IgnoreQueryFilters` sin justificar en el código fuente |
| `PruebasAuditoria` | El historial no expone importes |

Además, cada módulo de negocio incluye una prueba de que sus datos no son visibles desde otro
espacio: metas, presupuestos, viajes, deudas, informes y panel.

> **Nota sobre las pruebas y el limitador.** La fábrica de pruebas general sube los cupos a un
> número enorme. Con `WebApplicationFactory` la dirección IP es nula, así que **todas** las
> pruebas caen en la misma partición del limitador y compartirían los cinco intentos por minuto
> de producción: la suite se rompería sola en cuanto creciera. Que el límite corta de verdad se
> comprueba en `PruebasLimiteDePeticiones`, que levanta su propia fábrica con cupos bajos y
> ejercita exactamente el mismo código.

## 14. Pendiente

| Tarea | Fase | Riesgo si se olvida |
|---|---|---|
| Persistir las claves de Data Protection en Blob Storage | 10 | **Crítico.** Además de romper los enlaces de «olvidé mi clave», las contraseñas SMTP guardadas dejarían de poder descifrarse al reiniciar, y habría que volver a introducirlas |
| SPF y DKIM del dominio remitente | 10 | Los correos de invitación acabarían en spam |
| Identidad administrada para Key Vault y Azure SQL | 10 | Secretos en la configuración |
| Segundo factor para el administrador de plataforma | Posterior | Es la cuenta con más alcance del sistema |
| Limitador distribuido, si algún día hay más de una instancia | Posterior | El cupo actual es por proceso: con dos instancias, el límite efectivo se duplica |
