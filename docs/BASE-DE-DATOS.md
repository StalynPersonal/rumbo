# Base de datos

## 1. SQL Server para desarrollo

### Instancia en uso

El entorno de desarrollo usa **SQL Server 2025 Express** en la instancia
`localhost\SQLEXPRESS` (versión 17.0.1000.7). Convive con la instancia por defecto
`MSSQLSERVER`, que tiene SQL Server 2014 y no se toca.

```
Server=localhost\SQLEXPRESS;Database=Rumbo;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

### Por qué no se usa la instancia 2014

Esta máquina ya tiene **SQL Server 2014 Standard** en la instancia por defecto (`MSSQLSERVER`).
No la tocamos: sigue disponible para lo que ya la use.

La instancia 2014 no sirve para este proyecto por dos razones concretas:

1. El proveedor `Microsoft.EntityFrameworkCore.SqlServer` 10 da soporte oficial a **SQL Server 2016
   o superior**. Con 2014 habría que fijar `UseCompatibilityLevel(120)` y renunciar a parte de lo
   que EF Core genera.
2. Producción será **Azure SQL Database**, que corre con nivel de compatibilidad 160. Desarrollar
   contra 2014 significa que algo puede funcionar en un sitio y fallar en el otro; esa clase de
   divergencia siempre aparece tarde.

Con 2022 disponemos además de `OPENJSON`, `STRING_AGG`, `AT TIME ZONE` y las funciones JSON, que
usaremos en `RegistroAuditoria.Cambios` y `Recomendacion.Insumos`.

SQL Server permite varias instancias en la misma máquina sin conflicto, así que las dos conviven.

### Verificación

```powershell
Invoke-Sqlcmd -ServerInstance "localhost\SQLEXPRESS" -Query "SELECT @@VERSION"
```

Debe responder una versión **17.x**. Si `Invoke-Sqlcmd` no está disponible:

```powershell
Install-Module SqlServer -Scope CurrentUser
```

### Aplicar el esquema

```bash
dotnet ef database update --project src/Rumbo.Infraestructura --startup-project src/Rumbo.Api
```

Crea la base `Rumbo` con las 31 tablas y siembra el catálogo de monedas.

Para generar una migración nueva tras cambiar el modelo:

```bash
dotnet ef migrations add NombreDeLaMigracion --project src/Rumbo.Infraestructura --startup-project src/Rumbo.Api --output-dir Persistencia/Migrations
```

`Rumbo.Infraestructura` es el proyecto que contiene el contexto, y `Rumbo.Api` el que tiene
el paquete `Microsoft.EntityFrameworkCore.Design` con las herramientas.

## 2. Modelo de datos

Se documentará al completar la **Fase 2** (entidades, relaciones, índices, restricciones y primera
migración). Los nombres de todas las tablas y columnas siguen el [GLOSARIO.md](GLOSARIO.md).

### Convenciones ya decididas

| Aspecto | Convención | Razón |
|---|---|---|
| Clave primaria | `Guid` con `Guid.CreateVersion7()` | Secuencial en el tiempo, no fragmenta el índice agrupado como el v4, y no es adivinable como un entero |
| Dinero | `decimal(19,4)` | Nunca `float` ni `double`: perderían precisión en los céntimos |
| Moneda | `char(3)` ISO-4217 junto a cada monto | El sistema es multi-moneda desde el primer día |
| Fecha contable | `DateOnly` | Un gasto registrado a las 11 p.m. no debe saltar al mes siguiente por zona horaria |
| Fecha de sistema | `DateTimeOffset` | Instantes reales, con desplazamiento explícito |
| Borrado | Lógico (`Eliminado`) en lo financiero | Los movimientos nunca se borran físicamente: auditoría y reconciliación |
| Concurrencia | `rowversion` en `Cuentas` y `Metas` | Dos personas del mismo hogar pueden escribir a la vez |
| Borrado en cascada | Desactivado (`Restrict`) por defecto | Ninguna cascada debe poder borrar historial financiero |
| Índices | `EspacioId` como primera columna | Rendimiento y aislamiento visible en el plan de ejecución |

## 3. Tablas creadas

31 tablas, todas con nombre en español (incluidas las de ASP.NET Core Identity).

### Identidad y espacios

| Tabla | Contenido |
|---|---|
| `Usuarios`, `Roles`, `UsuariosRoles`, `UsuariosReclamaciones`, `UsuariosInicioSesion`, `UsuariosTokens`, `RolesReclamaciones` | ASP.NET Core Identity, renombradas |
| `Espacios` | Los hogares, parejas, familias o negocios |
| `MembresiasEspacio` | Quién pertenece a qué espacio y con qué rol |
| `ConfiguracionesEspacio` | Preferencias de cada espacio |
| `Invitaciones` | Códigos de alta, de un solo uso |
| `TokensRenovacion` | Sesiones activas |

### Referencia monetaria (global)

| Tabla | Contenido |
|---|---|
| `Monedas` | Catálogo ISO-4217. Sembrado con DOP, USD, EUR y GBP |
| `TasasCambio` | Tipos de cambio por fecha |

### Núcleo financiero

| Tabla | Contenido |
|---|---|
| `Cuentas` | Bancarias, tarjetas, efectivo, ahorro, inversión |
| `Categorias` | Jerárquicas, de dos niveles |
| `Movimientos` | **El libro mayor.** Todos los asientos del sistema |
| `Transferencias` | Agrupan los dos movimientos de cada traspaso |
| `GastosRecurrentes`, `IngresosRecurrentes` | Obligaciones e ingresos periódicos |

### Planificación

| Tabla | Contenido |
|---|---|
| `Presupuestos`, `LineasPresupuesto` | Plan de gasto por categoría |
| `Metas`, `AportesMeta` | Objetivos de ahorro y sus aportes |
| `Viajes`, `LineasPresupuestoViaje` | Viajes y su presupuesto por partidas |
| `Deudas`, `PagosDeuda` | Obligaciones y su amortización |

### Soporte

| Tabla | Contenido |
|---|---|
| `RegistrosAuditoria` | Quién hizo qué y cuándo |
| `Notificaciones` | Avisos para los miembros |
| `Recomendaciones` | Sugerencias del motor, con sus insumos |

## 4. Aislamiento entre espacios en la base de datos

Cada tabla de negocio tiene una columna `EspacioId` y la usa como **primera columna de sus
índices**. Esto cumple dos funciones a la vez: hace la consulta eficiente y deja el
aislamiento a la vista en el plan de ejecución de SQL Server.

El filtro no se escribe en cada consulta. `ContextoRumbo` recorre las entidades del dominio y
aplica automáticamente dos filtros globales con nombre (una función de EF Core 10):

| Filtro | Qué hace |
|---|---|
| `FiltroEspacio` | `EspacioId` debe coincidir con el espacio activo de la petición |
| `FiltroBorradoLogico` | Oculta las filas marcadas como eliminadas |

Tener dos filtros separados, en lugar de uno combinado, permite desactivar uno sin el otro:
la reconciliación de saldos necesita ver registros borrados **de su propio espacio**, y eso no
debe obligar a desactivar también el aislamiento.

### Tablas globales

Siete entidades **no** llevan filtro de espacio, y cada excepción está justificada y cubierta
por una prueba de arquitectura que falla si alguien añade una octava sin explicarla:

| Tabla | Por qué es global |
|---|---|
| `Espacios` | Es la tabla raíz; no puede filtrarse por sí misma |
| `MembresiasEspacio` | Se consulta *antes* de saber cuál es el espacio activo |
| `Invitaciones` | Se canjean sin sesión ni espacio |
| `TokensRenovacion` | Pertenecen al usuario, no al hogar |
| `Monedas`, `TasasCambio` | Datos públicos, idénticos para todos |
| `RegistrosAuditoria` | Registra también acciones sin espacio; se filtra de forma explícita |

## 5. Integridad de los datos

### 29 restricciones CHECK

La base de datos rechaza por sí misma los datos imposibles, sin depender de que la aplicación
valide bien. Las más importantes:

| Restricción | Qué impide |
|---|---|
| `CK_Movimientos_MontoPositivo` | Un importe negativo o cero. La dirección la marca `Signo`, no el signo del importe |
| `CK_Movimientos_Signo` | Un signo distinto de `+1` o `-1` |
| `CK_Transferencias_CuentasDistintas` | Transferir dinero de una cuenta a sí misma |
| `CK_PagosDeuda_DesgloseCuadra` | Que capital + interés + cargos no sume el total pagado |
| `CK_Metas_ObjetivoPositivo` | Una meta de cero, que daría una división por cero |
| `CK_Viajes_Fechas` | Un viaje que termina antes de empezar |
| `CK_ConfiguracionesEspacio_Umbrales` | Umbrales de presupuesto en orden incoherente |

Los días del mes se limitan a 1–28 (`DiaCorte`, `DiaPago`, `DiaVencimiento`, `DiaInicioMes`)
porque es el último día que existe en **todos** los meses: permitir el 31 obligaría a decidir
qué hacer en febrero.

### Borrado

Todas las claves foráneas usan `Restrict` salvo tres casos: `ConfiguracionesEspacio`,
`LineasPresupuesto` y `LineasPresupuestoViaje`, que sí se borran en cascada con su padre
porque no contienen dinero ni historial, solo cifras previstas.

Borrar en cascada un movimiento o una cuenta destruiría la trazabilidad. Por eso, además,
todas las entidades financieras usan **borrado lógico**: `Remove` se convierte
automáticamente en una marca, y la fila permanece.

### Concurrencia

`Cuentas` y `Metas` llevan una columna `Version` (`rowversion`). Si dos miembros del hogar
registran un movimiento sobre la misma cuenta a la vez, el segundo guardado falla con un error
de concurrencia en lugar de pisar el saldo calculado por el primero.

## 6. Nota sobre las tasas de cambio

**La semilla no incluye tasas de cambio.** Inventar un tipo de cambio sería fabricar un dato
financiero: cualquier importe convertido con él saldría mal y el usuario no tendría forma de
saberlo.

Las tasas se cargan explícitamente. Cuando falte la de una fecha, el sistema usará la anterior
más cercana y marcará el movimiento con `TasaEsAproximada`; si no existe ninguna, rechazará la
operación con un mensaje claro en lugar de suponer una paridad de 1 a 1.


## 7. Migraciones aplicadas

| Migración | Qué añade |
|---|---|
| `MigracionInicial` | Todo el esquema: identidad, espacios, libro mayor y planificación |
| `AgregarTipoEspacioPropuestoAInvitacion` | El tipo de espacio se decide al invitar, no después |
| `AgregarConfiguracionDeCorreo` | SMTP de plataforma y por espacio, con la clave cifrada |
| `AgregarPartidaDeViajeAlMovimiento` | `Movimientos.CategoriaViaje`, para comparar el gasto real de un viaje **partida a partida** y no solo contra el total |

`Movimientos.CategoriaViaje` es nulable y solo tiene sentido junto a `ViajeId`. La API rechaza
una partida sin viaje: sería un dato huérfano que después aparece en un informe de viajes sin
pertenecer a ninguno.
