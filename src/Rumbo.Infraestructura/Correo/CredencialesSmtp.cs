namespace Rumbo.Infraestructura.Correo;

/// <summary>
/// Datos ya resueltos y descifrados con los que se conecta al servidor de correo.
/// </summary>
/// <param name="Host">Servidor SMTP.</param>
/// <param name="Puerto">Puerto de conexion.</param>
/// <param name="UsarSslDirecto">Si usa SSL directo en lugar de STARTTLS.</param>
/// <param name="Usuario">Usuario de autenticacion, o <c>null</c> si el servidor no la exige.</param>
/// <param name="Clave">Contrasena ya descifrada.</param>
/// <param name="RemitenteCorreo">Direccion del remitente.</param>
/// <param name="RemitenteNombre">Nombre del remitente.</param>
/// <param name="Origen">De donde salieron: util para diagnosticar y para los registros.</param>
/// <remarks>
/// Es un tipo interno y efimero: existe solo durante el envio. La contrasena descifrada no
/// se guarda en ningun sitio ni se registra en los logs.
/// </remarks>
public record CredencialesSmtp(
    string Host,
    int Puerto,
    bool UsarSslDirecto,
    string? Usuario,
    string? Clave,
    string RemitenteCorreo,
    string RemitenteNombre,
    string Origen);
