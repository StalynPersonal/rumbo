using Rumbo.Contratos.Reportes;

namespace Rumbo.Aplicacion.Contratos;

/// <summary>
/// Informes del espacio, calculados siempre desde el libro mayor.
/// </summary>
/// <remarks>
/// Ninguno cuenta las transferencias: mover dinero entre cuentas propias no es ingresar ni
/// gastar.
/// </remarks>
public interface IServicioReportes
{
    /// <summary>Totales de ingresos y gastos de un periodo.</summary>
    /// <param name="filtro">Periodo y filtros.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El resumen del periodo.</returns>
    Task<ResumenPeriodo> ResumirAsync(
        FiltroReporte filtro,
        CancellationToken cancelacion = default);

    /// <summary>Gasto desglosado por categoria.</summary>
    /// <param name="filtro">Periodo y filtros.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El informe, de mayor a menor gasto.</returns>
    Task<ReporteCategorias> PorCategoriaAsync(
        FiltroReporte filtro,
        CancellationToken cancelacion = default);

    /// <summary>Evolucion mes a mes.</summary>
    /// <param name="filtro">Periodo y filtros.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La serie mensual y sus promedios.</returns>
    /// <remarks>
    /// Incluye los meses sin movimientos: un mes vacio es informacion, y saltarselo
    /// deformaria la grafica.
    /// </remarks>
    Task<ReporteMensual> PorMesAsync(
        FiltroReporte filtro,
        CancellationToken cancelacion = default);

    /// <summary>Quien puso cuanto en los gastos compartidos.</summary>
    /// <param name="filtro">Periodo y filtros.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El reparto y el desbalance de cada persona.</returns>
    /// <remarks>
    /// Se <b>registra y reporta</b> el desbalance, pero no se liquida: quien salda y como es
    /// una conversacion del hogar, no una decision de la aplicacion.
    /// </remarks>
    Task<ReporteReparto> RepartoAsync(
        FiltroReporte filtro,
        CancellationToken cancelacion = default);
}
