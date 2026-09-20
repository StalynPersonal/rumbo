using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Usuarios;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// Perfil de la persona autenticada.
/// </summary>
/// <remarks>
/// <b>Solo el propio perfil.</b> No hay ninguna ruta que reciba un identificador de usuario:
/// una persona no tiene por qué poder consultar los datos de otra, ni siquiera dentro de su
/// mismo hogar. Para ver a los demás miembros está
/// <c>GET /espacios/actual/miembros</c>, que devuelve lo justo.
/// </remarks>
/// <param name="usuarios">Servicio de perfil.</param>
[ApiController]
[Route("api/v1/usuarios")]
[Produces("application/json")]
[Authorize]
public class UsuariosController(IServicioUsuarios usuarios) : ControllerBase
{
    /// <summary>Devuelve el perfil de quien llama.</summary>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Su perfil.</returns>
    [HttpGet("yo")]
    [ProducesResponseType<PerfilUsuario>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PerfilUsuario>> ObtenerMiPerfil(CancellationToken cancelacion) =>
        Ok(await usuarios.ObtenerPerfilAsync(UsuarioActual(), cancelacion));

    /// <summary>Cambia el nombre y la cultura de quien llama.</summary>
    /// <param name="solicitud">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El perfil actualizado.</returns>
    /// <remarks>
    /// El correo no se cambia aquí: es la credencial de acceso, y modificarlo sin verificar
    /// la dirección nueva permitiría secuestrar una cuenta. Esa operación necesitaría su
    /// propio flujo con confirmación por correo.
    /// </remarks>
    [HttpPut("yo")]
    [ProducesResponseType<PerfilUsuario>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PerfilUsuario>> ActualizarMiPerfil(
        [FromBody] SolicitudActualizarPerfil solicitud,
        CancellationToken cancelacion) =>
        Ok(await usuarios.ActualizarPerfilAsync(UsuarioActual(), solicitud, cancelacion));

    /// <summary>Identificador de quien llama, tomado del token firmado.</summary>
    /// <returns>El identificador.</returns>
    private Guid UsuarioActual() =>
        Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"),
            out var id)
            ? id
            : throw new InvalidOperationException("El token no contiene un usuario válido.");
}
