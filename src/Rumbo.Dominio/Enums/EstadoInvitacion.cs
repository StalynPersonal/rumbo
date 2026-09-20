namespace Rumbo.Dominio.Enums;

/// <summary>
/// Ciclo de vida de una invitacion.
/// </summary>
public enum EstadoInvitacion
{
    /// <summary>Emitida y todavia utilizable.</summary>
    Pendiente = 1,

    /// <summary>Ya se uso. Los codigos son de un solo uso.</summary>
    Aceptada = 2,

    /// <summary>Se paso de su fecha limite sin usarse.</summary>
    Expirada = 3,

    /// <summary>Quien la emitio la anulo antes de que se usara.</summary>
    Revocada = 4,
}
