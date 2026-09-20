using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Entidades.Identidad;
using Rumbo.Dominio.Entidades.Planificacion;
using Rumbo.Dominio.Entidades.Soporte;

namespace Rumbo.Aplicacion.Comun;

/// <summary>
/// Acceso a los datos desde la capa de aplicacion.
/// </summary>
/// <remarks>
/// <para>
/// Expone las tablas del dominio, pero <b>no</b> las de ASP.NET Core Identity: los servicios
/// financieros no tienen nada que hacer con usuarios y contrasenas.
/// </para>
/// <para>
/// <b>Por que una interfaz y no el contexto directamente.</b> Los servicios de negocio viven
/// en esta capa, que no puede referenciar a Infraestructura. Con esta abstraccion escriben
/// consultas LINQ normales sin conocer el contexto concreto ni el proveedor de base de datos.
/// </para>
/// <para>
/// <b>Lo que NO cambia.</b> La implementacion sigue siendo <c>ContextoRumbo</c>, asi que
/// todas sus garantias siguen vigentes: los filtros globales por espacio, el borrado logico y
/// los interceptores se aplican igual. Esta interfaz no es una puerta trasera al aislamiento.
/// </para>
/// </remarks>
public interface IContextoRumbo
{
    /// <summary>Espacios del sistema.</summary>
    DbSet<Espacio> Espacios { get; }

    /// <summary>Pertenencia de cada usuario a cada espacio.</summary>
    DbSet<MembresiaEspacio> MembresiasEspacio { get; }

    /// <summary>Preferencias de cada espacio.</summary>
    DbSet<ConfiguracionEspacio> ConfiguracionesEspacio { get; }

    /// <summary>Catalogo de monedas.</summary>
    DbSet<Moneda> Monedas { get; }

    /// <summary>Tasas de cambio historicas.</summary>
    DbSet<TasaCambio> TasasCambio { get; }

    /// <summary>Cuentas del espacio.</summary>
    DbSet<Cuenta> Cuentas { get; }

    /// <summary>Categorias del espacio.</summary>
    DbSet<Categoria> Categorias { get; }

    /// <summary>Libro mayor.</summary>
    DbSet<Movimiento> Movimientos { get; }

    /// <summary>Traspasos entre cuentas.</summary>
    DbSet<Transferencia> Transferencias { get; }

    /// <summary>Gastos recurrentes.</summary>
    DbSet<GastoRecurrente> GastosRecurrentes { get; }

    /// <summary>Ingresos recurrentes.</summary>
    DbSet<IngresoRecurrente> IngresosRecurrentes { get; }

    /// <summary>Presupuestos.</summary>
    DbSet<Presupuesto> Presupuestos { get; }

    /// <summary>Partidas de presupuesto.</summary>
    DbSet<LineaPresupuesto> LineasPresupuesto { get; }

    /// <summary>Metas de ahorro.</summary>
    DbSet<Meta> Metas { get; }

    /// <summary>Aportes a las metas.</summary>
    DbSet<AporteMeta> AportesMeta { get; }

    /// <summary>Viajes.</summary>
    DbSet<Viaje> Viajes { get; }

    /// <summary>Partidas del presupuesto de cada viaje.</summary>
    DbSet<LineaPresupuestoViaje> LineasPresupuestoViaje { get; }

    /// <summary>Deudas.</summary>
    DbSet<Deuda> Deudas { get; }

    /// <summary>Pagos de deuda.</summary>
    DbSet<PagoDeuda> PagosDeuda { get; }

    /// <summary>Notificaciones.</summary>
    DbSet<Notificacion> Notificaciones { get; }

    /// <summary>Recomendaciones del motor.</summary>
    DbSet<Recomendacion> Recomendaciones { get; }

    /// <summary>Guarda los cambios pendientes.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Cantidad de filas afectadas.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Ejecuta una operacion dentro de una transaccion de base de datos.
    /// </summary>
    /// <typeparam name="T">Tipo del resultado.</typeparam>
    /// <param name="operacion">Trabajo que debe aplicarse por completo o no aplicarse.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Lo que devuelva la operacion.</returns>
    /// <remarks>
    /// <para>
    /// Imprescindible para lo que toca varias tablas y debe cuadrar siempre: una
    /// transferencia crea dos movimientos y mueve dos saldos, y aplicar solo la mitad
    /// dejaria el libro mayor descuadrado.
    /// </para>
    /// <para>
    /// <b>Por que un metodo y no devolver la transaccion.</b> La conexion usa reintentos
    /// ante fallos transitorios, habituales en Azure SQL. Esa estrategia NO admite
    /// transacciones abiertas a mano: si se cortara la conexion a mitad, el reintento no
    /// sabria que rehacer. Envolviendo la operacion, todo el bloque se reintenta como una
    /// unidad. Exponer solo este metodo hace imposible cometer ese error.
    /// </para>
    /// </remarks>
    Task<T> EjecutarEnTransaccionAsync<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Da acceso a la informacion de seguimiento de entidades de EF Core.
    /// </summary>
    /// <returns>El rastreador de cambios.</returns>
    DatabaseFacade BaseDeDatos { get; }
}
