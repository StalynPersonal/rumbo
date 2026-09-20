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
que pertenece a él durante quince minutos. Comprobarlo en cada petición hace que revocar un
acceso surta efecto casi de inmediato.

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

## 6. Registro de auditoría

`InterceptorAuditoria` escribe el historial automáticamente al guardar cambios. Se hace en un
interceptor y no en cada servicio a propósito: si dependiera de que alguien se acuerde de
registrar la acción, el historial tendría huecos justo en las operaciones menos habituales,
que son las que más interesa auditar.

**Nunca se registran** contraseñas, hashes de token ni códigos de invitación. La lista de
campos redactados está en `PropiedadesSensibles`.

## 7. Errores

Todos salen como `ProblemDetails` (RFC 9457) con un `traceId`.

En producción **no se expone** ninguna traza de pila ni el mensaje interno de una excepción
inesperada: revelarían rutas de ficheros, nombres de tablas y versiones de librerías. Se
devuelve un mensaje genérico y el detalle queda solo en los registros del servidor.

Las excepciones de dominio sí muestran su mensaje, porque están escritas para que las lea la
persona: «El código de invitación ha caducado».

## 8. Cobertura de pruebas de seguridad

De las 53 pruebas automáticas, estas verifican seguridad directamente:

| Prueba | Verifica |
|---|---|
| `PruebasAislamientoEspacio` (7) | Filtros de EF Core: lectura, agregaciones, escritura cruzada, borrado lógico |
| `PruebasAislamientoEnLaApi` (6) | La cadena completa por HTTP: token, middleware, permisos, controladores |
| `PruebasSesion` (6) | Mensajes indistinguibles, rotación, detección de robo, cierre de sesión |
| `PruebasFlujoDeAlta` (5) | Sin invitación no se entra; código de un solo uso y ligado a un correo |
| `PruebasCoberturaDeAislamiento` (4) | Ninguna entidad de negocio se queda sin aislamiento |

## 9. Pendiente

| Tarea | Fase | Riesgo si se olvida |
|---|---|---|
| Persistir las claves de Data Protection en Blob Storage | 10 | Los enlaces de «olvidé mi clave» dejarían de funcionar al reiniciar o al escalar |
| Límite de peticiones (rate limiting) | 9 | Fuerza bruta distribuida sobre el inicio de sesión |
| Cabeceras de seguridad (HSTS, CSP, X-Content-Type-Options) | 9 | |
| SPF y DKIM del dominio remitente | 10 | Los correos de invitación acabarían en spam |
| Segundo factor para el administrador de plataforma | Posterior | Es la cuenta con más alcance del sistema |
| Revisión OWASP completa | 9 | |
