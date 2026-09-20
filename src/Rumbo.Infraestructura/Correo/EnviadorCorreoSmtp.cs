using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MimeKit;

using Rumbo.Aplicacion.Contratos;

namespace Rumbo.Infraestructura.Correo;

/// <summary>
/// Envia correos a traves de un servidor SMTP.
/// </summary>
/// <remarks>
/// <para>
/// Usa <b>MailKit</b> y no <c>System.Net.Mail.SmtpClient</c>: la propia documentacion de .NET
/// desaconseja esta ultima para codigo nuevo, porque no maneja bien STARTTLS moderno ni
/// OAuth2.
/// </para>
/// <para>
/// <b>Si no hay servidor configurado</b> (lo habitual en desarrollo), no falla: registra el
/// contenido del correo en el log y devuelve <c>false</c>. Asi se puede probar todo el flujo
/// de invitaciones copiando el enlace desde la consola, sin montar un servidor de correo.
/// </para>
/// </remarks>
/// <param name="opciones">Parametros del servidor SMTP.</param>
/// <param name="registro">Registro de eventos.</param>
public class EnviadorCorreoSmtp(
    IOptions<OpcionesCorreo> opciones,
    ILogger<EnviadorCorreoSmtp> registro) : IEnviadorCorreo
{
    private readonly OpcionesCorreo _opciones = opciones.Value;

    /// <inheritdoc />
    public async Task<bool> EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoHtml,
        CancellationToken cancelacion = default)
    {
        if (!_opciones.EstaConfigurado())
        {
            // En desarrollo esto es lo normal. Se registra el cuerpo para poder copiar el
            // enlace de invitacion o de restablecimiento desde la consola.
            registro.LogWarning(
                "No hay servidor SMTP configurado. El correo para {Destinatario} con asunto "
                + "'{Asunto}' NO se envio. Cuerpo:\n{Cuerpo}",
                destinatario, asunto, cuerpoHtml);

            return false;
        }

        try
        {
            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress(_opciones.RemitenteNombre, _opciones.RemitenteCorreo));
            mensaje.To.Add(MailboxAddress.Parse(destinatario));
            mensaje.Subject = asunto;
            mensaje.Body = new BodyBuilder { HtmlBody = cuerpoHtml }.ToMessageBody();

            using var cliente = new SmtpClient();

            var seguridad = _opciones.UsarSslDirecto
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            await cliente.ConnectAsync(_opciones.Host, _opciones.Puerto, seguridad, cancelacion);

            if (!string.IsNullOrWhiteSpace(_opciones.Usuario))
            {
                await cliente.AuthenticateAsync(_opciones.Usuario, _opciones.Clave, cancelacion);
            }

            await cliente.SendAsync(mensaje, cancelacion);
            await cliente.DisconnectAsync(quit: true, cancelacion);

            // Se registra el destinatario y el asunto, nunca el cuerpo: contiene codigos de
            // invitacion y enlaces de restablecimiento que no deben quedar en los logs.
            registro.LogInformation(
                "Correo enviado a {Destinatario} con asunto '{Asunto}'.", destinatario, asunto);

            return true;
        }
        catch (Exception excepcion)
        {
            // Un fallo de correo no debe tumbar la operacion: la invitacion ya se creo y
            // sigue siendo valida, y el codigo puede compartirse por otro medio.
            registro.LogError(excepcion,
                "No se pudo enviar el correo a {Destinatario} con asunto '{Asunto}'.",
                destinatario, asunto);

            return false;
        }
    }
}
