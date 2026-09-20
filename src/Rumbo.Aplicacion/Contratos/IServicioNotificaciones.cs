using Rumbo.Contratos.Notificaciones;

namespace Rumbo.Aplicacion.Contratos;

/// <summary>Avisos del hogar.</summary>
/// <remarks>
/// Se persisten aunque todavia no exista transporte push: cuando se enchufe, el historial ya
/// estara ahi.
/// </remarks>
public interface IServicioNotificaciones
{
    /// <summary>Lista los avisos del espacio.</summary>
    /// <param name="soloSinLeer">Si solo se devuelven los pendientes de leer.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Los avisos, del mas reciente al mas antiguo.</returns>
    Task<IReadOnlyList<NotificacionDto>> ListarAsync(
        bool soloSinLeer = false,
        CancellationToken cancelacion = default);

    /// <summary>Marca un aviso como leido.</summary>
    /// <param name="notificacionId">Aviso afectado.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El aviso actualizado.</returns>
    Task<NotificacionDto> MarcarComoLeidaAsync(
        Guid notificacionId,
        CancellationToken cancelacion = default);

    /// <summary>Marca como leidos todos los avisos pendientes.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Cuantos se marcaron.</returns>
    Task<int> MarcarTodasComoLeidasAsync(CancellationToken cancelacion = default);

    /// <summary>Descarta un aviso.</summary>
    /// <param name="notificacionId">Aviso afectado.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando queda descartado.</returns>
    /// <remarks>
    /// Se descarta, no se borra: saber que se aviso y que la persona lo aparto es
    /// informacion util para medir si los avisos sirven de algo.
    /// </remarks>
    Task DescartarAsync(Guid notificacionId, CancellationToken cancelacion = default);

    /// <summary>Revisa el estado del hogar y genera los avisos que procedan.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Cuantos se generaron y cuantos quedan sin leer.</returns>
    /// <remarks>
    /// No duplica: si ya existe un aviso del mismo tipo sobre lo mismo y sin leer, no se
    /// crea otro. Recibir cinco veces el mismo aviso hace que se dejen de leer todos.
    /// </remarks>
    Task<ResultadoGeneracionAvisos> GenerarAsync(CancellationToken cancelacion = default);
}
