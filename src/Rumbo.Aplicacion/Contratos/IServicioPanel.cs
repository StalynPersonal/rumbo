using Rumbo.Contratos.Panel;

namespace Rumbo.Aplicacion.Contratos;

/// <summary>
/// Pantalla de inicio del hogar, en una sola respuesta.
/// </summary>
public interface IServicioPanel
{
    /// <summary>Arma el panel del espacio activo.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Todo lo que la pantalla de inicio necesita.</returns>
    /// <remarks>
    /// Se devuelve todo junto a proposito: la aplicacion movil se abre con una llamada en
    /// lugar de ocho. En una conexion lenta, ocho peticiones son ocho oportunidades de que
    /// la pantalla se quede a medias.
    /// </remarks>
    Task<PanelInicio> ObtenerAsync(CancellationToken cancelacion = default);
}
