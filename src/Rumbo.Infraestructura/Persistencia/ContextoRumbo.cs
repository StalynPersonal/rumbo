using System.Linq.Expressions;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

using Rumbo.Aplicacion.Comun;
using Rumbo.Dominio;
using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Entidades.Identidad;
using Rumbo.Dominio.Entidades.Planificacion;
using Rumbo.Dominio.Entidades.Soporte;
using Rumbo.Infraestructura.Identidad;
using Rumbo.Infraestructura.Persistencia.Semilla;

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
    : IdentityDbContext<Usuario, Rol, Guid>(opciones), IContextoRumbo
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

    /// <summary>Servidor SMTP de la plataforma. Tabla global de una sola fila.</summary>
    public DbSet<ConfiguracionCorreoPlataforma> ConfiguracionCorreoPlataforma =>
        Set<ConfiguracionCorreoPlataforma>();

    /// <summary>Servidor SMTP propio de cada espacio.</summary>
    public DbSet<ConfiguracionCorreoEspacio> ConfiguracionesCorreoEspacio =>
        Set<ConfiguracionCorreoEspacio>();

    /// <summary>Nombre del filtro global que aisla los datos por espacio.</summary>
    public const string FiltroEspacio = "FiltroEspacio";

    /// <summary>Nombre del filtro global que oculta los registros borrados logicamente.</summary>
    public const string FiltroBorradoLogico = "FiltroBorradoLogico";

    /// <inheritdoc />
    public DatabaseFacade BaseDeDatos => Database;

    // SaveChangesAsync no se declara aqui: el que hereda de DbContext ya cumple el
    // contrato de IContextoRumbo, y redeclararlo solo ocultaria el original.

    /// <inheritdoc />
    public async Task<T> EjecutarEnTransaccionAsync<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancelacion = default)
    {
        // La estrategia de reintentos envuelve TODO el bloque, de modo que si la conexion
        // se corta a mitad, la operacion completa se rehace desde el principio.
        var estrategia = Database.CreateExecutionStrategy();

        return await estrategia.ExecuteAsync(async () =>
        {
            await using var transaccion = await Database.BeginTransactionAsync(cancelacion);

            var resultado = await operacion(cancelacion);

            await transaccion.CommitAsync(cancelacion);

            return resultado;
        });
    }

    /// <summary>
    /// Convenciones que se aplican a TODO el modelo antes de las configuraciones por entidad.
    /// </summary>
    /// <param name="configurador">Constructor de convenciones de EF Core.</param>
    /// <remarks>
    /// <para>
    /// <b>Dinero con <c>decimal(19,4)</c>.</b> Se fija aqui una sola vez en lugar de repetirlo
    /// en cada propiedad monetaria. Sin esta convencion, SQL Server usaria <c>decimal(18,2)</c>
    /// por defecto y cualquier propiedad nueva heredaria ese formato sin que nadie lo notase.
    /// Se usan 4 decimales, y no 2, porque los calculos intermedios (prorrateos, conversiones
    /// de moneda, reparto de una cuota entre capital e intereses) pierden precision si se
    /// redondea a centimos en cada paso.
    /// </para>
    /// <para>
    /// <b>Nunca <c>float</c> ni <c>double</c> para dinero.</b> Son binarios y no pueden
    /// representar 0,10 de forma exacta: sumar mil gastos acabaria dando un saldo que no
    /// cuadra con la realidad por centimos.
    /// </para>
    /// <para>
    /// <b>Textos con longitud maxima.</b> Por defecto EF Core generaria <c>nvarchar(max)</c>,
    /// que no puede indexarse y desperdicia espacio. 256 es un limite razonable para nombres y
    /// descripciones; donde haga falta mas, la configuracion de la entidad lo amplia.
    /// </para>
    /// </remarks>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurador)
    {
        base.ConfigureConventions(configurador);

        configurador.Properties<decimal>().HavePrecision(19, 4);
        configurador.Properties<string>().HaveMaxLength(256);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder constructor)
    {
        base.OnModelCreating(constructor);

        // Cada entidad tiene su clase de configuracion en Persistencia/Configuraciones.
        constructor.ApplyConfigurationsFromAssembly(typeof(ContextoRumbo).Assembly);

        // Catalogo global de monedas: forma parte de la migracion para que todo
        // entorno nuevo arranque con los mismos valores.
        SemillaMonedas.Sembrar(constructor);

        AplicarFiltrosGlobales(constructor);
    }

    /// <summary>
    /// Aplica automaticamente el filtro de espacio y el de borrado logico a todas las
    /// entidades del dominio que los necesitan.
    /// </summary>
    /// <param name="constructor">Constructor del modelo de EF Core.</param>
    /// <remarks>
    /// <para>
    /// <b>Por que se descubren las entidades por reflexion sobre el ensamblado del dominio y
    /// NO con <c>constructor.Model.GetEntityTypes()</c>.</b> Enumerar el modelo a medio
    /// construir obliga a EF Core a materializar tipos que todavia no ha terminado de
    /// clasificar. En .NET 10 eso rompe la configuracion de passkeys de ASP.NET Core Identity:
    /// <c>IdentityPasskeyData</c>, que debe ser un tipo complejo, queda registrado como
    /// entidad y el modelo falla al pedirle una clave primaria. Recorrer solo NUESTRAS
    /// entidades evita tocar las de Identity y ademas deja explicito el conjunto al que se
    /// aplica el aislamiento.
    /// </para>
    /// <para>
    /// <b>Por que automatico y no filtro a filtro.</b> Si cada filtro se escribiera a mano,
    /// anadir una entidad nueva y olvidar el suyo provocaria una fuga de datos entre hogares
    /// sin que nada avisara. Asi basta con marcar la entidad con
    /// <see cref="IEntidadDeEspacio"/>.
    /// </para>
    /// <para>
    /// <b>Filtros con nombre.</b> EF Core 10 permite varios filtros por entidad si se les da
    /// nombre. Se usan dos separados para poder desactivar uno sin el otro: la reconciliacion
    /// necesita ver registros borrados de SU espacio, y eso no debe obligar a desactivar
    /// tambien el aislamiento.
    /// </para>
    /// <para>
    /// <b>Como se lee el espacio activo.</b> La expresion referencia la instancia del
    /// contexto. EF Core detecta esa referencia y la sustituye por el contexto que ejecuta
    /// cada consulta, de modo que el filtro no queda congelado con el valor de la primera
    /// peticion aunque el modelo se cachee.
    /// </para>
    /// <para>
    /// <b>Si no hay espacio activo</b> (peticion anonima, inicio de sesion), el valor es
    /// <c>null</c>, la comparacion no encuentra ninguna fila y no se devuelve nada. Ante la
    /// duda, no se muestra informacion.
    /// </para>
    /// </remarks>
    private void AplicarFiltrosGlobales(ModelBuilder constructor)
    {
        var entidadesDelDominio = typeof(MarcadorDominio).Assembly
            .GetTypes()
            .Where(tipo => tipo is { IsClass: true, IsAbstract: false }
                           && (typeof(IEntidadDeEspacio).IsAssignableFrom(tipo)
                               || typeof(IBorradoLogico).IsAssignableFrom(tipo)));

        foreach (var tipoClr in entidadesDelDominio)
        {
            var constructorEntidad = constructor.Entity(tipoClr);
            var parametro = Expression.Parameter(tipoClr, "e");

            if (typeof(IEntidadDeEspacio).IsAssignableFrom(tipoClr))
            {
                // e.EspacioId == contextoActual.ContextoEspacio.EspacioId
                var espacioDeLaFila = Expression.Convert(
                    Expression.Property(parametro, nameof(IEntidadDeEspacio.EspacioId)),
                    typeof(Guid?));

                var espacioActivo = Expression.Property(
                    Expression.Property(Expression.Constant(this), nameof(ContextoEspacio)),
                    nameof(IContextoEspacio.EspacioId));

                constructorEntidad.HasQueryFilter(
                    FiltroEspacio,
                    Expression.Lambda(Expression.Equal(espacioDeLaFila, espacioActivo), parametro));
            }

            if (typeof(IBorradoLogico).IsAssignableFrom(tipoClr))
            {
                // !e.Eliminado
                constructorEntidad.HasQueryFilter(
                    FiltroBorradoLogico,
                    Expression.Lambda(
                        Expression.Not(Expression.Property(parametro, nameof(IBorradoLogico.Eliminado))),
                        parametro));
            }
        }
    }
}
