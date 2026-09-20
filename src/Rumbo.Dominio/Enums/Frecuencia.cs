namespace Rumbo.Dominio.Enums;

/// <summary>
/// Periodicidad de una obligacion o de un ingreso recurrente.
/// </summary>
public enum Frecuencia
{
    /// <summary>Cada 7 dias.</summary>
    Semanal = 1,

    /// <summary>Dos veces al mes.</summary>
    Quincenal = 2,

    /// <summary>Una vez al mes.</summary>
    Mensual = 3,

    /// <summary>Cada dos meses.</summary>
    Bimestral = 4,

    /// <summary>Cada tres meses.</summary>
    Trimestral = 5,

    /// <summary>Cada seis meses.</summary>
    Semestral = 6,

    /// <summary>Una vez al ano.</summary>
    Anual = 7,
}
