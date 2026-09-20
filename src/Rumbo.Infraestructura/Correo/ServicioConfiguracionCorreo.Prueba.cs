using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Rumbo.Contratos.Correo;
using Rumbo.Dominio.Entidades.Soporte;

namespace Rumbo.Infraestructura.Correo;

/// <summary>
/// Parte del servicio de correo que comprueba si la configuracion funciona de verdad.
/// </summary>
/// <remarks>
/// Probar antes de confiar importa mucho aqui: si las credenciales estuvieran mal, el fallo
/// aparecería semanas después, cuando alguien invite a su pareja y la invitación no llegue,
/// sin que nadie sepa por qué. Con esta comprobación el error se ve en el momento de
/// guardar.
/// </remarks>
public partial class ServicioConfiguracionCorreo
{
    /// <inheritdoc />
    public async Task<ResultadoPruebaCorreo> ProbarPlataformaAsync(
        SolicitudProbarCorreo solicitud,
        CancellationToken cancelacion = default)
    {
        var configuracion = await BuscarOCrearPlataformaAsync(cancelacion);

        return await ProbarYRegistrarAsync(configuracion, espacioId: null, solicitud, cancelacion);
    }

    /// <inheritdoc />
    public async Task<ResultadoPruebaCorreo> ProbarDelEspacioAsync(
        Guid espacioId,
        SolicitudProbarCorreo solicitud,
        CancellationToken cancelacion = default)
    {
        var configuracion = await BuscarOCrearDelEspacioAsync(espacioId, cancelacion);

        return await ProbarYRegistrarAsync(configuracion, espacioId, solicitud, cancelacion);
    }

    /// <summary>
    /// Intenta conectar y, si se pide, enviar un mensaje de prueba. Guarda el resultado.
    /// </summary>
    /// <param name="configuracion">Configuracion que se comprueba.</param>
    /// <param name="espacioId">Espacio, o <c>null</c> si es la de plataforma.</param>
    /// <param name="solicitud">Direccion de prueba, opcional.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El resultado, con un mensaje comprensible.</returns>
    private async Task<ResultadoPruebaCorreo> ProbarYRegistrarAsync(
        ConfiguracionCorreoBase configuracion,
        Guid? espacioId,
        SolicitudProbarCorreo solicitud,
        CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(configuracion.Host)
            || string.IsNullOrWhiteSpace(configuracion.RemitenteCorreo))
        {
            return new ResultadoPruebaCorreo(
                false, "Falta el servidor SMTP o la dirección del remitente.", false);
        }

        var credenciales = await resolvedor.ResolverAsync(espacioId, cancelacion);

        if (credenciales is null)
        {
            return new ResultadoPruebaCorreo(
                false,
                "No se pudo preparar la conexión. Comprueba que la configuración esté activa.",
                false);
        }

        var correoEnviado = false;
        string mensaje;
        bool correcta;

        try
        {
            using (var cliente = new SmtpClient())
            {
                await EnviadorCorreoSmtp.ConectarAsync(cliente, credenciales, cancelacion);
                await cliente.DisconnectAsync(quit: true, cancelacion);
            }

            correcta = true;
            mensaje = "Conexión y autenticación correctas.";

            if (!string.IsNullOrWhiteSpace(solicitud.CorreoDestinoPrueba))
            {
                await EnviadorCorreoSmtp.EnviarConAsync(
                    credenciales,
                    solicitud.CorreoDestinoPrueba.Trim(),
                    "Prueba de configuración de Rumbo",
                    CuerpoDePrueba(credenciales.RemitenteCorreo),
                    cancelacion);

                correoEnviado = true;
                mensaje = $"Conexión correcta y mensaje de prueba enviado a "
                          + $"{solicitud.CorreoDestinoPrueba.Trim()}.";
            }
        }
        catch (AuthenticationException)
        {
            // Se distingue este caso porque es el fallo mas habitual con diferencia: casi
            // siempre significa que se usó la contraseña normal en vez de una de aplicación.
            correcta = false;
            mensaje = "El servidor rechazó el usuario o la contraseña. Si tu proveedor exige "
                      + "verificación en dos pasos, necesitas una contraseña de aplicación.";
        }
        catch (SmtpCommandException excepcion)
        {
            correcta = false;
            mensaje = $"El servidor respondió con un error: {excepcion.Message}";
        }
        catch (Exception excepcion)
        {
            correcta = false;
            mensaje = "No se pudo conectar con el servidor. Comprueba el nombre del servidor, "
                      + $"el puerto y el tipo de cifrado. Detalle: {excepcion.Message}";
        }

        configuracion.FechaUltimaPrueba = DateTimeOffset.UtcNow;
        configuracion.UltimaPruebaCorrecta = correcta;

        // Se guarda el motivo del fallo, pero NUNCA la contraseña ni el cuerpo del mensaje.
        configuracion.UltimoErrorPrueba = correcta ? null : Recortar(mensaje);

        await contexto.SaveChangesAsync(cancelacion);

        registro.LogInformation(
            "Prueba de correo para {Ambito}: {Resultado}.",
            espacioId is null ? "la plataforma" : $"el espacio {espacioId}",
            correcta ? "correcta" : "fallida");

        return new ResultadoPruebaCorreo(correcta, mensaje, correoEnviado);
    }

    /// <summary>Cuerpo del mensaje de prueba.</summary>
    private static string CuerpoDePrueba(string remitente) => $"""
        <p>Si estás leyendo esto, la configuración de correo de Rumbo funciona.</p>
        <p>El mensaje salió desde <strong>{System.Net.WebUtility.HtmlEncode(remitente)}</strong>.</p>
        <p style="color:#627d98;font-size:13px;">Rumbo · Tus finanzas. Tus metas. Tu próximo destino.</p>
        """;

    /// <summary>Recorta el mensaje de error al tamaño de la columna.</summary>
    private static string Recortar(string mensaje) =>
        mensaje.Length <= 500 ? mensaje : mensaje[..500];
}
