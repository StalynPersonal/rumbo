namespace Rumbo.Dominio.Comun;

/// <summary>
/// Marca una entidad que nunca se elimina fisicamente de la base de datos.
/// </summary>
/// <remarks>
/// En un sistema financiero borrar una fila destruye la trazabilidad: deja de cuadrar la
/// reconciliacion de saldos y la auditoria queda incompleta. Por eso las entidades financieras
/// se marcan como eliminadas y un filtro global de EF Core las oculta de todas las consultas.
/// </remarks>
public interface IBorradoLogico
{
    /// <summary>Indica si el registro esta eliminado y por tanto oculto en las consultas.</summary>
    bool Eliminado { get; set; }

    /// <summary>Instante en que se elimino, o <c>null</c> si sigue activo.</summary>
    DateTimeOffset? FechaEliminacion { get; set; }

    /// <summary>Usuario que lo elimino.</summary>
    Guid? EliminadoPorUsuarioId { get; set; }
}
