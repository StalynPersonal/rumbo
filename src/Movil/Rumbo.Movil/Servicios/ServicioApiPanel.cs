using Rumbo.Contratos.Panel;

namespace Rumbo.Movil.Servicios;

/// <summary>
/// Llama al panel de la API.
/// </summary>
/// <remarks>
/// Los servicios de API de Rumbo son planos y aburridos a proposito: un metodo por
/// operacion, con nombre claro y sin logica. Toda la inteligencia esta en el servidor.
///
/// Fijate en PanelInicio: es LA MISMA clase que usa la API. Viene de Rumbo.Contratos, que
/// referencian los dos. Por eso, si manana cambio un campo en el servidor, esto deja de
/// compilar en lugar de fallar en el movil un domingo por la tarde.
/// </remarks>
/// <param name="api">Cliente HTTP.</param>
public class ServicioApiPanel(ClienteApi api)
{
    /// <summary>Trae toda la pantalla de inicio en una sola peticion.</summary>
    /// <returns>El panel del espacio activo.</returns>
    /// <remarks>
    /// UNA llamada, no ocho. En una conexion movil lenta, ocho peticiones son ocho
    /// oportunidades de que la pantalla se quede a medias.
    /// </remarks>
    public Task<PanelInicio> ObtenerAsync() =>
        api.ObtenerAsync<PanelInicio>("api/v1/panel");
}
