namespace Rumbo.Infraestructura.Correo;

/// <summary>
/// Parametros del servidor SMTP con el que se envian los correos.
/// </summary>
/// <remarks>
/// <para>
/// Se leen de la seccion <c>Correo</c>. La contrasena NUNCA va en <c>appsettings.json</c>:
/// en desarrollo se guarda con <c>dotnet user-secrets</c> y en produccion en Azure Key Vault.
/// </para>
/// <para>
/// Se recomienda usar una <b>contrasena de aplicacion</b> dedicada y no la contrasena
/// principal de la cuenta de correo: si se filtrara, se revoca sin cambiar nada mas.
/// </para>
/// </remarks>
public class OpcionesCorreo
{
    /// <summary>Nombre de la seccion de configuracion.</summary>
    public const string Seccion = "Correo";

    /// <summary>Servidor SMTP, por ejemplo <c>smtp.gmail.com</c>.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Puerto. Normalmente 587 con STARTTLS o 465 con SSL directo.</summary>
    public int Puerto { get; set; } = 587;

    /// <summary>
    /// Indica si se conecta con SSL directo desde el principio, en lugar de STARTTLS.
    /// </summary>
    public bool UsarSslDirecto { get; set; }

    /// <summary>Usuario de autenticacion, habitualmente la propia direccion de correo.</summary>
    public string Usuario { get; set; } = string.Empty;

    /// <summary>Contrasena de aplicacion. Solo de user-secrets o Key Vault.</summary>
    public string Clave { get; set; } = string.Empty;

    /// <summary>Direccion que figura como remitente.</summary>
    public string RemitenteCorreo { get; set; } = string.Empty;

    /// <summary>Nombre que figura como remitente.</summary>
    public string RemitenteNombre { get; set; } = "Rumbo";

    /// <summary>
    /// Direccion base de la aplicacion, para construir los enlaces de los correos.
    /// </summary>
    public string UrlBase { get; set; } = "https://localhost:7299";

    /// <summary>Indica si hay configuracion suficiente para poder enviar.</summary>
    /// <returns><c>true</c> si estan los datos minimos.</returns>
    public bool EstaConfigurado() =>
        !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(RemitenteCorreo);
}
