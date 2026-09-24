namespace Rumbo.Api.Seguridad;

/// <summary>
/// Versiones publicadas de la aplicacion movil.
/// </summary>
/// <remarks>
/// Van en configuracion y no en codigo para poder publicar un APK nuevo y avisar a la gente
/// sin volver a desplegar la API.
/// </remarks>
public class OpcionesVersionApp
{
    /// <summary>Nombre de la seccion en la configuracion.</summary>
    public const string Seccion = "VersionApp";

    /// <summary>
    /// Version por debajo de la cual la aplicacion deberia pedir que se actualice.
    /// </summary>
    /// <remarks>
    /// Solo se sube cuando una version antigua hace algo <b>incorrecto</b>, no cada vez que
    /// se publica un APK. Obligar a actualizar por gusto acostumbra a la gente a ignorar el
    /// aviso, y entonces el dia que importa de verdad nadie lo lee.
    /// </remarks>
    public string VersionMinima { get; set; } = "1.0.0";

    /// <summary>Ultima version publicada.</summary>
    public string VersionRecomendada { get; set; } = "1.0.0";

    /// <summary>De donde bajar el APK.</summary>
    public string? UrlDescarga { get; set; }
}
