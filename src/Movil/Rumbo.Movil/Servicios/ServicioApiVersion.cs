using Rumbo.Contratos.Aplicacion;

namespace Rumbo.Movil.Servicios;

/// <summary>
/// Pregunta al servidor si esta version de la aplicacion sigue valiendo.
/// </summary>
/// <remarks>
/// El APK se instala a mano, sin tienda: nadie avisa de que hay una version nueva si no
/// avisa la propia aplicacion.
/// </remarks>
/// <param name="api">Cliente HTTP.</param>
public class ServicioApiVersion(ClienteApi api)
{
    /// <summary>Consulta la version publicada.</summary>
    /// <param name="versionInstalada">La version que corre en este telefono.</param>
    /// <returns>Que version se espera y si conviene actualizar.</returns>
    public Task<VersionAplicacion> ConsultarAsync(string versionInstalada) =>
        api.ObtenerAsync<VersionAplicacion>(
            $"api/v1/app/version?version={versionInstalada}");
}
