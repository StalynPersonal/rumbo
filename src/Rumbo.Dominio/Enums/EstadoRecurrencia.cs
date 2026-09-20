namespace Rumbo.Dominio.Enums;

/// <summary>
/// Situacion de un gasto o de un ingreso recurrente.
/// </summary>
public enum EstadoRecurrencia
{
    /// <summary>Sigue vigente y genera avisos de proximo pago.</summary>
    Activa = 1,

    /// <summary>Temporalmente sin efecto, por ejemplo un servicio suspendido.</summary>
    Pausada = 2,

    /// <summary>Ya no aplica, por ejemplo un prestamo saldado.</summary>
    Finalizada = 3,
}
