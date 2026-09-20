namespace Rumbo.Dominio.Comun;

/// <summary>
/// Marca una entidad cuyo alta y ultima modificacion se registran automaticamente.
/// </summary>
/// <remarks>
/// Los valores los rellena <c>InterceptorAuditoria</c> al guardar cambios; no se asignan a mano.
/// </remarks>
public interface IAuditable
{
    /// <summary>Instante en que se creo el registro.</summary>
    DateTimeOffset FechaCreacion { get; set; }

    /// <summary>Usuario que creo el registro, si la operacion tenia usuario autenticado.</summary>
    Guid? CreadoPorUsuarioId { get; set; }

    /// <summary>Instante de la ultima modificacion, o <c>null</c> si nunca se modifico.</summary>
    DateTimeOffset? FechaActualizacion { get; set; }

    /// <summary>Usuario que realizo la ultima modificacion.</summary>
    Guid? ActualizadoPorUsuarioId { get; set; }
}
