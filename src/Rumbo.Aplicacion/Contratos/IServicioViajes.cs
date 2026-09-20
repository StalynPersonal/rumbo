using Rumbo.Contratos.Viajes;

namespace Rumbo.Aplicacion.Contratos;

/// <summary>Viajes planificados, su presupuesto, su fondo y su viabilidad.</summary>
public interface IServicioViajes
{
    /// <summary>Lista los viajes del espacio.</summary>
    /// <param name="incluirCerrados">Si se incluyen los finalizados y cancelados.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Los viajes, por fecha de salida.</returns>
    Task<IReadOnlyList<ViajeDetalle>> ListarAsync(
        bool incluirCerrados = false,
        CancellationToken cancelacion = default);

    /// <summary>Devuelve un viaje concreto.</summary>
    /// <param name="viajeId">Viaje buscado.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El viaje con su desglose y su gasto real.</returns>
    Task<ViajeDetalle> ObtenerAsync(Guid viajeId, CancellationToken cancelacion = default);

    /// <summary>Crea un viaje con su desglose.</summary>
    /// <param name="solicitud">Datos del viaje.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El viaje creado.</returns>
    Task<ViajeDetalle> CrearAsync(
        SolicitudGuardarViaje solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Modifica un viaje y su desglose.</summary>
    /// <param name="viajeId">Viaje que se modifica.</param>
    /// <param name="solicitud">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El viaje actualizado.</returns>
    Task<ViajeDetalle> ActualizarAsync(
        Guid viajeId,
        SolicitudGuardarViaje solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Cambia el estado de un viaje.</summary>
    /// <param name="viajeId">Viaje afectado.</param>
    /// <param name="solicitud">Estado nuevo.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El viaje actualizado.</returns>
    Task<ViajeDetalle> CambiarEstadoAsync(
        Guid viajeId,
        SolicitudCambiarEstadoViaje solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Elimina un viaje sin gastos imputados.</summary>
    /// <param name="viajeId">Viaje que se elimina.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando queda eliminado.</returns>
    /// <remarks>
    /// Un viaje con gastos no se borra: esos movimientos existen y apuntan a el. Se cancela.
    /// </remarks>
    Task EliminarAsync(Guid viajeId, CancellationToken cancelacion = default);

    /// <summary>Responde si el hogar puede permitirse el viaje.</summary>
    /// <param name="viajeId">Viaje analizado.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El veredicto con sus tres escenarios.</returns>
    /// <remarks>
    /// Es una proyeccion para decidir: no mueve dinero, no crea aportes y no reserva nada.
    /// </remarks>
    Task<ViabilidadViajeDto> AnalizarViabilidadAsync(
        Guid viajeId,
        CancellationToken cancelacion = default);
}
