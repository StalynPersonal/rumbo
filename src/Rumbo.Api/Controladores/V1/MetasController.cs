using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Api.Autorizacion;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Metas;
using Rumbo.Dominio.Autorizacion;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// Metas de ahorro del espacio y los aportes que las alimentan.
/// </summary>
/// <remarks>
/// Una meta no guarda dinero: el dinero vive en una cuenta. La meta dice cuánto se quiere
/// reunir y para cuándo, y calcula cuánto haría falta aportar cada mes para llegar.
/// </remarks>
/// <param name="metas">Servicio de metas.</param>
[ApiController]
[Route("api/v1/metas")]
[Produces("application/json")]
[Authorize]
public class MetasController(IServicioMetas metas) : ControllerBase
{
    /// <summary>Lista las metas con su proyección.</summary>
    /// <param name="incluirCerradas">Si se incluyen las alcanzadas y canceladas.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Las metas, por prioridad y fecha objetivo.</returns>
    [HttpGet]
    [RequierePermiso(Permisos.Metas.Leer)]
    [ProducesResponseType<IReadOnlyList<MetaDetalle>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MetaDetalle>>> Listar(
        [FromQuery] bool incluirCerradas = false,
        CancellationToken cancelacion = default) =>
        Ok(await metas.ListarAsync(incluirCerradas, cancelacion));

    /// <summary>Devuelve una meta.</summary>
    /// <param name="id">Meta buscada.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La meta con su proyección.</returns>
    [HttpGet("{id:guid}")]
    [RequierePermiso(Permisos.Metas.Leer)]
    [ProducesResponseType<MetaDetalle>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MetaDetalle>> Obtener(
        Guid id,
        CancellationToken cancelacion) =>
        Ok(await metas.ObtenerAsync(id, cancelacion));

    /// <summary>Crea una meta.</summary>
    /// <param name="solicitud">Datos de la meta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La meta creada.</returns>
    [HttpPost]
    [RequierePermiso(Permisos.Metas.Escribir)]
    [ProducesResponseType<MetaDetalle>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MetaDetalle>> Crear(
        [FromBody] SolicitudGuardarMeta solicitud,
        CancellationToken cancelacion)
    {
        var creada = await metas.CrearAsync(solicitud, cancelacion);

        return CreatedAtAction(nameof(Obtener), new { id = creada.Id }, creada);
    }

    /// <summary>Modifica una meta.</summary>
    /// <param name="id">Meta que se modifica.</param>
    /// <param name="solicitud">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La meta actualizada.</returns>
    [HttpPut("{id:guid}")]
    [RequierePermiso(Permisos.Metas.Escribir)]
    [ProducesResponseType<MetaDetalle>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MetaDetalle>> Actualizar(
        Guid id,
        [FromBody] SolicitudGuardarMeta solicitud,
        CancellationToken cancelacion) =>
        Ok(await metas.ActualizarAsync(id, solicitud, cancelacion));

    /// <summary>Pausa, reactiva o cancela una meta.</summary>
    /// <param name="id">Meta afectada.</param>
    /// <param name="solicitud">Estado nuevo.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La meta actualizada.</returns>
    [HttpPut("{id:guid}/estado")]
    [RequierePermiso(Permisos.Metas.Escribir)]
    [ProducesResponseType<MetaDetalle>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MetaDetalle>> CambiarEstado(
        Guid id,
        [FromBody] SolicitudCambiarEstadoMeta solicitud,
        CancellationToken cancelacion) =>
        Ok(await metas.CambiarEstadoAsync(id, solicitud, cancelacion));

    /// <summary>Elimina una meta que aún no tiene aportes.</summary>
    /// <param name="id">Meta que se elimina.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Sin contenido.</returns>
    /// <remarks>
    /// Una meta con aportes no se borra: esos movimientos existen y apuntan a ella.
    /// Para retirarla de la vista, cámbiale el estado a Cancelada.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [RequierePermiso(Permisos.Metas.Escribir)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        await metas.EliminarAsync(id, cancelacion);

        return NoContent();
    }

    /// <summary>Lista los aportes de una meta.</summary>
    /// <param name="id">Meta consultada.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Los aportes, del más reciente al más antiguo.</returns>
    [HttpGet("{id:guid}/aportes")]
    [RequierePermiso(Permisos.Metas.Leer)]
    [ProducesResponseType<IReadOnlyList<AporteMetaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AporteMetaDto>>> ListarAportes(
        Guid id,
        CancellationToken cancelacion) =>
        Ok(await metas.ListarAportesAsync(id, cancelacion));

    /// <summary>Aporta dinero a una meta desde una cuenta.</summary>
    /// <param name="id">Meta que recibe el dinero.</param>
    /// <param name="solicitud">Cuenta de origen, importe y fecha.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La meta con su progreso actualizado.</returns>
    /// <remarks>
    /// El aporte se registra como <b>transferencia</b> hacia la cuenta de ahorro de la meta,
    /// nunca como gasto: ahorrar no empobrece el mes, solo cambia el dinero de sitio.
    /// </remarks>
    [HttpPost("{id:guid}/aportes")]
    [RequierePermiso(Permisos.Metas.Escribir)]
    [ProducesResponseType<MetaDetalle>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MetaDetalle>> Aportar(
        Guid id,
        [FromBody] SolicitudAportarAMeta solicitud,
        CancellationToken cancelacion) =>
        Ok(await metas.AportarAsync(id, solicitud, cancelacion));
}
