# Rumbo — Plan del proyecto

> **Rumbo** — *Tus finanzas. Tus metas. Tu próximo destino.*

Este documento recoge el **plan completo aprobado en la Fase 0**, tal como se acordó antes de
escribir la primera línea de código. Se conserva íntegro a propósito: es la referencia contra la
que se comprueba si el proyecto sigue el rumbo acordado o se ha desviado.

Cuando una decisión posterior matiza o cambia algo de aquí, **no se reescribe este documento**:
se registra en [DECISIONES.md](DECISIONES.md) con su fecha y su porqué. Así siempre se puede ver
qué se planificó, qué se cambió y por qué.

## Estado actual

Al día de hoy (20 de septiembre de 2026):

| Fase | Contenido | Estado |
|---|---|---|
| 0 | Análisis y arquitectura | ✅ Completada |
| 1 | Estructura de la solución | ✅ Completada |
| 2 | Dominio, EF Core y primera migración | ✅ Completada |
| 3 | Autenticación, invitaciones y multi-tenancy | ✅ Completada |
| 4 | API financiera | ✅ Completada |
| 5 | Presupuestos, metas y recomendaciones | ✅ Completada |
| 6 | Viajes y viabilidad | ✅ Completada |
| 7 | Panel y reportes | ✅ Completada |
| 8 | Aplicación .NET MAUI | ✅ Completada — nueve pantallas y APK Release verificado |
| 9 | Seguridad y pruebas | ✅ Completada |
| 10 | Despliegue en Azure | ✅ Completada — falta ejecutar los comandos en tu suscripción |
| 11 | Generación del APK | ✅ Completada — firma verificada de punta a punta |

**Desviaciones del plan original registradas hasta ahora:** ver D1–D72 en
[DECISIONES.md](DECISIONES.md). Las más relevantes respecto a este documento son la adición de
`Movimiento.CategoriaViaje` (D41), la interfaz `IDirectorioUsuarios` (D49) y el criterio de que
un pago de deuda cuenta como gasto (D45).

---

## Contexto

No existía código todavía: `C:\Users\scontreras\Desktop\mio\rumbo` estaba vacío y no era un
repositorio git. Se construye desde cero un asistente financiero personal/familiar multi-tenant:
backend ASP.NET Core en Azure + app Android .NET MAUI (APK). El objetivo no es registrar gastos,
sino **analizar** el flujo financiero del hogar y **recomendar** cómo alcanzar metas (viajes,
fondo de emergencia, etc.) de forma explicable y sin mover dinero automáticamente.

Entorno verificado en esta máquina:

| Herramienta | Estado |
|---|---|
| .NET SDK | 10.0.401 ✅ (también 8/9 instalados) |
| EF Core Tools | 10.0.11 ✅ |
| git | 2.54.0 ✅ |
| SQL Server `localhost` | **12.0.5000.0 = SQL Server 2014 Standard**, instancia por defecto `MSSQLSERVER`, en ejecución. Se conserva intacta; Rumbo usará una instancia nueva (ver decisión 24 y Fase 1). |
| Workloads MAUI | `android`, `ios`, `maccatalyst`, `maui-windows` — **falta `maui-android`** (`dotnet workload install maui-android`) |

**Decisiones aprobadas en Fase 0:**

1. Identidad: **ASP.NET Core Identity** (`Usuario : IdentityUser<Guid>`), la tenencia vive fuera
   de Identity.
2. Saldos: **calculados + instantánea** `SaldoActual` con `rowversion` y reconciliación.
3. Moneda: **multi-moneda con tasas de cambio desde el día 1**.
4. Capa de aplicación: **servicios por módulo + FluentValidation**, sin MediatR.
5. **Todo el código en español**: proyectos, namespaces, entidades, métodos, DTOs, endpoints y
   base de datos.
6. **Sin tildes ni ñ en identificadores** (`Categoria`, `Anio`); los textos visibles al usuario
   sí llevan ortografía correcta.
7. **Git local** en la Fase 1; el remoto de GitHub lo conecta el usuario.
8. **Base de datos de desarrollo: SQL Server local**, en una instancia nueva, sin tocar la
   instancia 2014 existente. Azure SQL entra en la Fase 10.
9. **Correo por SMTP** configurado con una cuenta propia (host, puerto, usuario, clave,
   remitente).
10. **Alta cerrada por invitación**, con un **administrador de plataforma** invisible para los
    espacios.
11. **Código MAUI simple y didáctico**, pensado para aprender: sin atajos "mágicos", con
    comentarios explicativos.

---

## 0. Glosario canónico (contrato entre fases)

Normativo. Ninguna fase puede introducir un nombre distinto para un mismo concepto; cualquier
término nuevo se agrega aquí y a [DECISIONES.md](DECISIONES.md). La versión viva está en
[GLOSARIO.md](GLOSARIO.md).

| Concepto | Nombre en el código | Nota |
|---|---|---|
| Tenant | **`Espacio`** | Unidad de aislamiento. Inicialmente hogar/pareja, extensible a negocio. Se prefiere «Espacio» sobre «Hogar» porque `TipoEspacio` incluirá `Negocio`. |
| Tenant membership | `MembresiaEspacio` | |
| User | `Usuario` | |
| Platform admin | `AdministradorPlataforma` | Rol de Identity (plataforma), **no** un `RolEspacio`. |
| Account | `Cuenta` | |
| Transaction (asiento) | **`Movimiento`** | Se evita `Transaccion` para no confundirlo con la transacción de base de datos. |
| Transfer | `Transferencia` | |
| Category | `Categoria` | |
| Budget / line | `Presupuesto` / `LineaPresupuesto` | |
| Goal / contribution | `Meta` / `AporteMeta` | |
| Trip | `Viaje` / `LineaPresupuestoViaje` | |
| Recurring expense/income | `GastoRecurrente` / `IngresoRecurrente` | |
| Debt / payment | `Deuda` / `PagoDeuda` | |
| Currency / FX rate | `Moneda` / `TasaCambio` | |
| Invitation | `Invitacion` (`TipoInvitacion`: `Propietario` \| `Miembro`) | |
| Audit log | `RegistroAuditoria` | |
| Recommendation | `Recomendacion` | |
| Notification | `Notificacion` | |
| Refresh token | `TokenRenovacion` | |
| Personal / Shared | `TipoReparto`: `Personal` \| `Compartido` | |
| Roles de espacio | `RolEspacio`: `Propietario` \| `Administrador` \| `Miembro` | |
| Dashboard | `Panel` | `/api/v1/panel` |

**Nombres impuestos por el framework que NO se traducen:** `Program.cs`, sufijos `Controller` y
`Attribute`, `IdentityUser<>`/`IdentityRole<>`/`IdentityDbContext<>`,
`DbContext`/`DbSet`/`OnModelCreating`/`SaveChangesAsync`, carpeta `Migrations` (la espera
`dotnet ef`), `appsettings.json`, `Platforms/Android`. Nuestras clases sí van en español,
incluido `ContextoRumbo : IdentityDbContext<...>`.

---

## 1. Análisis de requisitos — qué se entendió

### Dominio

Rumbo es un **libro mayor compartido** más una **capa analítica**. Todo movimiento de dinero es
un asiento en la entidad central `Movimiento`; presupuestos, metas, viajes, deudas y recurrentes
son *vistas, restricciones o proyecciones* sobre ese libro mayor, nunca almacenes paralelos de
dinero.

Consecuencias directas de los requisitos:

- **Transferencia ≠ gasto.** Dos asientos (salida y entrada) unidos por `TransferenciaId`, y
  **todos los reportes excluyen `TipoMovimiento.Transferencia`** de ingresos/gastos. Un solo
  registro con `CuentaOrigenId`/`CuentaDestinoId` obligaría a casos especiales en cada consulta
  de saldo; dos asientos hacen que `SUM(Monto)` por cuenta sea siempre correcto.
- **Un aporte a una meta es una transferencia**, no un gasto: dinero que va de una cuenta
  operativa a una de ahorro, etiquetado con `MetaId`. Evita el error clásico de contabilizar el
  ahorro como gasto.
- **Un gasto de viaje sí es un gasto**, etiquetado con `ViajeId`. El viaje tiene presupuesto
  propio y fondo de ahorro propio: dos caras distintas.
- **`TipoReparto` + `PagadoPorUsuarioId`** no son lo mismo: un gasto compartido pagado por Juan
  genera una deuda implícita de María. En v1 se **registra** y se **reporta** el desbalance; la
  liquidación entre personas queda como extensión.
- **Motor de recomendaciones determinístico**, no IA. Cada recomendación lleva sus insumos y su
  fórmula para ser auditable, y **nunca** ejecuta el movimiento: el usuario confirma
  (`[Crear aporte] / [Ignorar]`).

### Prioridad ante conflictos

Seguridad > aislamiento multi-tenant > integridad financiera > correctitud > mantenibilidad >
UX > rendimiento > funcionalidades. Es el criterio de desempate de todo el plan.

### Lo que NO es v1

Docker/K8s/microservicios; notificaciones push; sincronización offline completa; amortización
avanzada de deudas; liquidación entre miembros; IA generativa; Google Play.

---

## 2. Arquitectura

```
┌─────────────────────────────────────────────────────────────┐
│  Rumbo.Movil  (.NET MAUI / net10.0-android)                 │
│  Vistas (XAML) → ModelosVista (MVVM) → Servicios (HttpClient)│
│  SecureStorage: token de acceso + token de renovacion       │
│  Sin logica financiera confiable. Sin acceso a BD.          │
└───────────────────────────┬─────────────────────────────────┘
                            │ HTTPS · REST · JSON · Bearer JWT
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  Rumbo.Api  (ASP.NET Core Web API, Azure App Service)       │
│  Controladores delgados · /api/v1/* · OpenAPI               │
│  Middleware: ManejoExcepciones(ProblemDetails) → Auth       │
│              → ResolucionEspacio → LimitePeticiones         │
└───────────────────────────┬─────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  Rumbo.Aplicacion                                           │
│  Interfaces + servicios por modulo · DTOs · FluentValidation│
│  Calculadoras: Saldos · Presupuestos · Metas · Viajes ·     │
│                FlujoCaja · ConversorMonedas                 │
│  MotorRecomendaciones (reglas deterministicas)              │
└───────────────────────────┬─────────────────────────────────┘
                            ▼
┌──────────────────────────┐   ┌──────────────────────────────┐
│  Rumbo.Dominio           │◄──│  Rumbo.Infraestructura       │
│  Entidades · enums ·     │   │  ContextoRumbo (EF Core 10)  │
│  invariantes · Dinero ·  │   │  Filtros globales de Espacio │
│  contratos de repos      │   │  Identity · JWT · Auditoria  │
│  SIN dependencias        │   │  SMTP · KeyVault · AppInsights│
└──────────────────────────┘   └──────────────┬───────────────┘
                                              ▼
                                   ┌──────────────────────┐
                                   │  SQL Server / Azure  │
                                   └──────────────────────┘
```

**Regla de dependencias:** `Api → Aplicacion → Dominio`; `Infraestructura → Aplicacion,
Dominio`; `Api → Infraestructura` **solo** en `Program.cs`/DI. `Dominio` no referencia nada.
`Movil` referencia únicamente `Rumbo.Contratos`.

> **Cambio propuesto a la estructura original:** añadir **`Rumbo.Contratos`** (sin dependencias)
> con los DTOs de petición/respuesta. Sin él, o se duplican los DTOs a mano en MAUI (se
> desincronizan silenciosamente) o MAUI acaba referenciando `Aplicacion` y arrastrando EF Core
> al APK. También mover `Rumbo.Movil` a `src/Movil/` para que la CI del backend no compile los
> workloads de Android.

### Flujo de autenticación

```
POST /api/v1/autenticacion/iniciar-sesion  { correo, clave }
   → Identity.PasswordHasher.Verify + bloqueo por intentos
   → carga MembresiaEspacio del usuario
   → TokenAcceso (JWT, 15 min) + TokenRenovacion (opaco, 30 dias, hasheado en BD)

Claims del JWT:  sub (UsuarioId) · email · esp (EspacioId activo)
                 rol (rol en ESE espacio) · perm[] · jti · exp

POST /api/v1/autenticacion/renovar { tokenRenovacion }
   → valida hash + expiracion + no revocado
   → ROTACION: revoca el anterior, emite uno nuevo
   → reuso de token revocado ⇒ revoca toda la familia (deteccion de robo)

POST /api/v1/autenticacion/cambiar-espacio { espacioId }
   → verifica membresia ACTIVA → nuevo token de acceso con otro `esp`
```

El claim `esp` viaja **firmado por el servidor**, nunca en el body ni en una cabecera que el
cliente controle.

### Flujo multi-tenant (por petición)

```
Peticion → JwtBearer valida firma/exp → ClaimsPrincipal
        → MiddlewareResolucionEspacio: lee `esp`, verifica membresia ACTIVA en BD
          (IMemoryCache corto, invalidado al cambiar membresias)
        → puebla IContextoEspacio (scoped)
        → ContextoRumbo lee IContextoEspacio en el constructor
        → Filtro global: e => e.EspacioId == _contexto.EspacioId && !e.Eliminado
        → SaveChangesAsync: asigna EspacioId al insertar y RECHAZA
          (ExcepcionEspacioNoCoincide) toda entidad modificada de otro espacio
```

---

## 3. Modelo de multi-tenancy y de alta de usuarios

### 3.1 Aislamiento: cinco capas independientes

Tenencia **de fila compartida** (`EspacioId` en cada tabla). Ninguna capa basta por sí sola; el
diseño asume que cada una puede fallar.

| # | Capa | Qué hace | Qué falla si no está |
|---|---|---|---|
| 1 | `IContextoEspacio` (scoped) | Única fuente del `EspacioId`, derivada del JWT + membresía verificada. Nunca del body/query/header. | Un cliente suplantaría espacio enviando un id. |
| 2 | **Filtros globales de consulta** (EF Core) | Toda entidad `IEntidadDeEspacio` filtra por `EspacioId` y `Eliminado` en cada `SELECT`. | Un `WHERE` olvidado → fuga de lectura. |
| 3 | **`InterceptorEspacio` en `SaveChanges`** | Asigna `EspacioId` al insertar; excepción si un `Modified`/`Deleted` pertenece a otro espacio; dispara `InterceptorAuditoria`. | Escritura cruzada o alta sin espacio. |
| 4 | **Validación de referencias** | `CuentaId`, `CategoriaId`, `MetaId`, `ViajeId` deben pertenecer al espacio activo. Si el filtro no los devuelve → **404, no 403** (no se filtra la existencia). | Referencias cruzadas por FK adivinada. |
| 5 | **Autorización por permiso** | `[RequierePermiso(Permisos.Movimientos.Escribir)]`, resuelto desde el rol de la membresía del espacio activo. | Un `Miembro` haría cosas de `Propietario`. |

**Detalles que hacen la diferencia:**

- Tablas **globales** (`Usuarios`, `Espacios`, `MembresiasEspacio`, `Monedas`, `TasasCambio`,
  categorías del sistema) sin filtro; se marcan explícitamente y un test enumera que *toda*
  entidad no listada sí lo tenga.
- `IgnoreQueryFilters()` prohibido fuera del módulo de administración de plataforma; test de
  arquitectura lo verifica.
- `EspacioId` es la **primera columna de casi todo índice** (`(EspacioId, ...)`), por rendimiento
  y porque hace evidente el aislamiento en el plan de ejecución.
- Migración futura sin re-arquitectura: si un espacio creciera mucho, se pasa a base por espacio
  cambiando solo la resolución de la cadena de conexión.

**Test obligatorio:** un usuario del Espacio A recibe **404** en
`GET /api/v1/movimientos/{idDelEspacioB}`, y un `POST` con `cuentaId` del Espacio B es
rechazado. Prueba de integración sobre el pipeline HTTP real (`WebApplicationFactory` + SQL
Server), no unitaria del `DbContext`: lo que debe probarse es la cadena completa.

### 3.2 Administrador de plataforma y alta cerrada por invitación

```
AdministradorPlataforma
  └─ crea Invitacion(Tipo=Propietario, correo, espacioNombre?)
        │  envia correo con enlace + codigo (un solo uso, expira 7 dias)
        ▼
     Persona acepta → se registra → se crea su Espacio
                                  → queda como RolEspacio.Propietario
        │
        └─ Propietario crea Invitacion(Tipo=Miembro, correo, rol)
              └─ Invitado se registra o inicia sesion
                 → MembresiaEspacio SOLO de ese espacio, con el rol indicado
```

**Por qué el administrador es invisible en cada espacio, estructuralmente y no por un filtro:**
`AdministradorPlataforma` es un **rol de Identity**, no un `RolEspacio`. Ese usuario **no tiene
ninguna fila en `MembresiasEspacio`**, y el listado de miembros de un espacio se construye desde
esa tabla. No aparece porque no está, no porque se esté escondiendo. Un filtro se puede olvidar
en un endpoint nuevo; la ausencia de fila no.

**El administrador de plataforma NO puede leer datos financieros.** Puede crear invitaciones,
suspender o reactivar un espacio, ver métricas agregadas (número de espacios, usuarios activos,
fecha de último acceso) y consultar la auditoría de accesos. No puede ver movimientos, cuentas,
saldos, metas ni viajes de nadie. Razones:

- Mínimo privilegio, y todo dato financiero se trata como sensible.
- Es dinero de una pareja; un rol "dios" es un riesgo permanente, no una comodidad.
- Técnicamente: el área administrativa vive en `Rumbo.Aplicacion/Modulos/Administracion/`, es el
  **único** lugar con permiso para `IgnoreQueryFilters()`, y ese permiso se limita a `Espacios`,
  `Usuarios`, `MembresiasEspacio` e `Invitaciones` mediante un `ContextoAdministracion` que solo
  expone esos `DbSet`. Todo lo que haga queda en `RegistroAuditoria` con auditoría reforzada.

**Detalles de seguridad de los códigos de invitación:**

- Token aleatorio de 256 bits (`RandomNumberGenerator`), mostrado una sola vez; en BD se guarda
  **solo el hash SHA-256**.
- Ligado al correo del destinatario, de un solo uso, expira en 7 días, revocable.
- Aceptarlo exige que el correo de registro coincida con el de la invitación.
- Límite de invitaciones por espacio y por hora (anti-abuso).
- El primer `AdministradorPlataforma` se crea con una semilla controlada por
  configuración/variable de entorno en el primer arranque, nunca con credenciales en el código.

`POST /api/v1/autenticacion/registrar` **exige un código de invitación válido**. No hay
auto-registro público.

---

## 4. Modelo de datos inicial

### Convenciones transversales

- PK: `Guid` con `Guid.CreateVersion7()` (secuencial en el tiempo → no fragmenta el índice
  agrupado como el v4, y no es adivinable como un entero).
- Dinero: `decimal(19,4)`. **Nunca** `float`/`double`. Todo monto lleva su `Moneda` (char(3),
  ISO-4217).
- Fechas: `DateTimeOffset` para instantes del sistema (`FechaCreacion`); `DateOnly` para fechas
  contables (`FechaMovimiento`, `FechaVencimiento`) — evita que un gasto del día 1 aparezca el
  día 30 por zona horaria.
- Auditoría base (`EntidadAuditable`): `FechaCreacion`, `CreadoPorUsuarioId`,
  `FechaActualizacion`, `ActualizadoPorUsuarioId`.
- Borrado lógico (`IBorradoLogico`): `Eliminado`, `FechaEliminacion`, `EliminadoPorUsuarioId`.
  Los movimientos **nunca** se borran físicamente.
- `Version` (`rowversion`) en `Cuentas` y `Metas` para concurrencia optimista.
- Tablas en plural y español (`Movimientos`, `Cuentas`, `LineasPresupuesto`); las de Identity se
  renombran (`Usuarios`, `Roles`, `UsuariosRoles`, `ClavesUsuario`…).

### Entidades

**Identidad, espacios e invitaciones (globales, sin filtro de espacio)**

| Entidad | Campos clave |
|---|---|
| `Usuario : IdentityUser<Guid>` | + `NombreCompleto`, `CulturaPreferida`, `FechaCreacion`, `UltimoAcceso`. |
| `Rol : IdentityRole<Guid>` | Roles de plataforma: `AdministradorPlataforma`. |
| `Espacio` | `Nombre`, `Tipo` (`TipoEspacio`: Personal/Pareja/Familia/Negocio), `MonedaBase`, `ZonaHoraria`, `Estado` (Activo/Suspendido). |
| `MembresiaEspacio` | `UsuarioId`, `EspacioId`, `Rol` (`RolEspacio`), `Estado`, `FechaIngreso`. **Único (UsuarioId, EspacioId)**. |
| `Invitacion` | `Tipo` (`TipoInvitacion`: Propietario/Miembro), `Correo`, `HashCodigo`, `EspacioId?`, `RolAsignado?`, `EmitidaPorUsuarioId`, `FechaExpiracion`, `FechaUso?`, `Estado`. |
| `TokenRenovacion` | `Hash`, `FechaExpiracion`, `FechaRevocacion`, `ReemplazadoPorTokenId` (familia → detección de reuso). |

**Referencia monetaria (globales)**

| Entidad | Campos clave |
|---|---|
| `Moneda` | `Codigo` (PK, char 3), `Nombre`, `Simbolo`, `Decimales`. Semilla: DOP, USD, EUR, GBP. |
| `TasaCambio` | `MonedaOrigen`, `MonedaDestino`, `Fecha`, `Tasa decimal(19,8)`, `Origen` (Manual/Api). **Único (Origen, Destino, Fecha)**. Se usa la tasa vigente **a la fecha del movimiento**, no la de hoy. |

**Núcleo financiero (por espacio)**

| Entidad | Campos clave |
|---|---|
| `Cuenta` | `Nombre`, `Tipo` (Bancaria/TarjetaCredito/Efectivo/Ahorro/Inversion/Otra), `Moneda`, `SaldoInicial`, `SaldoActual`, `PropietarioUsuarioId?`, `EsCompartida`, `Activa`, `Institucion`, `Version`. Tarjeta: `LimiteCredito`, `DiaCorte`, `DiaPago`. |
| `Categoria` | `Nombre`, `CategoriaPadreId?` (jerarquía, 2 niveles en v1), `Tipo` (Ingreso/Gasto/Ambos), `Icono`, `Color`, `EsDelSistema`. |
| `Movimiento` | **Entidad central.** `Tipo` (Ingreso/Gasto/Transferencia/Ajuste), `CuentaId`, `CategoriaId?`, `Monto` (siempre positivo), `Signo` (+1/−1), `Moneda`, `MontoEnMonedaBase`, `TasaCambioAplicada`, `FechaMovimiento`, `Descripcion`, `Notas`, `MetodoPago`, `Reparto`, `PagadoPorUsuarioId`, `MetaId?`, `ViajeId?`, `TransferenciaId?`, `GastoRecurrenteId?`, `DeudaId?`. |
| `Transferencia` | `MovimientoOrigenId`, `MovimientoDestinoId`, `MontoOrigen`, `MontoDestino`, `TasaCambioAplicada`. |
| `GastoRecurrente` | `Nombre`, `CategoriaId`, `CuentaId`, `MontoEstimado`, `EsMontoFijo`, `Frecuencia` (Semanal/Quincenal/Mensual/Trimestral/Anual), `ProximaFechaPago`, `DiasAvisoPrevio`, `Estado`, `GeneracionAutomatica` (false por defecto). |
| `IngresoRecurrente` | Análogo, para salarios y rentas. Alimenta las proyecciones. |

**Planificación (por espacio)**

| Entidad | Campos clave |
|---|---|
| `Presupuesto` | `Nombre`, `TipoPeriodo` (Mensual), `InicioPeriodo`, `FinPeriodo`, `Moneda`, `Estado`. |
| `LineaPresupuesto` | `PresupuestoId`, `CategoriaId`, `MontoAsignado`, `UmbralAviso` (80), `UmbralCritico` (90), `UmbralExcedido` (100) — configurables, con respaldo en `ConfiguracionEspacio`. |
| `Meta` | `Nombre`, `Descripcion`, `MontoObjetivo`, `MontoActual`, `Moneda`, `FechaObjetivo`, `Prioridad`, `AporteMensualMinimo`, `Estado`, `CuentaVinculadaId?`, `Version`. |
| `AporteMeta` | `MetaId`, `MovimientoId?`, `Monto`, `Fecha`. El aporte real es una transferencia; esta tabla es la proyección auditable. |
| `Viaje` | `Nombre`, `Destino`, `FechaInicio`, `FechaFin`, `PresupuestoTotal`, `Moneda`, `Estado`, `MetaId?` (el fondo del viaje es una meta). |
| `LineaPresupuestoViaje` | `ViajeId`, `Categoria` (Vuelos/Hospedaje/Alimentacion/Transporte/Actividades/Compras/Documentos/Seguro/Otros), `MontoPlanificado`. |
| `Deuda` | `Nombre`, `Tipo` (TarjetaCredito/Prestamo/PrestamoPersonal/Vehiculo/Hipoteca/Otra), `MontoOriginal`, `SaldoActual`, `TasaInteres`, `PagoMinimo`, `PagoMensual`, `DiaVencimiento`, `FechaInicio`, `FechaEstimadaLiquidacion`, `CuentaVinculadaId?`. |
| `PagoDeuda` | `DeudaId`, `MovimientoId`, `MontoCapital`, `MontoInteres`, `Fecha`. |

**Soporte (por espacio)**

| Entidad | Campos clave |
|---|---|
| `RegistroAuditoria` | `EspacioId?`, `UsuarioId`, `Accion`, `TipoEntidad`, `EntidadId`, `Cambios` (JSON en `nvarchar(max)`, **sin** datos sensibles), `FechaHora`, `DireccionIp`, `IdCorrelacion`. |
| `Notificacion` | `Tipo`, `Titulo`, `Cuerpo`, `Datos`, `ProgramadaPara`, `FechaLectura`, `Estado`. Persistida en v1; el transporte push se enchufa después. |
| `Recomendacion` | `Tipo`, `Titulo`, `Cuerpo`, `Insumos` (JSON), `MontoSugerido`, `FechaGeneracion`, `FechaExpiracion`, `Estado` (Pendiente/Aceptada/Descartada), `MetaId?`, `ViajeId?`. |
| `ConfiguracionEspacio` | Umbrales de presupuesto, día de inicio de mes, moneda base, preferencias de recomendación. |

### Relaciones principales

```
Usuario ──< MembresiaEspacio >── Espacio ;  Usuario ──< Invitacion (emitidas)
Espacio ──< Cuenta ──< Movimiento >── Categoria (jerarquica, auto-referencia)
Movimiento >── Usuario (PagadoPor)
Movimiento >── Meta | Viaje | Transferencia | Deuda | GastoRecurrente  (opcionales)
Transferencia ── 2 × Movimiento (salida + entrada)
Presupuesto ──< LineaPresupuesto >── Categoria
Meta ──< AporteMeta >── Movimiento
Viaje ──< LineaPresupuestoViaje ;  Viaje ── Meta (fondo del viaje)
Deuda ──< PagoDeuda >── Movimiento
Moneda ──< TasaCambio
```

### Índices y restricciones destacados

- `IX_Movimientos_Espacio_Fecha` → `(EspacioId, FechaMovimiento DESC) INCLUDE (Monto, Tipo,
  CuentaId, CategoriaId)` — sirve al panel y a los reportes.
- `IX_Movimientos_Espacio_Cuenta_Fecha`, `IX_Movimientos_Espacio_Categoria_Fecha`,
  `IX_Movimientos_TransferenciaId`; filtrados por `Eliminado = 0`.
- Únicos: `(UsuarioId, EspacioId)` en membresías; `(EspacioId, Nombre, CategoriaPadreId)` en
  categorías; `(MonedaOrigen, MonedaDestino, Fecha)` en tasas; `HashCodigo` en invitaciones.
- Check: `Monto > 0`; `MontoObjetivo > 0`; `MontoActual >= 0`; `FinPeriodo > InicioPeriodo`.
- **Comportamiento de borrado: `Restrict` por defecto.** Ninguna cascada que borre historial
  financiero.

---

## 5. Estructura de la solución

```
Rumbo.slnx
.editorconfig · Directory.Build.props · Directory.Packages.props (gestion central de paquetes)
.gitignore · README.md · LICENSE

src/
  Rumbo.Dominio/
    Comun/             EntidadAuditable, IEntidadDeEspacio, IBorradoLogico, Dinero, Resultado
    Entidades/         Identidad/ Financiero/ Planificacion/ Soporte/
    Enums/             TipoMovimiento, TipoCuenta, RolEspacio, TipoReparto, TipoInvitacion, ...
    Excepciones/       ExcepcionDominio, ExcepcionEspacioNoCoincide
  Rumbo.Aplicacion/
    Comun/             IContextoEspacio, IUsuarioActual, IProveedorFechaHora, IContextoRumbo
    Contratos/         IServicioCuentas, IServicioMovimientos, IServicioMetas, IEnviadorCorreo, ...
    Modulos/           Cuentas/ Movimientos/ Categorias/ Presupuestos/ Metas/ Viajes/
                       GastosRecurrentes/ Deudas/ Reportes/ Recomendaciones/ Panel/
                       Autenticacion/ Espacios/ Invitaciones/ Administracion/
                         (cada uno: Servicio, Validadores, Mapeos)
    Calculadoras/      CalculadoraSaldos, CalculadoraPresupuestos, CalculadoraMetas,
                       CalculadoraViajes, AnalizadorFlujoCaja, ConversorMonedas
    Recomendaciones/   IReglaRecomendacion + reglas + MotorRecomendaciones
  Rumbo.Contratos/     DTOs compartidos API ↔ MAUI (sin dependencias)
    Autenticacion/ Cuentas/ Movimientos/ ... Comun/ (RespuestaApi, ResultadoPaginado)
  Rumbo.Infraestructura/
    Persistencia/      ContextoRumbo, ContextoAdministracion, Configuraciones/,
                       Migrations/, Semilla/
    Persistencia/Interceptores/  InterceptorEspacio, InterceptorAuditoria,
                                 InterceptorBorradoLogico
    Identidad/         ServicioTokensJwt, ServicioTokenRenovacion, PoliticaClaves
    MultiEspacio/      ContextoEspacio, MiddlewareResolucionEspacio, CacheMembresias
    Correo/            EnviadorCorreoSmtp (MailKit), PlantillasCorreo/
    Servicios/         ProveedorTasasCambio, DespachadorNotificaciones
    InyeccionDependencias.cs
  Rumbo.Api/
    Controladores/V1/  AutenticacionController, CuentasController, MovimientosController,
                       ..., AdministracionController
    Middleware/        MiddlewareManejoExcepciones, MiddlewareRegistroPeticiones
    Autorizacion/      RequierePermisoAttribute, ManejadorPermisos, Permisos
    Extensiones/       ExtensionesServicios, ExtensionesAplicacion
    Program.cs · appsettings{,.Development,.Staging,.Production}.json

src/Movil/
  Rumbo.Movil/         (net10.0-android)
    Vistas/ ModelosVista/ Servicios/ Modelos/ Convertidores/ Controles/ Recursos/
    Platforms/Android/ MauiProgram.cs · AppShell.xaml

pruebas/
  Rumbo.PruebasUnitarias/     Calculadoras, MotorRecomendaciones, validadores, JWT
  Rumbo.PruebasIntegracion/   WebApplicationFactory + SQL: autenticacion, invitaciones,
                              CRUD, transferencias,
                              **PruebasAislamientoEspacio (obligatorio)**
  Rumbo.PruebasArquitectura/  NetArchTest: reglas de dependencia, filtros de espacio,
                              prohibicion de IgnoreQueryFilters fuera de Administracion

docs/   ARQUITECTURA.md · BASE-DE-DATOS.md · API.md · SEGURIDAD.md · DESPLIEGUE.md
        MOVIL.md · DECISIONES.md · GLOSARIO.md · PLAN.md
.github/workflows/  backend-ci.yml · backend-deploy.yml · movil-apk.yml
```

> Los documentos se nombran en español por consistencia (`ARCHITECTURE.md` → `ARQUITECTURA.md`,
> etc.). `README.md` y `API.md` conservan su nombre por convención universal. `GLOSARIO.md`
> publica la tabla de la §0; `DECISIONES.md` es el registro conceptual. `MOVIL.md` es además el
> material de aprendizaje de MAUI (ver §7).

### Endpoints (`/api/v1/`)

```
autenticacion/{registrar, iniciar-sesion, renovar, cerrar-sesion,
               cambiar-clave, olvide-clave, restablecer-clave, cambiar-espacio}
usuarios · espacios · espacios/{id}/membresias · invitaciones
cuentas · cuentas/{id}/reconciliar · categorias
movimientos · movimientos/transferencias
gastos-recurrentes · ingresos-recurrentes
presupuestos · metas · metas/{id}/aportes · viajes · viajes/{id}/viabilidad
deudas · reportes/{...} · recomendaciones · panel · notificaciones · monedas · tasas-cambio
administracion/{espacios, usuarios, invitaciones, metricas}   ← solo AdministradorPlataforma
```

---

## 6. Decisiones técnicas

| # | Decisión | Por qué |
|---|---|---|
| 1 | **Todo el código en español, sin tildes ni ñ en identificadores** | Coherencia con el dominio. Se excluyen los nombres impuestos por el framework (§0). Las tildes en identificadores rompen búsquedas, scripts SQL y herramientas CLI; los textos visibles al usuario sí llevan ortografía correcta. |
| 2 | **`Movimiento`, no `Transaccion`** | `Transaccion` colisiona con la transacción de base de datos y con `IDbContextTransaction`; esa ambigüedad cuesta cara en revisiones y en logs. |
| 3 | **`Espacio` como unidad de tenencia** | Neutral respecto a `TipoEspacio` (Personal/Pareja/Familia/Negocio). «Hogar» quedaría contradictorio cuando llegue Negocio. |
| 4 | **ASP.NET Core Identity** con `Usuario : IdentityUser<Guid>` | Hashing PBKDF2 auditado, bloqueo por intentos, tokens de restablecimiento y camino a 2FA sin escribir criptografía. La tenencia vive **fuera** de Identity, en `MembresiaEspacio`. |
| 5 | **Registro cerrado por invitación + `AdministradorPlataforma`** | Sin auto-registro no hay superficie de alta abierta. La invisibilidad del admin es **estructural** (no tiene fila en `MembresiasEspacio`), no un filtro que se pueda olvidar. |
| 6 | **El administrador de plataforma no lee datos financieros** | Mínimo privilegio. Gestiona espacios, usuarios, invitaciones y métricas agregadas mediante `ContextoAdministracion`, que solo expone esos `DbSet`. Sin acceso a movimientos, saldos, metas ni viajes. |
| 7 | **Saldo calculado + instantánea** | `SaldoActual` se actualiza dentro de la **misma transacción de BD** que el movimiento, con `Version` (`rowversion`). `POST /cuentas/{id}/reconciliar` recalcula desde el libro mayor y reporta la desviación. La verdad sigue siendo la suma de asientos; la instantánea es caché verificable. |
| 8 | **Multi-moneda con tasas desde el día 1** | Cada `Movimiento` persiste `Monto` + `Moneda` + `MontoEnMonedaBase` + `TasaCambioAplicada`, **congelados a la fecha del movimiento**. Los reportes agregan sobre `MontoEnMonedaBase`; el histórico no cambia cuando cambia la tasa. Transferencia entre monedas: `MontoOrigen` ≠ `MontoDestino`, con la diferencia explicada por la tasa. |
| 9 | **Servicios + FluentValidation, sin MediatR** | Menos indirección, depuración directa y sin la licencia comercial de MediatR v13+. Se conserva un ámbito transaccional para agrupar operaciones atómicas. |
| 10 | **Transferencia = 2 asientos + `Transferencia`** | `SUM` por cuenta correcto sin casos especiales, y excluir transferencias de ingresos/gastos es un único filtro por `Tipo`. |
| 11 | **`Guid` v7 como PK** | Generable en cliente y servidor, no adivinable, secuencial en el tiempo → no fragmenta el índice agrupado como el v4. |
| 12 | **`DateOnly` contable / `DateTimeOffset` de sistema** | Registrar a las 11 p.m. en RD no debe mover el gasto al mes siguiente. Elimina toda una clase de errores de reporte. |
| 13 | **Borrado lógico en lo financiero** | Auditoría, reversión y reconciliación, vía interceptor + filtro global combinado con el de espacio. |
| 14 | **`IEnviadorCorreo` + `EnviadorCorreoSmtp` con MailKit** | SMTP propio. Se usa **MailKit**, no `System.Net.Mail.SmtpClient` (la propia documentación de .NET lo desaconseja para código nuevo: no soporta bien STARTTLS moderno ni OAuth2). En desarrollo la clave va en **user-secrets**, en producción en **Key Vault**; nunca en `appsettings.json` versionado. Plantillas HTML en español para invitación, restablecimiento y confirmación. |
| 15 | **Motor de recomendaciones = reglas `IReglaRecomendacion`** | Cada regla es una clase testeable que devuelve la recomendación **con sus insumos**. Permite explicar el porqué y añadir IA después como *otra* fuente, sin reescribir nada. |
| 16 | **Las recomendaciones nunca ejecutan movimientos** | `Recomendacion.Estado` = `Pendiente` hasta que el usuario acepta; aceptar crea la transferencia por el endpoint normal, con auditoría. |
| 17 | **Autorización por permisos, no por rol** | `[RequierePermiso(Permisos.Presupuestos.Escribir)]` + mapa rol→permisos. Añadir mañana un rol «Observador» no toca ningún controlador. |
| 18 | **`ProblemDetails` (RFC 9457) global** | `IExceptionHandler`; `traceId` en cada respuesta correlacionado con Application Insights; sin trazas de pila en Producción. Mensajes al usuario en español. |
| 19 | **Gestión central de paquetes** | Una versión por paquete en toda la solución; evita conflictos silenciosos entre API y MAUI. |
| 20 | **Identidad administrada para Key Vault y Azure SQL** | Cero secretos en configuración; la cadena de conexión de producción no lleva contraseña. |
| 21 | **API versionada por URL** (`/api/v1/`) | La app se actualiza tarde (APK por sideload): la v1 debe seguir viva cuando exista v2. |
| 22 | **Nulables + advertencias como errores** | `<Nullable>enable</Nullable>` y `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` en `Directory.Build.props`. |
| 23 | **Cultura `es-DO` por defecto** | Formato `RD$ 50,000.00` y nombres de mes correctos. La API responde `decimal` crudo; el formateo ocurre en MAUI. |
| 24 | **Instancia de SQL Server moderna para desarrollo** | La instancia 2014 existente se conserva intacta. Con una versión moderna el nivel de compatibilidad iguala al de Azure SQL, EF Core 10 queda dentro de su soporte oficial y se dispone de `OPENJSON`, `STRING_AGG`, `AT TIME ZONE` y funciones JSON para `RegistroAuditoria.Cambios` y `Recomendacion.Insumos`. Elimina el riesgo de que algo funcione en producción y falle en local. |

---

## 7. Estilo del código MAUI: simple y didáctico

Requisito explícito: el código móvil debe servir para **aprender**. Eso cambia decisiones reales
en la Fase 8, no solo el tono de los comentarios.

| Decisión | Por qué, para aprender |
|---|---|
| **MVVM escrito a mano, sin `CommunityToolkit.Mvvm`** | El toolkit genera propiedades y comandos con atributos (`[ObservableProperty]`) mediante generadores de código: se escribe menos, pero **no se ve** qué ocurre. Se escribirá una vez `ModeloVistaBase : INotifyPropertyChanged` y `ComandoSimple : ICommand`, ~60 líneas comentadas, y a partir de ahí todo es C# normal y visible. Cuando el patrón ya se domine, migrar al toolkit es mecánico. |
| **Un patrón repetido, sin excepciones** | Cada pantalla es siempre el mismo trío: `XxxPagina.xaml` + `XxxPagina.xaml.cs` (vacío, solo `InitializeComponent`) + `XxxModeloVista.cs`. Una vez entendida una pantalla, se entienden las diez. |
| **La primera pantalla es la plantilla comentada** | `IniciarSesionPagina` se escribe con comentarios línea a línea explicando binding, comando, `IsBusy` y navegación. Las demás la siguen sin repetir esos comentarios. |
| **XAML sencillo: `VerticalStackLayout`, `Grid`, `CollectionView`** | Nada de estilos implícitos complicados, `DataTemplateSelector` ni `ControlTemplate` en v1. Los colores y tamaños viven en `Recursos/Estilos.xaml` como recursos con nombre claro (`ColorPrimario`, `TextoTitulo`). |
| **Sin librerías de UI de terceros** | Solo controles de MAUI. Menos que instalar, menos que depurar, y lo que se aprenda es transferible. |
| **Servicios de API planos** | `ServicioApiMovimientos` con métodos como `Task<List<MovimientoDto>> ObtenerDelMesAsync(int anio, int mes)`. Un `ClienteApi` central maneja el token y la renovación con un `DelegatingHandler` (explicado en `MOVIL.md`, porque es el único punto verdaderamente no obvio). |
| **Manejo de errores visible** | Cada `ModeloVista` tiene `EstaCargando` y `MensajeError`; nada de excepciones silenciosas. Aprender a depurar es parte del objetivo. |
| **`docs/MOVIL.md` como tutorial** | Explica el ciclo de vida de una página, qué es binding, qué hace `BindingContext`, cómo funciona la navegación de Shell y cómo depurar en el dispositivo. Escrito para alguien que no conoce MAUI. |
| **Comentarios en español, explicando el *porqué*** | No `// asigna el nombre`, sino `// El BindingContext conecta esta vista con su ModeloVista: todo {Binding X} del XAML busca la propiedad X aqui.` |

---

## 8. Riesgos identificados

| # | Riesgo | Impacto | Mitigación |
|---|---|---|---|
| R1 | **Fuga entre espacios** por un `IgnoreQueryFilters()`, el módulo de administración o una entidad nueva sin `IEntidadDeEspacio` | Crítico | 5 capas (§3.1) + test de arquitectura que enumera entidades sin filtro + suite de aislamiento bloqueante en CI. |
| R2 | **Desincronización de la instantánea de saldo** | Alto | Actualización en la misma transacción SQL, `rowversion`, endpoint de reconciliación y test que compara instantánea vs `SUM` tras N operaciones concurrentes. |
| R3 | **Transferencia contabilizada como gasto** en algún reporte nuevo | Alto (requisito explícito) | Un único `IQueryable` base `ConsultasMovimientos.SoloGastos()` reutilizado por todos los reportes + test por reporte. |
| R4 | **Doble conteo de aportes a metas** | Alto | Aportes modelados como transferencias; `AporteMeta` referencia el movimiento, no crea dinero. |
| R5 | **Recomendación malinterpretada como consejo financiero** | Reputacional/legal | Escenarios (conservador/esperado/optimista), insumos visibles, aviso legal, nunca imperativo absoluto. |
| R6 | **Recomendaciones sin datos suficientes** (mes 1) | Medio | Con <3 meses de datos el motor marca `ConfianzaBaja` y pide al usuario declarar ingreso/gasto esperado. |
| R7 | **El administrador de plataforma como punto único de fallo** | Alto | Acceso de solo gestión (D6), auditoría reforzada de cada acción, 2FA recomendado para ese rol, y al menos dos administradores para no quedar bloqueado. |
| R8 | **Credenciales SMTP filtradas** | Alto | Nunca en `appsettings.json` versionado: user-secrets en desarrollo, Key Vault en producción. Usar una **clave de aplicación** dedicada, no la clave principal del correo. Reenvío limitado y monitoreado. |
| R9 | **Correo de invitación marcado como spam** → nadie puede entrar | Medio | SPF/DKIM si es dominio propio; el admin puede copiar el enlace de invitación desde el panel de administración como alternativa. |
| R10 | **Azure SQL Serverless con auto-pausa** → primera petición de 30-60 s | Medio (UX) | Tier Basic/S0 provisionado o auto-pausa desactivada en producción; pantalla de carga tolerante en MAUI. |
| R11 | **Arranque en frío de App Service (F1/B1)** | Medio | `Always On` (requiere B1+); F1 solo para pruebas. |
| R12 | **APK por sideload sin actualización automática** | Medio | `GET /api/v1/app/version` + aviso en la app; la v1 de la API nunca se rompe. |
| R13 | **Falta el workload `maui-android`** | Bajo, bloqueante en Fase 8 | `dotnet workload install maui-android` antes de la Fase 8. |
| R14 | **Tasa de cambio ausente** para la fecha de un movimiento | Medio | Respaldo con la última tasa anterior + marcar `TasaAproximada`; si no existe ninguna, error claro en vez de asumir 1:1. |
| R15 | **Deriva entre fases** | Medio | `DECISIONES.md` + `GLOSARIO.md` + `API.md` actualizados al cerrar cada fase. |
| R16 | **SQL Server local antiguo vs Azure SQL en producción** | Alto → **resuelto** | Instancia moderna de SQL Server para desarrollo, lado a lado con la 2014, que queda intacta (decisión 24). |
| R17 | **Concurrencia de pareja** sobre la misma cuenta | Medio | `rowversion` + reintento; el libro mayor es solo-anexar, ningún movimiento se pierde. |
| R18 | **Datos financieros en los logs** | Alto (privacidad) | Nunca montos junto a identificación de usuario; `RegistroAuditoria.Cambios` sin campos sensibles; telemetría con muestreo y sin PII. |
| R19 | **Mezcla español/inglés** con el paso de las fases | Medio | El glosario (§0) es normativo, se publica en `docs/GLOSARIO.md` y se revisa al cerrar cada fase. |

---

## 9. Fases de desarrollo

**Cada fase termina con: código compilable + tests verdes + documentación actualizada +
aprobación explícita antes de continuar.**

| Fase | Contenido | Entregable verificable |
|---|---|---|
| **0** | Análisis y arquitectura | Aprobación |
| **1** | Guía de instalación de SQL Server, solución, 6 proyectos + 3 de pruebas, `Directory.*.props`, `.editorconfig`, `git init` + primer commit, esqueleto de `Program.cs`, Swagger, endpoint de salud, `README.md` + `ARQUITECTURA.md` + `GLOSARIO.md` | `dotnet build` limpio; `GET /salud` → 200; conexión verificada |
| **2** | Dominio completo, `ContextoRumbo`, configuraciones EF, interceptores, semilla (categorías, monedas, tasas), **primera migración**, `BASE-DE-DATOS.md` | `dotnet ef database update` crea la base `Rumbo`; diagrama de tablas |
| **3** | Identity, JWT + renovación con rotación, **invitaciones y `AdministradorPlataforma`**, registrar/iniciar-sesión/cerrar-sesión/renovar/cambiar-clave/olvide-restablecer, espacios, membresías, `IContextoEspacio`, middleware, permisos, **SMTP real funcionando**, **PruebasAislamientoEspacio**, `SEGURIDAD.md` | Invitación por correo de punta a punta + suite de aislamiento verde |
| **4** | Cuentas, Categorías, Movimientos (Ingreso/Gasto/Ajuste), **Transferencias**, Gastos/Ingresos recurrentes, conversión de moneda, reconciliación de saldos, `API.md` | Swagger operativo; test de que transferencia ≠ gasto |
| **5** | Presupuestos con alertas configurables, Metas + aportes, `CalculadoraMetas`, `AnalizadorFlujoCaja`, **`MotorRecomendaciones`** con las primeras reglas | Tests con los ejemplos de referencia (RD$180,000 / 15 meses → RD$10,000/mes) |
| **6** | Viajes, líneas de presupuesto, gastos y fondo del viaje, **«¿Podemos permitirnos este viaje?»** con 3 escenarios, sugerencias ante ingreso extraordinario | `GET /viajes/{id}/viabilidad` con escenarios explicados |
| **7** | `Panel` agregado (una sola petición), reportes con filtros, series mensuales, Deudas + pagos, Notificaciones persistidas | Respuesta de `/panel` que reproduce la pantalla de inicio |
| **8** | MAUI Android **con el estilo didáctico de la §7**: primero `MOVIL.md` + `ModeloVistaBase` + `ComandoSimple` + `ClienteApi` explicados; luego Iniciar sesión → Panel → Movimientos (alta rápida) → Cuentas → Presupuestos → Metas → Viajes → Reportes → Ajustes | APK de depuración en el dispositivo, y poder explicar cómo funciona una pantalla |
| **9** | Revisión OWASP, límite de peticiones, cabeceras de seguridad, validación de entrada, rotación de secretos, cobertura de pruebas críticas, revisión de logs | Informe en `SEGURIDAD.md` |
| **10** | Azure paso a paso: SQL, App Service (B1 + Always On), Key Vault + identidad administrada, App Insights, migraciones, GitHub Actions, HTTPS, `DESPLIEGUE.md` | API publicada y accesible por HTTPS |
| **11** | Keystore, firma, `dotnet publish -f net10.0-android -c Release`, APK firmado, URL de producción por configuración, instalación, y qué haría falta para Google Play | APK release firmado |

---

## 10. Preguntas que quedaron abiertas en la Fase 0

Ninguna bloqueante. Se resolvieron sobre la marcha y quedaron documentadas en
[DECISIONES.md](DECISIONES.md):

- Datos del servidor SMTP (host, puerto, cuenta) — necesarios al empezar la **Fase 3**.
- Correo del primer `AdministradorPlataforma` para la semilla inicial — también en la Fase 3.
