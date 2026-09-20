using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.Extensions.Logging;

using MimeKit;

using Rumbo.Aplicacion.Contratos;

namespace Rumbo.Infraestructura.Correo;

/// <summary>
/// Envia correos por SMTP, eligiendo el servidor segun el espacio de origen.
/// </summary>
/// <remarks>
/// <para>
/// Usa <b>MailKit</b> y no <c>System.Net.Mail.SmtpClient</c>: la documentacion de .NET
/// desaconseja esta ultima para codigo nuevo porque no maneja bien STARTTLS moderno ni
/// OAuth2.
/// </para>
/// <para>
/// <b>Si no hay servidor configurado</b> no falla: registra el contenido del correo y
/// devuelve <c>false</c>. Asi se puede probar el flujo completo de invitaciones copiando el
/// enlace desde la consola, sin montar un servidor de correo.
/// </para>
/// </remarks>
/// <param name="resolvedor">Decide que servidor usar.</param>
/// <param name="registro">Registro de eventos.</param>
public class EnviadorCorreoSmtp(
    ResolvedorCorreo resolvedor,
    ILogger<EnviadorCorreoSmtp> registro) : IEnviadorCorreo
{
    /// <inheritdoc />
    public async Task<bool> EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoHtml,
        Guid? espacioId = null,
        CancellationToken cancelacion = default)
    {
        var credenciales = await resolvedor.ResolverAsync(espacioId, cancelacion);

        if (credenciales is null)
        {
            // En desarrollo esto es lo normal. Se registra el cuerpo para poder copiar el
            // enlace de invitacion o de restablecimiento desde la consola.
            registro.LogWarning(
                "No hay servidor SMTP configurado. El correo para {Destinatario} con asunto "
                + "'{Asunto}' NO se envió. Cuerpo:\n{Cuerpo}",
                destinatario, asunto, cuerpoHtml);

            return false;
        }

        try
        {
            await EnviarConAsync(credenciales, destinatario, asunto, cuerpoHtml, cancelacion);

            // Se registra el destinatario, el asunto y de que servidor salio; nunca el
            // cuerpo, que contiene codigos de invitacion y enlaces de restablecimiento.
            registro.LogInformation(
                "Correo enviado a {Destinatario} con asunto '{Asunto}' desde {Origen}.",
                destinatario, asunto, credenciales.Origen);

            return true;
        }
        catch (Exception excepcion)
        {
            // Un fallo de correo no tumba la operacion: la invitacion ya se creo y sigue
            // siendo valida, y su codigo puede compartirse por otro medio.
            registro.LogError(excepcion,
                "No se pudo enviar el correo a {Destinatario} desde {Origen}.",
                destinatario, credenciales.Origen);

            return false;
        }
    }

    /// <summary>
    /// Conecta, autentica y envia. Es la unica parte que habla con el servidor.
    /// </summary>
    /// <param name="credenciales">Servidor y credenciales ya resueltos.</param>
    /// <param name="destinatario">Direccion de destino.</param>
    /// <param name="asunto">Asunto del mensaje.</param>
    /// <param name="cuerpoHtml">Cuerpo en HTML.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando el mensaje sale.</returns>
    internal static async Task EnviarConAsync(
        CredencialesSmtp credenciales,
        string destinatario,
        string asunto,
        string cuerpoHtml,
        CancellationToken cancelacion)
    {
        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(
            credenciales.RemitenteNombre, credenciales.RemitenteCorreo));
        mensaje.To.Add(MailboxAddress.Parse(destinatario));
        mensaje.Subject = asunto;
        mensaje.Body = new BodyBuilder { HtmlBody = cuerpoHtml }.ToMessageBody();

        using var cliente = new SmtpClient();

        await ConectarAsync(cliente, credenciales, cancelacion);
        await cliente.SendAsync(mensaje, cancelacion);
        await cliente.DisconnectAsync(quit: true, cancelacion);
    }

    /// <summary>
    /// Abre la conexion y se autentica. Se reutiliza tambien en la prueba de conexion.
    /// </summary>
    /// <param name="cliente">Cliente SMTP.</param>
    /// <param name="credenciales">Servidor y credenciales.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando la sesion esta lista.</returns>
    internal static async Task ConectarAsync(
        SmtpClient cliente,
        CredencialesSmtp credenciales,
        CancellationToken cancelacion)
    {
        var seguridad = credenciales.UsarSslDirecto
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        await cliente.ConnectAsync(credenciales.Host, credenciales.Puerto, seguridad, cancelacion);

        if (!string.IsNullOrWhiteSpace(credenciales.Usuario))
        {
            await cliente.AuthenticateAsync(
                credenciales.Usuario, credenciales.Clave ?? string.Empty, cancelacion);
        }
    }
}
