using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Api.Autorizacion;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Monedas;
using Rumbo.Dominio.Autorizacion;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// Catálogo de monedas y tasas de cambio.
/// </summary>
/// <remarks>
/// <para>
/// Son datos <b>globales</b>: el catálogo ISO-4217 y las cotizaciones son información
/// pública, no de un hogar concreto.
/// </para>
/// <para>
/// Cualquier miembro puede consultarlos. Solo quien administra un espacio puede registrar
/// tasas: una cotización equivocada distorsiona los informes de todo el hogar.
/// </para>
/// </remarks>
/// <param name="monedas">Servicio de monedas.</param>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
[Authorize]
public class MonedasController(IServicioMonedas monedas) : ControllerBase
{
    /// <summary>Lista las monedas disponibles.</summary>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El catálogo de monedas.</returns>
    [HttpGet("monedas")]
    [ProducesResponseType<IReadOnlyList<MonedaDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MonedaDto>>> ListarMonedas(
        CancellationToken cancelacion) =>
        Ok(await monedas.ListarMonedasAsync(cancelacion));

    /// <summary>Lista las tasas registradas para un par de monedas.</summary>
    /// <param name="origen">Código de partida, por ejemplo USD.</param>
    /// <param name="destino">Código de destino, por ejemplo DOP.</param>
    /// <param name="desde">Fecha inicial, opcional.</param>
    /// <param name="hasta">Fecha final, opcional.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Las tasas, de la más reciente a la más antigua.</returns>
    [HttpGet("tasas-cambio")]
    [ProducesResponseType<IReadOnlyList<TasaCambioDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TasaCambioDto>>> ListarTasas(
        [FromQuery] string origen,
        [FromQuery] string destino,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        CancellationToken cancelacion) =>
        Ok(await monedas.ListarTasasAsync(origen, destino, desde, hasta, cancelacion));

    /// <summary>Registra o actualiza una tasa de cambio.</summary>
    /// <param name="solicitud">Par de monedas, fecha y valor.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La tasa guardada.</returns>
    /// <remarks>
    /// Si ya existe una tasa para ese par y esa fecha, se <b>actualiza</b>. Dos valores para
    /// el mismo día harían que el mismo movimiento se convirtiera distinto según cuál se
    /// leyera.
    /// </remarks>
    [HttpPost("tasas-cambio")]
    [RequierePermiso(Permisos.Espacio.Escribir)]
    [ProducesResponseType<TasaCambioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TasaCambioDto>> GuardarTasa(
        [FromBody] SolicitudGuardarTasa solicitud,
        CancellationToken cancelacion) =>
        Ok(await monedas.GuardarTasaAsync(solicitud, cancelacion));

    /// <summary>Elimina una tasa registrada.</summary>
    /// <param name="id">Tasa que se elimina.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Sin contenido.</returns>
    /// <remarks>
    /// Los movimientos ya registrados no cambian: guardan su importe convertido y la tasa
    /// aplicada, congelados en el momento del registro.
    /// </remarks>
    [HttpDelete("tasas-cambio/{id:guid}")]
    [RequierePermiso(Permisos.Espacio.Escribir)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EliminarTasa(Guid id, CancellationToken cancelacion)
    {
        await monedas.EliminarTasaAsync(id, cancelacion);

        return NoContent();
    }

    /// <summary>Convierte un importe entre dos monedas.</summary>
    /// <param name="monto">Importe de partida.</param>
    /// <param name="origen">Moneda de partida.</param>
    /// <param name="destino">Moneda de destino.</param>
    /// <param name="fecha">Fecha de la conversión.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El importe convertido, con la tasa usada.</returns>
    /// <remarks>
    /// Sirve para previsualizar en la aplicación antes de registrar un movimiento en otra
    /// moneda. Devuelve también si la tasa fue aproximada.
    /// </remarks>
    [HttpGet("tasas-cambio/convertir")]
    [ProducesResponseType<ResultadoConversionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResultadoConversionDto>> Convertir(
        [FromQuery] decimal monto,
        [FromQuery] string origen,
        [FromQuery] string destino,
        [FromQuery] DateOnly fecha,
        CancellationToken cancelacion) =>
        Ok(await monedas.ConvertirAsync(monto, origen, destino, fecha, cancelacion));
}
