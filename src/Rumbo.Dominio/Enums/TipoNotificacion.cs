namespace Rumbo.Dominio.Enums;

/// <summary>
/// Motivo por el que se avisa al usuario.
/// </summary>
public enum TipoNotificacion
{
    /// <summary>Un gasto recurrente vence pronto.</summary>
    ProximoPago = 1,

    /// <summary>Una partida alcanzo su umbral de aviso.</summary>
    PresupuestoCercaDelLimite = 2,

    /// <summary>Una partida supero su monto asignado.</summary>
    PresupuestoExcedido = 3,

    /// <summary>Una meta no avanza al ritmo necesario.</summary>
    MetaAtrasada = 4,

    /// <summary>Se completo una meta.</summary>
    MetaAlcanzada = 5,

    /// <summary>Otro miembro registro un ingreso.</summary>
    NuevoIngreso = 6,

    /// <summary>Aviso generico programado.</summary>
    Recordatorio = 7,
}
