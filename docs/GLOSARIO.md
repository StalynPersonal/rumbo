# Glosario de Rumbo

**Este documento es normativo.** Ninguna fase puede introducir un nombre distinto para un concepto
que ya está aquí. Si aparece un concepto nuevo, se añade a esta tabla *antes* de escribir el código
que lo usa, y se anota la razón en [DECISIONES.md](DECISIONES.md).

Motivo: evitar que el proyecto acabe mezclando español e inglés, o con dos nombres para la misma
idea (`Transaccion` en un sitio, `Movimiento` en otro).

## Regla de idioma

Todo el código va en **español**, con identificadores **sin tildes ni ñ**:

| Correcto | Incorrecto |
|---|---|
| `Categoria` | `Categoría`, `Category` |
| `Anio` | `Año`, `Year` |
| `MetodoPago` | `MétodoPago`, `PaymentMethod` |
| `Movimiento` | `Movimiénto`, `Transaction` |

Los textos que ve el usuario (mensajes de error, correos, etiquetas de la app) **sí** llevan
ortografía correcta: «La categoría no existe».

## Términos del dominio

| Concepto (inglés) | Nombre canónico | Nota |
|---|---|---|
| Tenant | **`Espacio`** | La unidad de aislamiento. Un hogar, una pareja, una familia o un negocio. Se eligió «Espacio» y no «Hogar» porque `TipoEspacio` incluye `Negocio`. |
| Tenant membership | `MembresiaEspacio` | Relación entre un usuario y un espacio, con su rol. |
| User | `Usuario` | |
| Platform admin | `AdministradorPlataforma` | Rol de Identity (plataforma), **no** un `RolEspacio`. No tiene fila en `MembresiasEspacio`. |
| Account | `Cuenta` | Cuenta bancaria, tarjeta, efectivo, ahorro o inversión. |
| Transaction | **`Movimiento`** | Asiento del libro mayor. Se evita `Transaccion` porque choca con la transacción de base de datos (`IDbContextTransaction`). |
| Transfer | `Transferencia` | Agrupa los **dos** movimientos de un traspaso entre cuentas. |
| Category | `Categoria` | Jerárquica (padre/hijo). |
| Budget / budget line | `Presupuesto` / `LineaPresupuesto` | |
| Goal / contribution | `Meta` / `AporteMeta` | |
| Trip / trip budget line | `Viaje` / `LineaPresupuestoViaje` | |
| Recurring expense / income | `GastoRecurrente` / `IngresoRecurrente` | |
| Debt / debt payment | `Deuda` / `PagoDeuda` | |
| Currency / exchange rate | `Moneda` / `TasaCambio` | |
| Invitation | `Invitacion` | `TipoInvitacion`: `Propietario` \| `Miembro`. |
| Refresh token | `TokenRenovacion` | |
| Audit log | `RegistroAuditoria` | |
| Recommendation | `Recomendacion` | |
| Notification | `Notificacion` | |
| Dashboard | `Panel` | Endpoint `/api/v1/panel`. |

## Enumeraciones

| Enum | Valores |
|---|---|
| `TipoEspacio` | `Personal`, `Pareja`, `Familia`, `Negocio` |
| `RolEspacio` | `Propietario`, `Administrador`, `Miembro` |
| `TipoCuenta` | `Bancaria`, `TarjetaCredito`, `Efectivo`, `Ahorro`, `Inversion`, `Otra` |
| `TipoMovimiento` | `Ingreso`, `Gasto`, `Transferencia`, `Ajuste` |
| `TipoCategoria` | `Ingreso`, `Gasto`, `Ambos` |
| `TipoReparto` | `Personal`, `Compartido` |
| `Frecuencia` | `Semanal`, `Quincenal`, `Mensual`, `Trimestral`, `Anual` |
| `TipoDeuda` | `TarjetaCredito`, `Prestamo`, `PrestamoPersonal`, `Vehiculo`, `Hipoteca`, `Otra` |
| `CategoriaViaje` | `Vuelos`, `Hospedaje`, `Alimentacion`, `Transporte`, `Actividades`, `Compras`, `Documentos`, `Seguro`, `Otros` |
| `TipoInvitacion` | `Propietario`, `Miembro` |

## Nombres que NO se traducen

Los impone el framework y cambiarlos rompería la compilación o las convenciones de ASP.NET Core:

`Program.cs` · sufijo `Controller` · sufijo `Attribute` · `IdentityUser<>` · `IdentityRole<>` ·
`IdentityDbContext<>` · `DbContext` · `DbSet` · `OnModelCreating` · `SaveChangesAsync` ·
carpeta `Migrations` · `appsettings.json` · `Platforms/Android`

Nuestras propias clases sí van en español, incluido `ContextoRumbo : IdentityDbContext<...>`.
