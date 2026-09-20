using System.Net;

namespace Rumbo.Infraestructura.Correo.Plantillas;

/// <summary>
/// Genera el HTML de los correos que envia Rumbo.
/// </summary>
/// <remarks>
/// <para>
/// Plantillas escritas a mano y no un motor como Razor: son tres correos cortos, y anadir un
/// motor de plantillas para esto seria mas piezas que mantener sin ninguna ventaja.
/// </para>
/// <para>
/// <b>Todo dato variable se escapa con <c>WebUtility.HtmlEncode</c>.</b> Un nombre de
/// espacio elegido por un usuario acaba dentro de un correo que lee otra persona: si no se
/// escapara, se podria inyectar HTML en el buzon ajeno.
/// </para>
/// </remarks>
public static class PlantillasCorreo
{
    /// <summary>Correo de invitacion para crear un espacio propio.</summary>
    /// <param name="nombreEspacio">Nombre propuesto para el espacio.</param>
    /// <param name="codigo">Codigo de un solo uso.</param>
    /// <param name="urlBase">Direccion base de la aplicacion.</param>
    /// <param name="diasValidez">Dias que el codigo sigue siendo valido.</param>
    /// <returns>Asunto y cuerpo en HTML.</returns>
    public static (string Asunto, string Cuerpo) InvitacionPropietario(
        string nombreEspacio,
        string codigo,
        string urlBase,
        int diasValidez)
    {
        var espacio = WebUtility.HtmlEncode(nombreEspacio);
        var codigoSeguro = WebUtility.HtmlEncode(codigo);

        var cuerpo = Envolver($"""
            <h1>Te damos la bienvenida a Rumbo</h1>
            <p>Han creado una invitación para que administres las finanzas de
            <strong>{espacio}</strong>.</p>
            <p>Tu código de acceso es:</p>
            {CajaCodigo(codigoSeguro)}
            <p>Úsalo al registrarte en la aplicación. El código sirve <strong>una sola vez</strong>
            y caduca en {diasValidez} días.</p>
            <p><a class="boton" href="{urlBase}/registro?codigo={Uri.EscapeDataString(codigo)}">
            Crear mi cuenta</a></p>
            <p class="nota">Si no esperabas esta invitación, puedes ignorar este mensaje: sin el
            código nadie puede usarla.</p>
            """);

        return ("Invitación para crear tu espacio en Rumbo", cuerpo);
    }

    /// <summary>Correo de invitacion para unirse a un espacio existente.</summary>
    /// <param name="nombreEspacio">Espacio al que se invita.</param>
    /// <param name="quienInvita">Nombre de quien envia la invitacion.</param>
    /// <param name="codigo">Codigo de un solo uso.</param>
    /// <param name="urlBase">Direccion base de la aplicacion.</param>
    /// <param name="diasValidez">Dias que el codigo sigue siendo valido.</param>
    /// <returns>Asunto y cuerpo en HTML.</returns>
    public static (string Asunto, string Cuerpo) InvitacionMiembro(
        string nombreEspacio,
        string quienInvita,
        string codigo,
        string urlBase,
        int diasValidez)
    {
        var espacio = WebUtility.HtmlEncode(nombreEspacio);
        var invita = WebUtility.HtmlEncode(quienInvita);
        var codigoSeguro = WebUtility.HtmlEncode(codigo);

        var cuerpo = Envolver($"""
            <h1>{invita} te invita a Rumbo</h1>
            <p>Te han invitado a compartir las finanzas de <strong>{espacio}</strong>.</p>
            <p>Tu código de acceso es:</p>
            {CajaCodigo(codigoSeguro)}
            <p>El código sirve <strong>una sola vez</strong> y caduca en {diasValidez} días.</p>
            <p><a class="boton" href="{urlBase}/registro?codigo={Uri.EscapeDataString(codigo)}">
            Unirme</a></p>
            <p class="nota">Si no conoces a quien te invita, ignora este mensaje.</p>
            """);

        return ($"{invita} te invita a compartir las finanzas de {espacio}", cuerpo);
    }

    /// <summary>Correo para restablecer la contrasena.</summary>
    /// <param name="nombre">Nombre de la persona.</param>
    /// <param name="codigo">Codigo de restablecimiento.</param>
    /// <param name="correo">Correo de la cuenta.</param>
    /// <param name="urlBase">Direccion base de la aplicacion.</param>
    /// <returns>Asunto y cuerpo en HTML.</returns>
    public static (string Asunto, string Cuerpo) RestablecerClave(
        string nombre,
        string codigo,
        string correo,
        string urlBase)
    {
        var nombreSeguro = WebUtility.HtmlEncode(nombre);
        var enlace = $"{urlBase}/restablecer?correo={Uri.EscapeDataString(correo)}"
                     + $"&codigo={Uri.EscapeDataString(codigo)}";

        var cuerpo = Envolver($"""
            <h1>Restablecer tu contraseña</h1>
            <p>Hola {nombreSeguro}, hemos recibido una solicitud para cambiar la contraseña de tu
            cuenta de Rumbo.</p>
            <p><a class="boton" href="{enlace}">Elegir una contraseña nueva</a></p>
            <p class="nota">El enlace caduca en 2 horas y solo puede usarse una vez.</p>
            <p class="nota">Si no fuiste tú, ignora este mensaje: tu contraseña actual sigue
            siendo válida y nadie ha accedido a tu cuenta.</p>
            """);

        return ("Restablecer tu contraseña de Rumbo", cuerpo);
    }

    /// <summary>Caja destacada con el codigo de invitacion.</summary>
    private static string CajaCodigo(string codigo) =>
        $"""<div class="codigo">{codigo}</div>""";

    /// <summary>
    /// Envuelve el contenido en la estructura y los estilos comunes.
    /// </summary>
    /// <remarks>
    /// Los estilos van en linea y en un bloque dentro del propio documento porque los
    /// clientes de correo no cargan hojas de estilo externas.
    /// </remarks>
    private static string Envolver(string contenido) => $$"""
        <!DOCTYPE html>
        <html lang="es">
        <head>
          <meta charset="utf-8">
          <style>
            body { font-family: -apple-system, "Segoe UI", Roboto, sans-serif;
                   color: #1f2933; background: #f5f7fa; margin: 0; padding: 24px; }
            .marco { max-width: 560px; margin: 0 auto; background: #ffffff;
                     border-radius: 12px; padding: 32px; }
            h1 { font-size: 20px; margin: 0 0 16px; color: #0f766e; }
            p { line-height: 1.6; margin: 0 0 16px; }
            .codigo { font-family: Consolas, monospace; font-size: 22px; letter-spacing: 2px;
                      background: #f0fdfa; border: 1px dashed #0f766e; border-radius: 8px;
                      padding: 16px; text-align: center; margin: 0 0 16px; word-break: break-all; }
            .boton { display: inline-block; background: #0f766e; color: #ffffff;
                     text-decoration: none; padding: 12px 24px; border-radius: 8px; }
            .nota { font-size: 13px; color: #627d98; }
            .pie { font-size: 12px; color: #9aa5b1; text-align: center; margin-top: 24px; }
          </style>
        </head>
        <body>
          <div class="marco">
            {{contenido}}
          </div>
          <p class="pie">Rumbo · Tus finanzas. Tus metas. Tu próximo destino.</p>
        </body>
        </html>
        """;
}
