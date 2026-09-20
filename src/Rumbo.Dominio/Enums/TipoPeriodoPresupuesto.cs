namespace Rumbo.Dominio.Enums;

/// <summary>
/// Duracion del ciclo de un presupuesto.
/// </summary>
public enum TipoPeriodoPresupuesto
{
    /// <summary>El ciclo habitual, alineado con el dia de inicio de mes del espacio.</summary>
    Mensual = 1,

    /// <summary>Tres meses.</summary>
    Trimestral = 2,

    /// <summary>Doce meses.</summary>
    Anual = 3,

    /// <summary>Fechas de inicio y fin definidas a mano.</summary>
    Personalizado = 4,
}
