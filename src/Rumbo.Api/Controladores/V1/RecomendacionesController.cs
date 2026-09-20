using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Api.Autorizacion;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Recomendaciones;
using Rumbo.Dominio.Autorizacion;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// Sugerencias que el sistema calcula a partir de los datos del espacio.
/// </summary>
/// <remarks>
/// <para>
/// Las recomendaciones son <b>deterministas</b>: reglas y aritmética, no inteligencia
/// artificial. Cada una guarda los insumos con los que se calculó, así que siempre se puede
/// responder de dónde sale el número.
/// </para>
/// <para>
/// <b>Ninguna recomendación mueve dinero.</b> Aceptarla deja constancia de la decisión; el
/// aporte o el ajuste se registra después por su propia operación, que la persona confirma.
/// </para>
/// </remarks>
/// <param name="recomendaciones">Servicio de recomendaciones.</param>
[ApiController]
[Route("api/v1/recomendaciones")]
[Produces("application/json")]
[Authorize]
public class RecomendacionesController(IServicioRecomendaciones recomendaciones)
    : ControllerBase
{
    /// <summary>Lista las sugerencias del espacio.</summary>
    /// <param name="incluirRespondidas">Si se incluyen las aceptadas y descartadas.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Las sugerencias, de la más reciente a la más antigua.</returns>
    [HttpGet]
    [RequierePermiso(Permisos.Recomendaciones.Leer)]
    [ProducesResponseType<IReadOnlyList<RecomendacionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecomendacionDto>>> Listar(
        [FromQuery] bool incluirRespondidas = false,
        CancellationToken cancelacion = default) =>
        Ok(await recomendaciones.ListarAsync(incluirRespondidas, cancelacion));

    /// <summary>Vuelve a calcular las sugerencias con los datos de hoy.</summary>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Las sugerencias vigentes tras el recálculo.</returns>
    /// <remarks>
    /// Las pendientes anteriores se sustituyen. Las que ya respondiste se conservan: son tu
    /// historial de decisiones, y volver a proponerte algo que rechazaste sería molesto.
    /// </remarks>
    [HttpPost("recalcular")]
    [RequierePermiso(Permisos.Recomendaciones.Leer)]
    [ProducesResponseType<IReadOnlyList<RecomendacionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecomendacionDto>>> Recalcular(
        CancellationToken cancelacion) =>
        Ok(await recomendaciones.RecalcularAsync(cancelacion));

    /// <summary>Acepta o descarta una sugerencia.</summary>
    /// <param name="id">Sugerencia respondida.</param>
    /// <param name="solicitud">Aceptada o Descartada.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La sugerencia con su estado nuevo.</returns>
    [HttpPut("{id:guid}/respuesta")]
    [RequierePermiso(Permisos.Recomendaciones.Responder)]
    [ProducesResponseType<RecomendacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecomendacionDto>> Responder(
        Guid id,
        [FromBody] SolicitudResponderRecomendacion solicitud,
        CancellationToken cancelacion) =>
        Ok(await recomendaciones.ResponderAsync(id, solicitud, cancelacion));
}
