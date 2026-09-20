using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Api.Autorizacion;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Presupuestos;
using Rumbo.Dominio.Autorizacion;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// Presupuestos por período y el consumo real de cada partida.
/// </summary>
/// <remarks>
/// Un presupuesto no impide registrar un gasto. Compara lo planificado con lo realmente
/// gastado y avisa cuando una categoría se acerca al límite.
/// </remarks>
/// <param name="presupuestos">Servicio de presupuestos.</param>
[ApiController]
[Route("api/v1/presupuestos")]
[Produces("application/json")]
[Authorize]
public class PresupuestosController(IServicioPresupuestos presupuestos) : ControllerBase
{
    /// <summary>Lista los presupuestos con el estado de sus partidas.</summary>
    /// <param name="soloVigente">Si solo se devuelve el que cubre la fecha de hoy.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Los presupuestos, del más reciente al más antiguo.</returns>
    [HttpGet]
    [RequierePermiso(Permisos.Presupuestos.Leer)]
    [ProducesResponseType<IReadOnlyList<PresupuestoDetalle>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PresupuestoDetalle>>> Listar(
        [FromQuery] bool soloVigente = false,
        CancellationToken cancelacion = default) =>
        Ok(await presupuestos.ListarAsync(soloVigente, cancelacion));

    /// <summary>Devuelve un presupuesto.</summary>
    /// <param name="id">Presupuesto buscado.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El presupuesto con el consumo de cada partida.</returns>
    [HttpGet("{id:guid}")]
    [RequierePermiso(Permisos.Presupuestos.Leer)]
    [ProducesResponseType<PresupuestoDetalle>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PresupuestoDetalle>> Obtener(
        Guid id,
        CancellationToken cancelacion) =>
        Ok(await presupuestos.ObtenerAsync(id, cancelacion));

    /// <summary>Crea un presupuesto con sus partidas.</summary>
    /// <param name="solicitud">Datos del presupuesto.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El presupuesto creado.</returns>
    [HttpPost]
    [RequierePermiso(Permisos.Presupuestos.Escribir)]
    [ProducesResponseType<PresupuestoDetalle>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PresupuestoDetalle>> Crear(
        [FromBody] SolicitudGuardarPresupuesto solicitud,
        CancellationToken cancelacion)
    {
        var creado = await presupuestos.CrearAsync(solicitud, cancelacion);

        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    /// <summary>Modifica un presupuesto y sus partidas.</summary>
    /// <param name="id">Presupuesto que se modifica.</param>
    /// <param name="solicitud">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El presupuesto actualizado.</returns>
    /// <remarks>
    /// Las partidas se reemplazan en bloque por las que envíes. El gasto real no se toca:
    /// vive en los movimientos, no en el presupuesto.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [RequierePermiso(Permisos.Presupuestos.Escribir)]
    [ProducesResponseType<PresupuestoDetalle>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PresupuestoDetalle>> Actualizar(
        Guid id,
        [FromBody] SolicitudGuardarPresupuesto solicitud,
        CancellationToken cancelacion) =>
        Ok(await presupuestos.ActualizarAsync(id, solicitud, cancelacion));

    /// <summary>Elimina un presupuesto.</summary>
    /// <param name="id">Presupuesto que se elimina.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Sin contenido.</returns>
    [HttpDelete("{id:guid}")]
    [RequierePermiso(Permisos.Presupuestos.Escribir)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        await presupuestos.EliminarAsync(id, cancelacion);

        return NoContent();
    }
}
