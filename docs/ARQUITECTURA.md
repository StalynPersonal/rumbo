# Arquitectura de Rumbo

Documento para alguien que llega nuevo al proyecto. Explica **cómo** está organizado el sistema y,
sobre todo, **por qué**.

## 1. Vista general

```
┌─────────────────────────────────────────────────────────────┐
│  Rumbo.Movil  (.NET MAUI / Android)                         │
│  Vistas (XAML) → ModelosVista (MVVM) → Servicios (HttpClient)│
│  SecureStorage guarda el token. Sin acceso a base de datos.  │
└───────────────────────────┬─────────────────────────────────┘
                            │ HTTPS · REST · JSON · Bearer JWT
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  Rumbo.Api                                                  │
│  Controladores delgados · /api/v1/* · OpenAPI               │
└───────────────────────────┬─────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  Rumbo.Aplicacion                                           │
│  Servicios por modulo · DTOs · Validaciones · Calculadoras  │
└───────────────────────────┬─────────────────────────────────┘
                            ▼
┌──────────────────────────┐   ┌──────────────────────────────┐
│  Rumbo.Dominio           │◄──│  Rumbo.Infraestructura       │
│  Entidades e invariantes │   │  EF Core · Identity · SMTP   │
└──────────────────────────┘   └──────────────┬───────────────┘
                                              ▼
                                   ┌──────────────────────┐
                                   │  SQL Server / Azure  │
                                   └──────────────────────┘
```

## 2. Las capas y su regla de dependencia

| Proyecto | Responsabilidad | Puede referenciar a |
|---|---|---|
| `Rumbo.Dominio` | Entidades, enums, invariantes del negocio | **nada** |
| `Rumbo.Contratos` | DTOs que viajan por HTTP | **nada** |
| `Rumbo.Aplicacion` | Casos de uso, validaciones, cálculos | Dominio, Contratos |
| `Rumbo.Infraestructura` | EF Core, Identity, JWT, SMTP, tasas de cambio | Dominio, Aplicación |
| `Rumbo.Api` | Controladores, middleware, autorización | Aplicación, Infraestructura (solo para DI), Contratos |
| `Rumbo.Movil` | App Android | **solo** Contratos |

**Por qué el Dominio no referencia nada:** si las entidades dependieran de EF Core, no se podrían
probar sin base de datos y cualquier cambio del ORM obligaría a tocar el negocio. Al revés
funciona: la Infraestructura conoce al Dominio, no al contrario.

**Por qué la app móvil solo ve `Rumbo.Contratos`:** si referenciara `Aplicacion`, el APK acabaría
llevando dentro EF Core y la lógica del servidor. Y si los DTOs se copiaran a mano, se
desincronizarían en silencio. Un proyecto compartido sin dependencias resuelve ambos problemas.

**La API es la única autoridad.** La app móvil no calcula saldos, no decide permisos y no aplica
reglas financieras: solo muestra lo que la API le dice. Un APK se puede modificar; el servidor no.

## 3. Flujo de autenticación

```
POST /api/v1/autenticacion/iniciar-sesion  { correo, clave }
   → se verifica la clave (hash PBKDF2 de ASP.NET Core Identity)
   → se cargan las membresias del usuario
   → TokenAcceso  (JWT, 15 min)
   → TokenRenovacion (opaco, 30 dias; en base de datos se guarda solo su hash)

Claims del JWT:
   sub   → UsuarioId
   esp   → EspacioId activo
   rol   → rol del usuario EN ESE espacio
   perm  → permisos concretos
```

Cuando el token de acceso caduca, la app llama a `/autenticacion/renovar`. El token de renovación
**rota**: se revoca el anterior y se emite uno nuevo. Si alguien intenta reutilizar un token ya
revocado, se revoca toda la familia de tokens, porque esa es la señal de que un token fue robado.

## 4. Multi-tenancy: cómo se aíslan los espacios

Un `Espacio` es un hogar, una pareja o una familia. **Un usuario jamás debe ver datos de otro
espacio.** No confiamos en que el programador recuerde escribir `WHERE EspacioId = ...`: hay cinco
capas, y el diseño asume que cualquiera de ellas puede fallar.

```
Peticion → se valida el JWT
        → MiddlewareResolucionEspacio lee el claim `esp`
          y COMPRUEBA EN BASE DE DATOS que el usuario tiene membresia activa
        → rellena IContextoEspacio
        → ContextoRumbo aplica el filtro global en TODA consulta
        → SaveChanges rechaza escribir en un espacio ajeno
```

| Capa | Qué protege |
|---|---|
| 1. `IContextoEspacio` | El `EspacioId` sale del token firmado, nunca de lo que envíe el cliente |
| 2. Filtros globales de EF Core | Toda consulta filtra por espacio, aunque el programador lo olvide |
| 3. `InterceptorEspacio` | Asigna el espacio al insertar y rechaza modificar datos de otro |
| 4. Validación de referencias | Una cuenta o categoría de otro espacio da **404**, no 403 (no revelamos que existe) |
| 5. Permisos | `[RequierePermiso(...)]` según el rol dentro de ese espacio |

El `EspacioId` es la primera columna de casi todos los índices, tanto por rendimiento como porque
hace visible el aislamiento en el plan de ejecución de SQL.

Hay una prueba de integración **obligatoria** que verifica que un usuario del Espacio A recibe 404
al pedir datos del Espacio B. Si esa prueba falla, no se despliega.

### El administrador de plataforma

`AdministradorPlataforma` es un rol de Identity, **no** un rol de espacio. Ese usuario **no tiene
ninguna fila en `MembresiasEspacio`**, y el listado de miembros se construye desde esa tabla: por
eso no aparece en ningún espacio. No está oculto por un filtro que se pueda olvidar, sencillamente
no está.

Además, **no puede leer datos financieros**. Gestiona espacios, usuarios e invitaciones a través de
`ContextoAdministracion`, que solo expone esas tablas. No ve movimientos, saldos, metas ni viajes.

## 5. Decisiones que conviene entender antes de tocar código

### La transferencia son dos movimientos, no uno

Traspasar RD$10,000 de la cuenta de Juan a la cuenta conjunta crea **dos** filas en `Movimientos`
(una salida y una entrada), unidas por un `TransferenciaId`.

Así el saldo de cualquier cuenta es siempre la suma de sus movimientos, sin casos especiales, y
excluir las transferencias de los reportes de ingresos y gastos es un único filtro por `Tipo`. Con
un solo registro que tuviera cuenta origen y cuenta destino, cada consulta de saldo tendría que
acordarse de tratarlo aparte, y tarde o temprano alguna no lo haría.

**Una transferencia nunca es un gasto.** Contabilizarla como tal distorsionaría todos los informes.

### Un aporte a una meta también es una transferencia

Ahorrar no es gastar. Cuando el usuario aporta a la meta «Viaje a Colombia», el dinero se mueve de
una cuenta operativa a una de ahorro y el movimiento queda etiquetado con `MetaId`.

### Un gasto de viaje sí es un gasto

Lleva `ViajeId` para poder agruparlo, pero sigue siendo un gasto normal.

### El saldo se guarda, pero la verdad es el libro mayor

`Cuenta.SaldoActual` se actualiza en la misma transacción de base de datos que el movimiento, con
control de concurrencia. Es una caché para que el panel sea rápido. La verdad sigue siendo la suma
de los movimientos, y `POST /cuentas/{id}/reconciliar` la recalcula y avisa si hay desviación.

### Las recomendaciones nunca mueven dinero

El motor de recomendaciones sugiere; **el usuario confirma**. Una recomendación nace con estado
`Pendiente` y solo se convierte en movimiento cuando la persona pulsa «Crear aporte». Cada
recomendación guarda sus insumos, para poder explicar de dónde salió el número.

## 6. Manejo de errores

Todos los errores se devuelven como `ProblemDetails` (RFC 9457), con un `traceId` que permite
encontrar la petición en Application Insights. En producción no se exponen trazas de pila. Los
mensajes dirigidos al usuario van en español.

## 7. Configuración y secretos

| Ambiente | Dónde viven los secretos |
|---|---|
| Development | `dotnet user-secrets`, fuera del repositorio |
| Staging / Production | Azure Key Vault, leído con identidad administrada |

Nunca en `appsettings.json`. El `.gitignore` bloquea además `appsettings.Local.json`, `.env`,
keystores y certificados.
