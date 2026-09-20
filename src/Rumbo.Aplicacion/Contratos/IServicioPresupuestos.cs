using Rumbo.Contratos.Presupuestos;

namespace Rumbo.Aplicacion.Contratos;

/// <summary>Presupuestos del espacio y su consumo real.</summary>
public interface IServicioPresupuestos
{
    /// <summary>Lista los presupuestos con el estado de sus partidas.</summary>
    /// <param name="soloVigente">Si solo se devuelve el que cubre la fecha de hoy.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Los presupuestos, del mas reciente al mas antiguo.</returns>
    Task<IReadOnlyList<PresupuestoDetalle>> ListarAsync(
        bool soloVigente = false,
        CancellationToken cancelacion = default);

    /// <summary>Devuelve un presupuesto concreto.</summary>
    /// <param name="presupuestoId">Presupuesto buscado.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El presupuesto con el consumo de cada partida.</returns>
    Task<PresupuestoDetalle> ObtenerAsync(
        Guid presupuestoId,
        CancellationToken cancelacion = default);

    /// <summary>Crea un presupuesto con sus partidas.</summary>
    /// <param name="solicitud">Datos del presupuesto.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El presupuesto creado.</returns>
    Task<PresupuestoDetalle> CrearAsync(
        SolicitudGuardarPresupuesto solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Modifica un presupuesto y sus partidas.</summary>
    /// <param name="presupuestoId">Presupuesto que se modifica.</param>
    /// <param name="solicitud">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El presupuesto actualizado.</returns>
    Task<PresupuestoDetalle> ActualizarAsync(
        Guid presupuestoId,
        SolicitudGuardarPresupuesto solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Elimina un presupuesto.</summary>
    /// <param name="presupuestoId">Presupuesto que se elimina.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando queda eliminado.</returns>
    /// <remarks>
    /// Eliminarlo no toca ningun movimiento: un presupuesto es una intencion de gasto, no
    /// dinero. El historial real permanece intacto.
    /// </remarks>
    Task EliminarAsync(Guid presupuestoId, CancellationToken cancelacion = default);
}
