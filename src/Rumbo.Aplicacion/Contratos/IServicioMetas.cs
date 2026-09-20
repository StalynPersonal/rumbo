using Rumbo.Contratos.Metas;

namespace Rumbo.Aplicacion.Contratos;

/// <summary>Metas de ahorro y sus aportes.</summary>
public interface IServicioMetas
{
    /// <summary>Lista las metas del espacio con su proyeccion.</summary>
    /// <param name="incluirCerradas">Si se incluyen las alcanzadas y canceladas.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Las metas, por prioridad y fecha objetivo.</returns>
    Task<IReadOnlyList<MetaDetalle>> ListarAsync(
        bool incluirCerradas = false,
        CancellationToken cancelacion = default);

    /// <summary>Devuelve una meta concreta.</summary>
    /// <param name="metaId">Meta buscada.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La meta con su proyeccion.</returns>
    Task<MetaDetalle> ObtenerAsync(Guid metaId, CancellationToken cancelacion = default);

    /// <summary>Crea una meta.</summary>
    /// <param name="solicitud">Datos de la meta.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La meta creada.</returns>
    Task<MetaDetalle> CrearAsync(
        SolicitudGuardarMeta solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Modifica una meta.</summary>
    /// <param name="metaId">Meta que se modifica.</param>
    /// <param name="solicitud">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La meta actualizada.</returns>
    Task<MetaDetalle> ActualizarAsync(
        Guid metaId,
        SolicitudGuardarMeta solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Cambia el estado de una meta.</summary>
    /// <param name="metaId">Meta afectada.</param>
    /// <param name="solicitud">Estado nuevo.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La meta actualizada.</returns>
    Task<MetaDetalle> CambiarEstadoAsync(
        Guid metaId,
        SolicitudCambiarEstadoMeta solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Elimina una meta sin aportes.</summary>
    /// <param name="metaId">Meta que se elimina.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando queda eliminada.</returns>
    /// <remarks>
    /// Una meta con aportes no se elimina: esos movimientos existen y apuntan a ella.
    /// Cancelarla la retira de la vista sin romper el historial.
    /// </remarks>
    Task EliminarAsync(Guid metaId, CancellationToken cancelacion = default);

    /// <summary>Lista los aportes de una meta.</summary>
    /// <param name="metaId">Meta consultada.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Los aportes, del mas reciente al mas antiguo.</returns>
    Task<IReadOnlyList<AporteMetaDto>> ListarAportesAsync(
        Guid metaId,
        CancellationToken cancelacion = default);

    /// <summary>Aporta dinero a una meta desde una cuenta.</summary>
    /// <param name="metaId">Meta que recibe el dinero.</param>
    /// <param name="solicitud">Cuenta de origen, importe y fecha.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La meta con su progreso actualizado.</returns>
    /// <remarks>
    /// Se registra como <b>transferencia</b> hacia la cuenta de ahorro de la meta, no como
    /// gasto: ahorrar no es gastar.
    /// </remarks>
    Task<MetaDetalle> AportarAsync(
        Guid metaId,
        SolicitudAportarAMeta solicitud,
        CancellationToken cancelacion = default);
}
