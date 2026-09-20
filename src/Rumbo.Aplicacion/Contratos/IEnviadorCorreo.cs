namespace Rumbo.Aplicacion.Contratos;

/// <summary>
/// Envia los correos transaccionales del sistema.
/// </summary>
/// <remarks>
/// Solo tres tipos de correo, todos ligados a la seguridad de la cuenta: invitacion,
/// restablecimiento de contrasena y confirmacion de correo. Rumbo no envia correos
/// publicitarios ni resumenes automaticos.
/// </remarks>
public interface IEnviadorCorreo
{
    /// <summary>
    /// Envia un correo.
    /// </summary>
    /// <param name="destinatario">Direccion de destino.</param>
    /// <param name="asunto">Asunto del mensaje.</param>
    /// <param name="cuerpoHtml">Cuerpo en HTML.</param>
    /// <param name="espacioId">
    /// Espacio del que parte el correo. Si se indica y ese espacio tiene su propio servidor
    /// configurado, el mensaje sale desde el correo del propietario. Si es <c>null</c>, o si
    /// el espacio no tiene servidor propio, se usa el de la plataforma.
    /// </param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>
    /// <c>true</c> si el correo salio. Devuelve <c>false</c> en lugar de lanzar una excepcion
    /// porque un fallo de correo NO debe tumbar la operacion: una invitacion creada sigue
    /// siendo valida aunque el correo no llegue, y el codigo puede compartirse a mano.
    /// </returns>
    Task<bool> EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoHtml,
        Guid? espacioId = null,
        CancellationToken cancelacion = default);
}
