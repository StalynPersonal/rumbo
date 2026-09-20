using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Correo;
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
/// <param name="correo">Servicio de configuracion de correo.</param>
/// <param name="usuarioActual">Identidad de quien llama.</param>
[ApiController]
[Route("api/v1/administracion")]
[Produces("application/json")]
[Authorize(Roles = RolesPlataforma.AdministradorPlataforma)]
public class AdministracionController(
    IServicioInvitaciones invitaciones,
    IServicioConfiguracionCorreo correo,
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

    /// <summary>Devuelve el servidor de correo de la plataforma.</summary>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La configuración, sin la contraseña.</returns>
    /// <remarks>
    /// Este servidor envía lo que no pertenece a ningún hogar: la invitación a un futuro
    /// propietario, cuyo espacio todavía no existe, y el restablecimiento de contraseña, que
    /// pertenece a la persona y no a un espacio.
    /// </remarks>
    [HttpGet("correo")]
    [ProducesResponseType<ConfiguracionCorreoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ConfiguracionCorreoDto>> ObtenerCorreo(
        CancellationToken cancelacion) =>
        Ok(await correo.ObtenerPlataformaAsync(cancelacion));

    /// <summary>Guarda el servidor de correo de la plataforma.</summary>
    /// <param name="solicitud">Datos del servidor.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La configuración guardada, sin la contraseña.</returns>
    /// <remarks>
    /// Dejar la contraseña vacía conserva la que ya estuviera guardada. La contraseña se
    /// cifra antes de almacenarse y la API no la devuelve nunca.
    /// </remarks>
    [HttpPut("correo")]
    [ProducesResponseType<ConfiguracionCorreoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ConfiguracionCorreoDto>> GuardarCorreo(
        [FromBody] SolicitudGuardarCorreo solicitud,
        CancellationToken cancelacion) =>
        Ok(await correo.GuardarPlataformaAsync(solicitud, cancelacion));

    /// <summary>Comprueba que el servidor de la plataforma funciona.</summary>
    /// <param name="solicitud">Dirección de prueba, opcional.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El resultado de la prueba.</returns>
    [HttpPost("correo/probar")]
    [ProducesResponseType<ResultadoPruebaCorreo>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResultadoPruebaCorreo>> ProbarCorreo(
        [FromBody] SolicitudProbarCorreo solicitud,
        CancellationToken cancelacion) =>
        Ok(await correo.ProbarPlataformaAsync(solicitud, cancelacion));
}
