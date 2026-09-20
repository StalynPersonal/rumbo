namespace Rumbo.Dominio.Enums;

/// <summary>
/// Situacion de una deuda.
/// </summary>
public enum EstadoDeuda
{
    /// <summary>Con saldo pendiente.</summary>
    Activa = 1,

    /// <summary>Totalmente pagada.</summary>
    Saldada = 2,

    /// <summary>Sustituida por otra deuda en condiciones distintas.</summary>
    Refinanciada = 3,
}
