using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Api.Autorizacion;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Notificaciones;
using Rumbo.Dominio.Autorizacion;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// Avisos del hogar: pagos que vencen, presupuestos al límite y metas alcanzadas.
/// </summary>
/// <remarks>
/// Los avisos se guardan aunque todavía no exista transporte push. Cuando se enchufe, el
/// historial ya estará ahí y no habrá que inventarlo.
/// </remarks>
/// <param name="notificaciones">Servicio de avisos.</param>
[ApiController]
[Route("api/v1/notificaciones")]
[Produces("application/json")]
[Authorize]
public class NotificacionesController(IServicioNotificaciones notificaciones) : ControllerBase
{
    /// <summary>Lista los avisos del espacio.</summary>
    /// <param name="soloSinLeer">Si solo se devuelven los pendientes de leer.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Los avisos, del más reciente al más antiguo.</returns>
    [HttpGet]
    [RequierePermiso(Permisos.Notificaciones.Leer)]
    [ProducesResponseType<IReadOnlyList<NotificacionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificacionDto>>> Listar(
        [FromQuery] bool soloSinLeer = false,
        CancellationToken cancelacion = default) =>
        Ok(await notificaciones.ListarAsync(soloSinLeer, cancelacion));

    /// <summary>Revisa el estado del hogar y genera los avisos que procedan.</summary>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Cuántos se generaron y cuántos quedan sin leer.</returns>
    /// <remarks>
    /// No duplica: si ya existe un aviso del mismo tipo sobre lo mismo y sin leer, no se crea
    /// otro. Recibir cinco veces «el recibo de la luz vence pronto» hace que se dejen de leer
    /// todos.
    /// </remarks>
    [HttpPost("generar")]
    [RequierePermiso(Permisos.Notificaciones.Leer)]
    [ProducesResponseType<ResultadoGeneracionAvisos>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoGeneracionAvisos>> Generar(
        CancellationToken cancelacion) =>
        Ok(await notificaciones.GenerarAsync(cancelacion));

    /// <summary>Marca un aviso como leído.</summary>
    /// <param name="id">Aviso afectado.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El aviso actualizado.</returns>
    [HttpPut("{id:guid}/leida")]
    [RequierePermiso(Permisos.Notificaciones.Gestionar)]
    [ProducesResponseType<NotificacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificacionDto>> MarcarComoLeida(
        Guid id,
        CancellationToken cancelacion) =>
        Ok(await notificaciones.MarcarComoLeidaAsync(id, cancelacion));

    /// <summary>Marca como leídos todos los avisos pendientes.</summary>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Cuántos se marcaron.</returns>
    [HttpPut("leidas")]
    [RequierePermiso(Permisos.Notificaciones.Gestionar)]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> MarcarTodasComoLeidas(
        CancellationToken cancelacion) =>
        Ok(await notificaciones.MarcarTodasComoLeidasAsync(cancelacion));

    /// <summary>Descarta un aviso.</summary>
    /// <param name="id">Aviso afectado.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Sin contenido.</returns>
    /// <remarks>
    /// Se descarta, no se borra: saber que se avisó y que la persona lo apartó permite medir
    /// si los avisos sirven de algo.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [RequierePermiso(Permisos.Notificaciones.Gestionar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Descartar(Guid id, CancellationToken cancelacion)
    {
        await notificaciones.DescartarAsync(id, cancelacion);

        return NoContent();
    }
}
