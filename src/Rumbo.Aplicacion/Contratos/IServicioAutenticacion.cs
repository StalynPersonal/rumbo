using Rumbo.Contratos.Autenticacion;

namespace Rumbo.Aplicacion.Contratos;

/// <summary>
/// Gestiona el ciclo de vida de la sesion: alta, inicio y cierre de sesion, renovacion y
/// cambio de contrasena.
/// </summary>
public interface IServicioAutenticacion
{
    /// <summary>
    /// Da de alta a una persona canjeando un codigo de invitacion.
    /// </summary>
    /// <param name="solicitud">Codigo, correo, nombre y contrasena.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Credenciales de la sesion recien iniciada.</returns>
    /// <remarks>
    /// No existe alta sin invitacion. Si el codigo es de tipo Propietario, ademas se crea el
    /// espacio con sus categorias predeterminadas.
    /// </remarks>
    Task<RespuestaAutenticacion> RegistrarAsync(
        SolicitudRegistrar solicitud,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Inicia sesion con correo y contrasena.
    /// </summary>
    /// <param name="solicitud">Credenciales.</param>
    /// <param name="direccionIp">IP desde la que se conecta, para la auditoria.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Credenciales de la sesion.</returns>
    Task<RespuestaAutenticacion> IniciarSesionAsync(
        SolicitudIniciarSesion solicitud,
        string? direccionIp,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Emite credenciales nuevas a partir de un token de renovacion.
    /// </summary>
    /// <param name="solicitud">Token de renovacion vigente.</param>
    /// <param name="direccionIp">IP desde la que se conecta.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Credenciales nuevas.</returns>
    /// <remarks>
    /// El token ROTA: el anterior queda revocado. Si llega uno ya revocado, se revoca toda su
    /// cadena, porque significa que existen dos copias circulando.
    /// </remarks>
    Task<RespuestaAutenticacion> RenovarAsync(
        SolicitudRenovar solicitud,
        string? direccionIp,
        CancellationToken cancelacion = default);

    /// <summary>Cierra la sesion revocando su token de renovacion.</summary>
    /// <param name="solicitud">Token que se quiere revocar.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando la sesion queda cerrada.</returns>
    Task CerrarSesionAsync(
        SolicitudCerrarSesion solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Cambia la contrasena de la persona autenticada.</summary>
    /// <param name="usuarioId">Usuario que la cambia.</param>
    /// <param name="solicitud">Contrasena actual y nueva.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando el cambio se aplica.</returns>
    /// <remarks>
    /// Revoca TODAS las sesiones abiertas. Si alguien cambia su contrasena es, muchas veces,
    /// porque sospecha que alguien mas la conoce: dejar las demas sesiones vivas anularia el
    /// motivo del cambio.
    /// </remarks>
    Task CambiarClaveAsync(
        Guid usuarioId,
        SolicitudCambiarClave solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Envia un enlace para restablecer la contrasena.</summary>
    /// <param name="solicitud">Correo de la cuenta.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando se procesa la solicitud.</returns>
    /// <remarks>
    /// Responde igual exista o no la cuenta. Decir "ese correo no esta registrado" permitiria
    /// a cualquiera averiguar quien usa Rumbo probando direcciones.
    /// </remarks>
    Task SolicitarRestablecerClaveAsync(
        SolicitudOlvideClave solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Fija una contrasena nueva con el codigo recibido por correo.</summary>
    /// <param name="solicitud">Correo, codigo y contrasena nueva.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando la contrasena queda cambiada.</returns>
    Task RestablecerClaveAsync(
        SolicitudRestablecerClave solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Emite credenciales para otro de los espacios de la persona.</summary>
    /// <param name="usuarioId">Usuario que cambia de espacio.</param>
    /// <param name="solicitud">Espacio al que quiere cambiar.</param>
    /// <param name="direccionIp">IP desde la que se conecta.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Credenciales con el espacio nuevo.</returns>
    Task<RespuestaAutenticacion> CambiarEspacioAsync(
        Guid usuarioId,
        SolicitudCambiarEspacio solicitud,
        string? direccionIp,
        CancellationToken cancelacion = default);
}
