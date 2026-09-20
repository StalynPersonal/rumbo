using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Api.Autorizacion;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Reportes;
using Rumbo.Dominio.Autorizacion;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// Informes del hogar, calculados siempre desde el libro mayor.
/// </summary>
/// <remarks>
/// Ninguno cuenta las transferencias: mover dinero entre cuentas propias no es ingresar ni
/// gastar. Todos suman por el equivalente en la moneda base, congelado a la fecha del
/// movimiento, así que un mes ya cerrado no cambia de resultado si mañana se mueve el tipo
/// de cambio.
/// </remarks>
/// <param name="reportes">Servicio de informes.</param>
[ApiController]
[Route("api/v1/reportes")]
[Produces("application/json")]
[Authorize]
public class ReportesController(IServicioReportes reportes) : ControllerBase
{
    /// <summary>Totales de ingresos y gastos de un período.</summary>
    /// <param name="filtro">Período y filtros.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El resumen del período.</returns>
    /// <remarks>
    /// Sin fechas se toman los últimos doce meses.
    /// </remarks>
    [HttpGet("resumen")]
    [RequierePermiso(Permisos.Reportes.Leer)]
    [ProducesResponseType<ResumenPeriodo>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResumenPeriodo>> Resumen(
        [FromQuery] FiltroReporte filtro,
        CancellationToken cancelacion) =>
        Ok(await reportes.ResumirAsync(filtro, cancelacion));

    /// <summary>Gasto desglosado por categoría.</summary>
    /// <param name="filtro">Período y filtros.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El informe, de mayor a menor gasto.</returns>
    [HttpGet("categorias")]
    [RequierePermiso(Permisos.Reportes.Leer)]
    [ProducesResponseType<ReporteCategorias>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReporteCategorias>> Categorias(
        [FromQuery] FiltroReporte filtro,
        CancellationToken cancelacion) =>
        Ok(await reportes.PorCategoriaAsync(filtro, cancelacion));

    /// <summary>Evolución mes a mes.</summary>
    /// <param name="filtro">Período y filtros.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La serie mensual y sus promedios.</returns>
    /// <remarks>
    /// Incluye los meses sin movimientos: un mes vacío es información, y saltárselo
    /// deformaría la gráfica.
    /// </remarks>
    [HttpGet("mensual")]
    [RequierePermiso(Permisos.Reportes.Leer)]
    [ProducesResponseType<ReporteMensual>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReporteMensual>> Mensual(
        [FromQuery] FiltroReporte filtro,
        CancellationToken cancelacion) =>
        Ok(await reportes.PorMesAsync(filtro, cancelacion));

    /// <summary>Quién puso cuánto en los gastos compartidos.</summary>
    /// <param name="filtro">Período y filtros.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El reparto y el desbalance de cada persona.</returns>
    /// <remarks>
    /// Rumbo <b>registra y reporta</b> el desbalance, pero no liquida entre personas ni mueve
    /// dinero por su cuenta. Quién salda y cómo es una conversación del hogar.
    /// </remarks>
    [HttpGet("reparto")]
    [RequierePermiso(Permisos.Reportes.Leer)]
    [ProducesResponseType<ReporteReparto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReporteReparto>> Reparto(
        [FromQuery] FiltroReporte filtro,
        CancellationToken cancelacion) =>
        Ok(await reportes.RepartoAsync(filtro, cancelacion));
}
