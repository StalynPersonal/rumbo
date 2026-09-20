using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Api.Autorizacion;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Viajes;
using Rumbo.Dominio.Autorizacion;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// Viajes planificados: su presupuesto, su fondo de ahorro y su viabilidad.
/// </summary>
/// <remarks>
/// Un viaje tiene dos caras. El <b>presupuesto</b> es lo que se piensa gastar, desglosado en
/// partidas. El <b>fondo</b> es lo que se lleva ahorrado para pagarlo, y eso vive en una meta
/// con su cuenta de ahorro. Los gastos del viaje sí son gastos normales del hogar, solo que
/// etiquetados con el viaje y su partida.
/// </remarks>
/// <param name="viajes">Servicio de viajes.</param>
[ApiController]
[Route("api/v1/viajes")]
[Produces("application/json")]
[Authorize]
public class ViajesController(IServicioViajes viajes) : ControllerBase
{
    /// <summary>Lista los viajes del espacio.</summary>
    /// <param name="incluirCerrados">Si se incluyen los finalizados y cancelados.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Los viajes, por fecha de salida.</returns>
    [HttpGet]
    [RequierePermiso(Permisos.Viajes.Leer)]
    [ProducesResponseType<IReadOnlyList<ViajeDetalle>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ViajeDetalle>>> Listar(
        [FromQuery] bool incluirCerrados = false,
        CancellationToken cancelacion = default) =>
        Ok(await viajes.ListarAsync(incluirCerrados, cancelacion));

    /// <summary>Devuelve un viaje.</summary>
    /// <param name="id">Viaje buscado.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El viaje con su desglose y su gasto real.</returns>
    [HttpGet("{id:guid}")]
    [RequierePermiso(Permisos.Viajes.Leer)]
    [ProducesResponseType<ViajeDetalle>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ViajeDetalle>> Obtener(
        Guid id,
        CancellationToken cancelacion) =>
        Ok(await viajes.ObtenerAsync(id, cancelacion));

    /// <summary>Responde si el hogar puede permitirse el viaje.</summary>
    /// <param name="id">Viaje analizado.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El veredicto con sus tres escenarios.</returns>
    /// <remarks>
    /// <para>
    /// Devuelve <b>tres escenarios</b> —conservador, esperado y optimista— en lugar de una
    /// sola cifra. Una respuesta única se lee como una promesa, y el excedente mensual de un
    /// hogar no es constante: un mes hay una reparación, otro una boda.
    /// </para>
    /// <para>
    /// Es una proyección para decidir: <b>no mueve dinero, no crea aportes y no reserva
    /// nada</b>. La decisión sigue siendo de las personas.
    /// </para>
    /// </remarks>
    [HttpGet("{id:guid}/viabilidad")]
    [RequierePermiso(Permisos.Viajes.Leer)]
    [ProducesResponseType<ViabilidadViajeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ViabilidadViajeDto>> Viabilidad(
        Guid id,
        CancellationToken cancelacion) =>
        Ok(await viajes.AnalizarViabilidadAsync(id, cancelacion));

    /// <summary>Crea un viaje con su desglose.</summary>
    /// <param name="solicitud">Datos del viaje.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El viaje creado.</returns>
    /// <remarks>
    /// El presupuesto total no se envía: es la suma de las partidas. Permitir las dos cosas
    /// dejaría abierta la puerta a que no cuadraran.
    /// </remarks>
    [HttpPost]
    [RequierePermiso(Permisos.Viajes.Escribir)]
    [ProducesResponseType<ViajeDetalle>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ViajeDetalle>> Crear(
        [FromBody] SolicitudGuardarViaje solicitud,
        CancellationToken cancelacion)
    {
        var creado = await viajes.CrearAsync(solicitud, cancelacion);

        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    /// <summary>Modifica un viaje y su desglose.</summary>
    /// <param name="id">Viaje que se modifica.</param>
    /// <param name="solicitud">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El viaje actualizado.</returns>
    [HttpPut("{id:guid}")]
    [RequierePermiso(Permisos.Viajes.Escribir)]
    [ProducesResponseType<ViajeDetalle>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ViajeDetalle>> Actualizar(
        Guid id,
        [FromBody] SolicitudGuardarViaje solicitud,
        CancellationToken cancelacion) =>
        Ok(await viajes.ActualizarAsync(id, solicitud, cancelacion));

    /// <summary>Cambia el estado de un viaje.</summary>
    /// <param name="id">Viaje afectado.</param>
    /// <param name="solicitud">Estado nuevo.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El viaje actualizado.</returns>
    [HttpPut("{id:guid}/estado")]
    [RequierePermiso(Permisos.Viajes.Escribir)]
    [ProducesResponseType<ViajeDetalle>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ViajeDetalle>> CambiarEstado(
        Guid id,
        [FromBody] SolicitudCambiarEstadoViaje solicitud,
        CancellationToken cancelacion) =>
        Ok(await viajes.CambiarEstadoAsync(id, solicitud, cancelacion));

    /// <summary>Elimina un viaje que aún no tiene gastos.</summary>
    /// <param name="id">Viaje que se elimina.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Sin contenido.</returns>
    /// <remarks>
    /// Un viaje con gastos no se borra: esos movimientos existen y apuntan a él. Para
    /// retirarlo de la vista, cámbiale el estado a Cancelado.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [RequierePermiso(Permisos.Viajes.Escribir)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        await viajes.EliminarAsync(id, cancelacion);

        return NoContent();
    }
}
