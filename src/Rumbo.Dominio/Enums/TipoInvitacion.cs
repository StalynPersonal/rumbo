namespace Rumbo.Dominio.Enums;

/// <summary>
/// Clase de invitacion segun quien la emite y que concede.
/// </summary>
/// <remarks>
/// No existe auto-registro en Rumbo: toda alta nace de una invitacion.
/// </remarks>
public enum TipoInvitacion
{
    /// <summary>La emite un administrador de plataforma. Al aceptarla se crea un espacio nuevo.</summary>
    Propietario = 1,

    /// <summary>La emite el propietario o un administrador para sumar a alguien a SU espacio.</summary>
    Miembro = 2,
}
