namespace Rumbo.Dominio.Enums;

/// <summary>
/// Situacion de una meta de ahorro.
/// </summary>
public enum EstadoMeta
{
    /// <summary>En curso.</summary>
    Activa = 1,

    /// <summary>Sin aportes por decision del usuario.</summary>
    Pausada = 2,

    /// <summary>Se llego al monto objetivo.</summary>
    Alcanzada = 3,

    /// <summary>Se abandono. El dinero ya aportado sigue en su cuenta.</summary>
    Cancelada = 4,
}
