using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Autenticacion;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// Alta, acceso y gestion de la sesion.
/// </summary>
/// <remarks>
/// Es el unico controlador con endpoints anonimos. Todo lo demas en Rumbo exige un token.
/// </remarks>
/// <param name="autenticacion">Servicio de autenticacion.</param>
[ApiController]
[Route("api/v1/autenticacion")]
[Produces("application/json")]
// El limitador aplica a este controlador un cupo estricto —cinco peticiones por minuto y
// por origen— por su ruta, no por un atributo: ver LimitesDePeticiones y docs/SEGURIDAD.md.
public class AutenticacionController(IServicioAutenticacion autenticacion) : ControllerBase
{
    /// <summary>Da de alta una cuenta canjeando un codigo de invitacion.</summary>
    /// <param name="solicitud">Codigo, correo, nombre y contrasena.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Credenciales de la sesion recien iniciada.</returns>
    [HttpPost("registrar")]
    [AllowAnonymous]
    [ProducesResponseType<RespuestaAutenticacion>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RespuestaAutenticacion>> Registrar(
        [FromBody] SolicitudRegistrar solicitud,
        CancellationToken cancelacion) =>
        Ok(await autenticacion.RegistrarAsync(solicitud, cancelacion));

    /// <summary>Inicia sesion con correo y contrasena.</summary>
    /// <param name="solicitud">Credenciales.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Credenciales de la sesion.</returns>
    [HttpPost("iniciar-sesion")]
    [AllowAnonymous]
    [ProducesResponseType<RespuestaAutenticacion>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<RespuestaAutenticacion>> IniciarSesion(
        [FromBody] SolicitudIniciarSesion solicitud,
        CancellationToken cancelacion) =>
        Ok(await autenticacion.IniciarSesionAsync(solicitud, ObtenerIp(), cancelacion));

    /// <summary>Obtiene credenciales nuevas a partir del token de renovacion.</summary>
    /// <param name="solicitud">Token de renovacion vigente.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Credenciales nuevas.</returns>
    [HttpPost("renovar")]
    [AllowAnonymous]
    [ProducesResponseType<RespuestaAutenticacion>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RespuestaAutenticacion>> Renovar(
        [FromBody] SolicitudRenovar solicitud,
        CancellationToken cancelacion) =>
        Ok(await autenticacion.RenovarAsync(solicitud, ObtenerIp(), cancelacion));

    /// <summary>Cierra la sesion revocando su token de renovacion.</summary>
    /// <param name="solicitud">Token que se quiere revocar.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Sin contenido.</returns>
    [HttpPost("cerrar-sesion")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CerrarSesion(
        [FromBody] SolicitudCerrarSesion solicitud,
        CancellationToken cancelacion)
    {
        await autenticacion.CerrarSesionAsync(solicitud, cancelacion);

        return NoContent();
    }

    /// <summary>Cambia la contrasena de la persona autenticada.</summary>
    /// <param name="solicitud">Contrasena actual y nueva.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Sin contenido.</returns>
    /// <remarks>Cambiar la contrasena cierra TODAS las sesiones abiertas.</remarks>
    [HttpPost("cambiar-clave")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CambiarClave(
        [FromBody] SolicitudCambiarClave solicitud,
        CancellationToken cancelacion)
    {
        await autenticacion.CambiarClaveAsync(ObtenerUsuarioId(), solicitud, cancelacion);

        return NoContent();
    }

    /// <summary>Pide un enlace para restablecer la contrasena.</summary>
    /// <param name="solicitud">Correo de la cuenta.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Sin contenido.</returns>
    /// <remarks>
    /// Responde 204 SIEMPRE, exista o no la cuenta. Distinguir ambos casos permitiria
    /// averiguar quien usa Rumbo probando direcciones de correo.
    /// </remarks>
    [HttpPost("olvide-clave")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> OlvideClave(
        [FromBody] SolicitudOlvideClave solicitud,
        CancellationToken cancelacion)
    {
        await autenticacion.SolicitarRestablecerClaveAsync(solicitud, cancelacion);

        return NoContent();
    }

    /// <summary>Fija una contrasena nueva con el codigo recibido por correo.</summary>
    /// <param name="solicitud">Correo, codigo y contrasena nueva.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Sin contenido.</returns>
    [HttpPost("restablecer-clave")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RestablecerClave(
        [FromBody] SolicitudRestablecerClave solicitud,
        CancellationToken cancelacion)
    {
        await autenticacion.RestablecerClaveAsync(solicitud, cancelacion);

        return NoContent();
    }

    /// <summary>Cambia de espacio activo sin volver a iniciar sesion.</summary>
    /// <param name="solicitud">Espacio al que se quiere cambiar.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Credenciales nuevas con el espacio cambiado.</returns>
    [HttpPost("cambiar-espacio")]
    [Authorize]
    [ProducesResponseType<RespuestaAutenticacion>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RespuestaAutenticacion>> CambiarEspacio(
        [FromBody] SolicitudCambiarEspacio solicitud,
        CancellationToken cancelacion) =>
        Ok(await autenticacion.CambiarEspacioAsync(
            ObtenerUsuarioId(), solicitud, ObtenerIp(), cancelacion));

    /// <summary>Identificador del usuario autenticado, tomado del token firmado.</summary>
    /// <returns>El identificador.</returns>
    private Guid ObtenerUsuarioId() =>
        Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"),
            out var id)
            ? id
            : throw new InvalidOperationException("El token no contiene un usuario válido.");

    /// <summary>Direccion IP del cliente, para la auditoria.</summary>
    /// <returns>La IP, o <c>null</c> si no se puede determinar.</returns>
    private string? ObtenerIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
