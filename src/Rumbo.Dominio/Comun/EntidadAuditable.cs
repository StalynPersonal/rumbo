namespace Rumbo.Dominio.Comun;

/// <summary>
/// Entidad con clave primaria y datos de auditoria de creacion y modificacion.
/// </summary>
/// <remarks>
/// La usan las entidades globales (las que no pertenecen a un espacio), como
/// <c>Espacio</c> o <c>Invitacion</c>.
/// </remarks>
public abstract class EntidadAuditable : EntidadBase, IAuditable
{
    /// <inheritdoc />
    public DateTimeOffset FechaCreacion { get; set; }

    /// <inheritdoc />
    public Guid? CreadoPorUsuarioId { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? FechaActualizacion { get; set; }

    /// <inheritdoc />
    public Guid? ActualizadoPorUsuarioId { get; set; }
}
