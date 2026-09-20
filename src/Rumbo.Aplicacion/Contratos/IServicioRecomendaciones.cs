using Rumbo.Contratos.Recomendaciones;

namespace Rumbo.Aplicacion.Contratos;

/// <summary>Sugerencias generadas por el motor determinista.</summary>
/// <remarks>
/// Este servicio solo lee y marca decisiones. <b>Nunca</b> mueve dinero: aceptar una
/// sugerencia deja constancia de la intencion, y el movimiento se crea despues por la
/// operacion normal, que la persona confirma.
/// </remarks>
public interface IServicioRecomendaciones
{
    /// <summary>Lista las sugerencias del espacio.</summary>
    /// <param name="incluirRespondidas">Si se incluyen las aceptadas y descartadas.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Las sugerencias, de la mas reciente a la mas antigua.</returns>
    Task<IReadOnlyList<RecomendacionDto>> ListarAsync(
        bool incluirRespondidas = false,
        CancellationToken cancelacion = default);

    /// <summary>Vuelve a calcular las sugerencias con los datos de hoy.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Las sugerencias vigentes tras el recalculo.</returns>
    Task<IReadOnlyList<RecomendacionDto>> RecalcularAsync(
        CancellationToken cancelacion = default);

    /// <summary>Registra la respuesta de la persona a una sugerencia.</summary>
    /// <param name="recomendacionId">Sugerencia respondida.</param>
    /// <param name="solicitud">Aceptada o Descartada.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La sugerencia con su estado nuevo.</returns>
    Task<RecomendacionDto> ResponderAsync(
        Guid recomendacionId,
        SolicitudResponderRecomendacion solicitud,
        CancellationToken cancelacion = default);
}
