namespace Rumbo.Infraestructura.Identidad;

/// <summary>
/// Parametros de emision y validacion de los tokens JWT.
/// </summary>
/// <remarks>
/// Se leen de la seccion <c>Jwt</c> de la configuracion. La clave de firma NUNCA va en
/// <c>appsettings.json</c>: en desarrollo se guarda con <c>dotnet user-secrets</c> y en
/// produccion en Azure Key Vault.
/// </remarks>
public class OpcionesJwt
{
    /// <summary>Nombre de la seccion de configuracion.</summary>
    public const string Seccion = "Jwt";

    /// <summary>Quien emite el token.</summary>
    public string Emisor { get; set; } = "Rumbo";

    /// <summary>Para quien es valido el token.</summary>
    public string Audiencia { get; set; } = "RumboMovil";

    /// <summary>
    /// Clave simetrica con la que se firma. Debe tener al menos 32 caracteres.
    /// </summary>
    /// <remarks>
    /// Quien conozca esta clave puede fabricar tokens validos para cualquier usuario y
    /// cualquier espacio. Es el secreto mas sensible del sistema.
    /// </remarks>
    public string ClaveFirma { get; set; } = string.Empty;

    /// <summary>
    /// Duracion del token de acceso, en minutos.
    /// </summary>
    /// <remarks>
    /// Corta a proposito. Un token de acceso no se puede revocar: una vez emitido, vale hasta
    /// que caduca. Quince minutos acotan el dano de un robo sin molestar al usuario, porque
    /// la app lo renueva sola con el token de renovacion.
    /// </remarks>
    public int MinutosTokenAcceso { get; set; } = 15;

    /// <summary>
    /// Duracion del token de renovacion, en dias.
    /// </summary>
    /// <remarks>
    /// Este si se puede revocar, porque se guarda en la base de datos. De ahi que pueda durar
    /// mucho mas sin asumir el mismo riesgo.
    /// </remarks>
    public int DiasTokenRenovacion { get; set; } = 30;
}
