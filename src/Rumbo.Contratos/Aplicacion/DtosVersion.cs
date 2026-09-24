namespace Rumbo.Contratos.Aplicacion;

/// <summary>
/// Version de la aplicacion movil que el servidor espera.
/// </summary>
/// <param name="VersionMinima">
/// Version por debajo de la cual la aplicacion no deberia seguir funcionando.
/// </param>
/// <param name="VersionRecomendada">Ultima version publicada.</param>
/// <param name="UrlDescarga">De donde bajar el APK nuevo.</param>
/// <param name="Mensaje">Que contarle a la persona, en espanol.</param>
/// <param name="ActualizacionObligatoria">
/// Si la version que pregunta esta por debajo de la minima.
/// </param>
/// <param name="HayActualizacion">Si existe una version mas reciente que la suya.</param>
/// <remarks>
/// <para>
/// El APK se instala de forma manual, sin tienda, asi que nadie avisa a la persona de que hay
/// una version nueva. Este endpoint es ese aviso.
/// </para>
/// <para>
/// El servidor <b>no</b> bloquea a las versiones antiguas: la API v1 sigue funcionando para
/// todas. Lo que hace es decirle a la aplicacion que se ha quedado atras, y es la aplicacion
/// la que decide si insiste o no. Cortar el acceso desde el servidor dejaria a alguien sin
/// ver sus finanzas por no haber actualizado, y eso es peor que la version vieja.
/// </para>
/// </remarks>
public record VersionAplicacion(
    string VersionMinima,
    string VersionRecomendada,
    string? UrlDescarga,
    string? Mensaje,
    bool ActualizacionObligatoria,
    bool HayActualizacion);
