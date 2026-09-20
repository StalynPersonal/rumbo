using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Invitaciones;
using Rumbo.Infraestructura.Identidad;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// Gestion de la plataforma: dar de alta propietarios de espacios nuevos.
/// </summary>
/// <remarks>
/// <para>
/// Reservado al rol <c>AdministradorPlataforma</c>, que NO pertenece a ningun espacio y por
/// tanto no aparece en el listado de miembros de ninguno: no tiene fila en
/// <c>MembresiasEspacio</c>, que es de donde se construye ese listado.
/// </para>
/// <para>
/// <b>Este rol no puede leer datos financieros.</b> Aqui solo hay gestion de altas. No existe
/// ningun endpoint que devuelva movimientos, saldos, metas ni viajes de un hogar, y el
/// administrador tampoco los obtendria por los controladores normales, porque la
/// autorizacion de esos exige un rol dentro del espacio, que el no tiene.
/// </para>
/// </remarks>
/// <param name="invitaciones">Servicio de invitaciones.</param>
/// <param name="usuarioActual">Identidad de quien llama.</param>
[ApiController]
[Route("api/v1/administracion")]
[Produces("application/json")]
[Authorize(Roles = RolesPlataforma.AdministradorPlataforma)]
public class AdministracionController(
    IServicioInvitaciones invitaciones,
    IUsuarioActual usuarioActual) : ControllerBase
{
    /// <summary>Invita a alguien a crear su propio espacio.</summary>
    /// <param name="solicitud">Correo, nombre del espacio y tipo.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La invitacion creada, con su codigo.</returns>
    /// <remarks>
    /// Es la unica forma de que entre alguien nuevo en Rumbo: no hay registro publico.
    /// </remarks>
    [HttpPost("invitaciones")]
    [ProducesResponseType<InvitacionCreada>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InvitacionCreada>> InvitarPropietario(
        [FromBody] SolicitudInvitarPropietario solicitud,
        CancellationToken cancelacion)
    {
        var creada = await invitaciones.InvitarPropietarioAsync(
            solicitud, usuarioActual.ObtenerUsuarioObligatorio(), cancelacion);

        return CreatedAtAction(nameof(ListarInvitaciones), new { }, creada);
    }

    /// <summary>Lista las invitaciones de propietario emitidas.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Las invitaciones, SIN sus codigos.</returns>
    [HttpGet("invitaciones")]
    [ProducesResponseType<IReadOnlyList<InvitacionResumen>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InvitacionResumen>>> ListarInvitaciones(
        CancellationToken cancelacion) =>
        Ok(await invitaciones.ListarDePlataformaAsync(cancelacion));

    /// <summary>Anula una invitacion de propietario pendiente.</summary>
    /// <param name="id">Invitacion que se quiere anular.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Sin contenido.</returns>
    [HttpDelete("invitaciones/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevocarInvitacion(Guid id, CancellationToken cancelacion)
    {
        await invitaciones.RevocarAsync(id, espacioId: null, cancelacion);

        return NoContent();
    }
}
