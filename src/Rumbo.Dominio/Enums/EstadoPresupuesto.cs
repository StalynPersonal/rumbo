namespace Rumbo.Dominio.Enums;

/// <summary>
/// Situacion de un presupuesto respecto a su periodo.
/// </summary>
public enum EstadoPresupuesto
{
    /// <summary>Se esta preparando y todavia no vigila gastos.</summary>
    Borrador = 1,

    /// <summary>Vigente: los gastos del periodo se contabilizan contra el.</summary>
    Activo = 2,

    /// <summary>Su periodo termino. Se conserva para comparar presupuesto contra real.</summary>
    Cerrado = 3,
}
