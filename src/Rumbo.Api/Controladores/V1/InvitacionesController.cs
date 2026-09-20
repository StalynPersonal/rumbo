using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Api.Autorizacion;
using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Invitaciones;
using Rumbo.Dominio.Autorizacion;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// Invitaciones para unirse a un espacio.
/// </summary>
/// <remarks>
/// Las invitaciones para CREAR un espacio nuevo no estan aqui: las emite un administrador de
/// plataforma desde <c>/api/v1/administracion/invitaciones</c>.
/// </remarks>
/// <param name="invitaciones">Servicio de invitaciones.</param>
/// <param name="contextoEspacio">Espacio activo de la peticion.</param>
/// <param name="usuarioActual">Identidad de quien llama.</param>
[ApiController]
[Route("api/v1/invitaciones")]
[Produces("application/json")]
[Authorize]
public class InvitacionesController(
    IServicioInvitaciones invitaciones,
    IContextoEspacio contextoEspacio,
    IUsuarioActual usuarioActual) : ControllerBase
{
    /// <summary>Invita a alguien a unirse al espacio activo.</summary>
    /// <param name="solicitud">Correo y rol que tendra.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La invitacion creada, con su codigo.</returns>
    /// <remarks>
    /// El codigo se devuelve UNA sola vez. Si el correo no llega, puede compartirse a mano
    /// desde aqui; despues ya no vuelve a estar disponible.
    /// </remarks>
    [HttpPost]
    [RequierePermiso(Permisos.Espacio.Invitar)]
    [ProducesResponseType<InvitacionCreada>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InvitacionCreada>> Invitar(
        [FromBody] SolicitudInvitarMiembro solicitud,
        CancellationToken cancelacion)
    {
        var creada = await invitaciones.InvitarMiembroAsync(
            solicitud,
            contextoEspacio.ObtenerEspacioObligatorio(),
            usuarioActual.ObtenerUsuarioObligatorio(),
            cancelacion);

        return CreatedAtAction(nameof(Listar), new { }, creada);
    }

    /// <summary>Lista las invitaciones del espacio activo.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Las invitaciones, SIN sus codigos.</returns>
    [HttpGet]
    [RequierePermiso(Permisos.Espacio.Leer)]
    [ProducesResponseType<IReadOnlyList<InvitacionResumen>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InvitacionResumen>>> Listar(
        CancellationToken cancelacion) =>
        Ok(await invitaciones.ListarDelEspacioAsync(
            contextoEspacio.ObtenerEspacioObligatorio(), cancelacion));

    /// <summary>Anula una invitacion pendiente del espacio activo.</summary>
    /// <param name="id">Invitacion que se quiere anular.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Sin contenido.</returns>
    [HttpDelete("{id:guid}")]
    [RequierePermiso(Permisos.Espacio.Invitar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revocar(Guid id, CancellationToken cancelacion)
    {
        // Se pasa el espacio activo para que solo puedan anularse invitaciones propias:
        // conocer el identificador de una ajena no debe bastar para tocarla.
        await invitaciones.RevocarAsync(id, contextoEspacio.ObtenerEspacioObligatorio(), cancelacion);

        return NoContent();
    }
}
