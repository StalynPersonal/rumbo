using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Api.Autorizacion;
using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Correo;
using Rumbo.Dominio.Autorizacion;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// Servidor de correo propio del espacio.
/// </summary>
/// <remarks>
/// <para>
/// Lo configura el propietario con su propia cuenta, para que las invitaciones a su pareja o
/// a su familia lleguen desde su dirección y no desde una genérica.
/// </para>
/// <para>
/// <b>Reservado al propietario.</b> Ni siquiera un administrador del hogar puede tocarlo: no
/// son credenciales del sistema, son las del correo personal de alguien.
/// </para>
/// <para>
/// <b>La contraseña nunca se devuelve.</b> Se guarda cifrada y no existe ningún endpoint que
/// la exponga, ni siquiera a quien la puso. Las respuestas solo indican si hay una guardada.
/// </para>
/// </remarks>
/// <param name="configuracion">Servicio de configuración de correo.</param>
/// <param name="contextoEspacio">Espacio activo de la petición.</param>
[ApiController]
[Route("api/v1/espacios/actual/correo")]
[Produces("application/json")]
[Authorize]
public class CorreoEspacioController(
    IServicioConfiguracionCorreo configuracion,
    IContextoEspacio contextoEspacio) : ControllerBase
{
    /// <summary>Devuelve el servidor de correo del espacio.</summary>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La configuración, sin la contraseña.</returns>
    [HttpGet]
    [RequierePermiso(Permisos.Espacio.ConfigurarCorreo)]
    [ProducesResponseType<ConfiguracionCorreoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ConfiguracionCorreoDto>> Obtener(CancellationToken cancelacion) =>
        Ok(await configuracion.ObtenerDelEspacioAsync(Espacio(), cancelacion));

    /// <summary>Guarda el servidor de correo del espacio.</summary>
    /// <param name="solicitud">Datos del servidor.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La configuración guardada, sin la contraseña.</returns>
    /// <remarks>
    /// Dejar la contraseña vacía conserva la que ya estuviera guardada, para poder cambiar el
    /// puerto o el remitente sin volver a escribirla.
    /// </remarks>
    [HttpPut]
    [RequierePermiso(Permisos.Espacio.ConfigurarCorreo)]
    [ProducesResponseType<ConfiguracionCorreoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ConfiguracionCorreoDto>> Guardar(
        [FromBody] SolicitudGuardarCorreo solicitud,
        CancellationToken cancelacion) =>
        Ok(await configuracion.GuardarDelEspacioAsync(Espacio(), solicitud, cancelacion));

    /// <summary>Comprueba que el servidor del espacio funciona.</summary>
    /// <param name="solicitud">Dirección de prueba, opcional.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El resultado de la prueba.</returns>
    /// <remarks>
    /// Sin dirección de prueba solo comprueba la conexión y la autenticación. Con ella,
    /// además envía un mensaje real, que es la única forma de saber con certeza que el correo
    /// llega.
    /// </remarks>
    [HttpPost("probar")]
    [RequierePermiso(Permisos.Espacio.ConfigurarCorreo)]
    [ProducesResponseType<ResultadoPruebaCorreo>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResultadoPruebaCorreo>> Probar(
        [FromBody] SolicitudProbarCorreo solicitud,
        CancellationToken cancelacion) =>
        Ok(await configuracion.ProbarDelEspacioAsync(Espacio(), solicitud, cancelacion));

    private Guid Espacio() => contextoEspacio.ObtenerEspacioObligatorio();
}
