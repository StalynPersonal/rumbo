namespace Rumbo.Dominio.Comun;

/// <summary>
/// Clase base de la mayoria de entidades del sistema: pertenece a un espacio, se audita y
/// se borra de forma logica.
/// </summary>
/// <remarks>
/// Heredar de aqui es la forma correcta de crear una entidad nueva de negocio. Se obtienen de
/// golpe las tres garantias: aislamiento entre espacios, auditoria y borrado no destructivo.
/// </remarks>
public abstract class EntidadDeEspacio : EntidadAuditable, IEntidadDeEspacio, IBorradoLogico
{
    /// <inheritdoc />
    public Guid EspacioId { get; set; }

    /// <inheritdoc />
    public bool Eliminado { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? FechaEliminacion { get; set; }

    /// <inheritdoc />
    public Guid? EliminadoPorUsuarioId { get; set; }
}
