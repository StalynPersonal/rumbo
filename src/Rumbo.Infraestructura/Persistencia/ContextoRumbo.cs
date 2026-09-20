using System.Linq.Expressions;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Comun;
using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Entidades.Identidad;
using Rumbo.Dominio.Entidades.Planificacion;
using Rumbo.Dominio.Entidades.Soporte;
using Rumbo.Infraestructura.Identidad;

namespace Rumbo.Infraestructura.Persistencia;

/// <summary>
/// Contexto de Entity Framework Core de Rumbo. Es el unico punto por el que la aplicacion
/// habla con la base de datos.
/// </summary>
/// <remarks>
/// <para>
/// Ademas de exponer las tablas, este contexto aplica por si mismo las dos garantias mas
/// importantes del sistema, de forma que no dependan de que nadie se acuerde de nada:
/// </para>
/// <list type="number">
/// <item><description>
/// <b>Aislamiento entre espacios.</b> Toda entidad que implemente
/// <see cref="IEntidadDeEspacio"/> recibe automaticamente un filtro global por
/// <c>EspacioId</c>.
/// </description></item>
/// <item><description>
/// <b>Borrado logico.</b> Las entidades marcadas como eliminadas desaparecen de las consultas,
/// pero siguen en la base de datos para la auditoria y la reconciliacion de saldos.
/// </description></item>
/// </list>
/// <para>
/// Los filtros se aplican recorriendo el modelo en <see cref="OnModelCreating"/>, y no
/// escribiendolos uno a uno. Si se escribieran a mano, anadir una entidad nueva y olvidar su
/// filtro provocaria una fuga de datos entre hogares sin que nada avisara.
/// </para>
/// </remarks>
/// <param name="opciones">Opciones de configuracion del contexto.</param>
/// <param name="contextoEspacio">Espacio activo de la peticion.</param>
public class ContextoRumbo(
    DbContextOptions<ContextoRumbo> opciones,
    IContextoEspacio contextoEspacio)
    : IdentityDbContext<Usuario, Rol, Guid>(opciones)
{
    /// <summary>
    /// Espacio activo de la peticion, expuesto para que el filtro global pueda leerlo.
    /// </summary>
    /// <remarks>
    /// Es publica porque el arbol de expresiones del filtro accede a ella en cada consulta;
    /// un campo privado no seria alcanzable desde la expresion compilada.
    /// </remarks>
    public IContextoEspacio ContextoEspacio { get; } = contextoEspacio;

    /// <summary>Espacios (hogares, parejas, familias o negocios) del sistema.</summary>
    public DbSet<Espacio> Espacios => Set<Espacio>();

    /// <summary>Pertenencia de cada usuario a cada espacio, con su rol.</summary>
    public DbSet<MembresiaEspacio> MembresiasEspacio => Set<MembresiaEspacio>();

    /// <summary>Invitaciones emitidas para registrarse o unirse a un espacio.</summary>
    public DbSet<Invitacion> Invitaciones => Set<Invitacion>();

    /// <summary>Tokens de renovacion de sesion.</summary>
    public DbSet<TokenRenovacion> TokensRenovacion => Set<TokenRenovacion>();

    /// <summary>Preferencias de cada espacio.</summary>
    public DbSet<ConfiguracionEspacio> ConfiguracionesEspacio => Set<ConfiguracionEspacio>();

    /// <summary>Catalogo de monedas admitidas. Tabla global.</summary>
    public DbSet<Moneda> Monedas => Set<Moneda>();

    /// <summary>Tasas de cambio historicas. Tabla global.</summary>
    public DbSet<TasaCambio> TasasCambio => Set<TasaCambio>();

    /// <summary>Cuentas donde se guarda o se mueve el dinero.</summary>
    public DbSet<Cuenta> Cuentas => Set<Cuenta>();

    /// <summary>Categorias de clasificacion, jerarquicas.</summary>
    public DbSet<Categoria> Categorias => Set<Categoria>();

    /// <summary>Libro mayor: todos los asientos del sistema.</summary>
    public DbSet<Movimiento> Movimientos => Set<Movimiento>();

    /// <summary>Traspasos entre cuentas, que agrupan sus dos movimientos.</summary>
    public DbSet<Transferencia> Transferencias => Set<Transferencia>();

    /// <summary>Obligaciones recurrentes.</summary>
    public DbSet<GastoRecurrente> GastosRecurrentes => Set<GastoRecurrente>();

    /// <summary>Ingresos recurrentes.</summary>
    public DbSet<IngresoRecurrente> IngresosRecurrentes => Set<IngresoRecurrente>();

    /// <summary>Presupuestos por periodo.</summary>
    public DbSet<Presupuesto> Presupuestos => Set<Presupuesto>();

    /// <summary>Partidas de cada presupuesto.</summary>
    public DbSet<LineaPresupuesto> LineasPresupuesto => Set<LineaPresupuesto>();

    /// <summary>Metas de ahorro.</summary>
    public DbSet<Meta> Metas => Set<Meta>();

    /// <summary>Aportes realizados a las metas.</summary>
    public DbSet<AporteMeta> AportesMeta => Set<AporteMeta>();

    /// <summary>Viajes planificados.</summary>
    public DbSet<Viaje> Viajes => Set<Viaje>();

    /// <summary>Partidas del presupuesto de cada viaje.</summary>
    public DbSet<LineaPresupuestoViaje> LineasPresupuestoViaje => Set<LineaPresupuestoViaje>();

    /// <summary>Deudas pendientes.</summary>
    public DbSet<Deuda> Deudas => Set<Deuda>();

    /// <summary>Pagos aplicados a las deudas.</summary>
    public DbSet<PagoDeuda> PagosDeuda => Set<PagoDeuda>();

    /// <summary>Historial de auditoria.</summary>
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

    /// <summary>Avisos para los miembros del espacio.</summary>
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    /// <summary>Sugerencias del motor de recomendaciones.</summary>
    public DbSet<Recomendacion> Recomendaciones => Set<Recomendacion>();
