using System.Globalization;

using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Aplicacion.Modulos.Movimientos;
using Rumbo.Contratos.Reportes;
using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Reportes;

/// <summary>
/// Informes del espacio activo, calculados siempre desde el libro mayor.
/// </summary>
/// <remarks>
/// <para>
/// Todos los informes parten de <c>ConsultasMovimientos</c>, que excluye las transferencias
/// de una vez y para siempre. Es la unica forma de que la regla «una transferencia no es un
/// gasto» no se rompa cada vez que se escribe un informe nuevo.
/// </para>
/// <para>
/// Se suma por <c>MontoEnMonedaBase</c>, congelado a la fecha del movimiento, para que un mes
/// ya cerrado no cambie de resultado si manana se mueve el tipo de cambio.
/// </para>
/// </remarks>
/// <param name="contexto">Acceso a los datos.</param>
/// <param name="directorio">Traductor de identificadores de usuario a nombres.</param>
/// <param name="contextoEspacio">Espacio activo.</param>
/// <param name="fechaHora">Proveedor de fecha y hora.</param>
public class ServicioReportes(
    IContextoRumbo contexto,
    IDirectorioUsuarios directorio,
    IContextoEspacio contextoEspacio,
    IProveedorFechaHora fechaHora) : IServicioReportes
{
    /// <summary>Cultura con la que se escriben los nombres de mes.</summary>
    private static readonly CultureInfo Espanol = new("es-DO");

    /// <inheritdoc />
    public async Task<ResumenPeriodo> ResumirAsync(
        FiltroReporte filtro,
        CancellationToken cancelacion = default)
    {
        var (desde, hasta) = await ResolverPeriodoAsync(filtro, cancelacion);
        var consulta = Aplicar(filtro, desde, hasta);

        var totales = await consulta
            .GroupBy(m => m.Signo)
            .Select(g => new
            {
                Signo = g.Key,
                Total = g.Sum(m => m.MontoEnMonedaBase),
                Cantidad = g.Count(),
            })
            .ToListAsync(cancelacion);

        var ingresos = totales.FirstOrDefault(t => t.Signo == 1)?.Total ?? 0m;
        var gastos = totales.FirstOrDefault(t => t.Signo == -1)?.Total ?? 0m;
        var cantidad = totales.Sum(t => t.Cantidad);

        var tasaAhorro = ingresos <= 0m
            ? 0m
            : Redondear((ingresos - gastos) / ingresos * 100m);

        return new ResumenPeriodo(
            desde, hasta, ingresos, gastos, Redondear(ingresos - gastos),
            tasaAhorro, cantidad, await ObtenerMonedaBaseAsync(cancelacion));
    }

    /// <inheritdoc />
    public async Task<ReporteCategorias> PorCategoriaAsync(
        FiltroReporte filtro,
        CancellationToken cancelacion = default)
    {
        var (desde, hasta) = await ResolverPeriodoAsync(filtro, cancelacion);
        var resumen = await ResumirAsync(filtro with { Desde = desde, Hasta = hasta }, cancelacion);

        var agrupado = await Aplicar(filtro, desde, hasta)
            .SoloGastos()
            .Where(m => m.CategoriaId != null)
            .GroupBy(m => m.CategoriaId!.Value)
            .Select(g => new
            {
                CategoriaId = g.Key,
                Total = g.Sum(m => m.MontoEnMonedaBase),
                Cantidad = g.Count(),
            })
            .ToListAsync(cancelacion);

        // Los nombres se resuelven en una segunda consulta y no con un Include: asi la
        // agregacion la hace SQL Server sobre indices, sin traerse cada movimiento.
        var ids = agrupado.Select(a => a.CategoriaId).ToList();

        var nombres = await contexto.Categorias
            .AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .Select(c => new
            {
                c.Id,
                c.Nombre,
                NombrePadre = c.CategoriaPadre != null ? c.CategoriaPadre.Nombre : null,
            })
            .ToListAsync(cancelacion);

        var totalGastos = agrupado.Sum(a => a.Total);

        var categorias = agrupado
            .Select(a =>
            {
                var info = nombres.FirstOrDefault(n => n.Id == a.CategoriaId);

                return new TotalPorCategoria(
                    a.CategoriaId,
                    info?.Nombre ?? "(categoría eliminada)",
                    info?.NombrePadre,
                    a.Total,
                    totalGastos <= 0m ? 0m : Redondear(a.Total / totalGastos * 100m),
                    a.Cantidad);
            })
            .OrderByDescending(c => c.Total)
            .ToList();

        return new ReporteCategorias(resumen, categorias);
    }

    /// <inheritdoc />
    public async Task<ReporteMensual> PorMesAsync(
        FiltroReporte filtro,
        CancellationToken cancelacion = default)
    {
        var (desde, hasta) = await ResolverPeriodoAsync(filtro, cancelacion);
        var resumen = await ResumirAsync(filtro with { Desde = desde, Hasta = hasta }, cancelacion);

        var agrupado = await Aplicar(filtro, desde, hasta)
            .GroupBy(m => new { m.FechaMovimiento.Year, m.FechaMovimiento.Month, m.Signo })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Signo,
                Total = g.Sum(m => m.MontoEnMonedaBase),
            })
            .ToListAsync(cancelacion);

        // Se recorren TODOS los meses del periodo, no solo los que tienen movimientos: un
        // mes vacio es informacion, y saltarselo deformaria la grafica.
        var meses = new List<PuntoMensual>();
        var cursor = new DateOnly(desde.Year, desde.Month, 1);
        var ultimo = new DateOnly(hasta.Year, hasta.Month, 1);

        while (cursor <= ultimo)
        {
            var ingresos = agrupado
                .FirstOrDefault(a => a.Year == cursor.Year
                                     && a.Month == cursor.Month && a.Signo == 1)?.Total ?? 0m;

            var gastos = agrupado
                .FirstOrDefault(a => a.Year == cursor.Year
                                     && a.Month == cursor.Month && a.Signo == -1)?.Total ?? 0m;

            meses.Add(new PuntoMensual(
                cursor.Year,
                cursor.Month,
                cursor.ToString("MMMM yyyy", Espanol),
                ingresos,
                gastos,
                Redondear(ingresos - gastos)));

            cursor = cursor.AddMonths(1);
        }

        var divisor = Math.Max(1, meses.Count);

        return new ReporteMensual(
            resumen,
            meses,
            Redondear(meses.Sum(m => m.Ingresos) / divisor),
            Redondear(meses.Sum(m => m.Gastos) / divisor));
    }

    /// <inheritdoc />
    public async Task<ReporteReparto> RepartoAsync(
        FiltroReporte filtro,
        CancellationToken cancelacion = default)
    {
        var (desde, hasta) = await ResolverPeriodoAsync(filtro, cancelacion);

        var pagado = await Aplicar(filtro, desde, hasta)
            .SoloGastos()
            .Where(m => m.Reparto == TipoReparto.Compartido && m.PagadoPorUsuarioId != null)
            .GroupBy(m => m.PagadoPorUsuarioId!.Value)
            .Select(g => new { UsuarioId = g.Key, Total = g.Sum(m => m.MontoEnMonedaBase) })
            .ToListAsync(cancelacion);

        var espacioId = contextoEspacio.ObtenerEspacioObligatorio();

        var miembrosDelEspacio = await contexto.MembresiasEspacio
            .AsNoTracking()
            .Where(m => m.EspacioId == espacioId && m.Estado == EstadoMembresia.Activa)
            .Select(m => m.UsuarioId)
            .ToListAsync(cancelacion);

        // Los nombres se piden al directorio: los usuarios viven en las tablas de Identity,
        // que son cosa de la infraestructura, y esta capa no tiene por que saberlo.
        var nombres = await directorio.ObtenerNombresAsync(miembrosDelEspacio, cancelacion);

        var total = pagado.Sum(p => p.Total);
        var porPersona = miembrosDelEspacio.Count == 0
            ? 0m
            : Redondear(total / miembrosDelEspacio.Count);

        var miembros = miembrosDelEspacio
            .Select(id =>
            {
                var puso = pagado.FirstOrDefault(p => p.UsuarioId == id)?.Total ?? 0m;

                return new AporteDeMiembro(
                    id,
                    nombres.TryGetValue(id, out var nombre) ? nombre : "(sin nombre)",
                    puso,
                    porPersona,
                    Redondear(puso - porPersona));
            })
            .OrderByDescending(m => m.TotalPagado)
            .ToList();

        return new ReporteReparto(
            desde, hasta, total, miembros, await ObtenerMonedaBaseAsync(cancelacion));
    }

    /// <summary>Construye la consulta base con los filtros aplicados.</summary>
    /// <param name="filtro">Filtros recibidos.</param>
    /// <param name="desde">Primer dia del periodo.</param>
    /// <param name="hasta">Ultimo dia del periodo.</param>
    /// <returns>La consulta lista para agregar.</returns>
    /// <remarks>
    /// <c>IngresosYGastos()</c> excluye las transferencias. Es el unico punto por el que
    /// pasan todos los informes, y por eso la regla no se puede romper por olvido.
    /// </remarks>
    private IQueryable<Movimiento> Aplicar(FiltroReporte filtro, DateOnly desde, DateOnly hasta)
    {
        var consulta = contexto.Movimientos
            .AsNoTracking()
            .IngresosYGastos()
            .EnRango(desde, hasta);

        if (filtro.CuentaId is { } cuentaId)
        {
            consulta = consulta.Where(m => m.CuentaId == cuentaId);
        }

        if (filtro.CategoriaId is { } categoriaId)
        {
            consulta = consulta.Where(m => m.CategoriaId == categoriaId);
        }

        if (filtro.UsuarioId is { } usuarioId)
        {
            consulta = consulta.Where(m => m.PagadoPorUsuarioId == usuarioId);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Reparto))
        {
            if (!Enum.TryParse<TipoReparto>(filtro.Reparto, ignoreCase: true, out var reparto))
            {
                throw new ExcepcionDominio("El reparto debe ser Personal o Compartido.");
            }

            consulta = consulta.Where(m => m.Reparto == reparto);
        }

        return consulta;
    }

    /// <summary>Completa las fechas que no vengan en el filtro.</summary>
    /// <param name="filtro">Filtros recibidos.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El periodo efectivo.</returns>
    /// <remarks>
    /// Sin fechas se toman los ultimos doce meses completos mas el actual: es el horizonte
    /// que hace util una grafica sin castigar a la base de datos.
    /// </remarks>
    private async Task<(DateOnly Desde, DateOnly Hasta)> ResolverPeriodoAsync(
        FiltroReporte filtro,
        CancellationToken cancelacion)
    {
        var hoy = await ObtenerHoyAsync(cancelacion);

        var hasta = filtro.Hasta ?? hoy;
        var desde = filtro.Desde ?? new DateOnly(hasta.Year, hasta.Month, 1).AddMonths(-11);

        if (desde > hasta)
        {
            throw new ExcepcionDominio("La fecha inicial no puede ser posterior a la final.");
        }

        return (desde, hasta);
    }

    /// <summary>Moneda base del espacio.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El codigo ISO-4217.</returns>
    private async Task<string> ObtenerMonedaBaseAsync(CancellationToken cancelacion)
    {
        var espacioId = contextoEspacio.ObtenerEspacioObligatorio();

        return await contexto.Espacios
            .AsNoTracking()
            .Where(e => e.Id == espacioId)
            .Select(e => e.MonedaBase)
            .FirstOrDefaultAsync(cancelacion) ?? "DOP";
    }

    /// <summary>Fecha de hoy en la zona horaria del espacio.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La fecha actual.</returns>
    private async Task<DateOnly> ObtenerHoyAsync(CancellationToken cancelacion)
    {
        var espacioId = contextoEspacio.ObtenerEspacioObligatorio();

        var zona = await contexto.Espacios
            .AsNoTracking()
            .Where(e => e.Id == espacioId)
            .Select(e => e.ZonaHoraria)
            .FirstOrDefaultAsync(cancelacion);

        return fechaHora.HoyEn(zona ?? "America/Santo_Domingo");
    }

    /// <summary>Redondea a dos decimales con redondeo bancario.</summary>
    /// <param name="valor">Importe.</param>
    /// <returns>El importe redondeado.</returns>
    private static decimal Redondear(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.ToEven);
}
