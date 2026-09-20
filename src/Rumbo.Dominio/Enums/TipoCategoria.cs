namespace Rumbo.Dominio.Enums;

/// <summary>
/// Indica en que clase de movimientos puede usarse una categoria.
/// </summary>
public enum TipoCategoria
{
    /// <summary>Solo en ingresos, por ejemplo Salario.</summary>
    Ingreso = 1,

    /// <summary>Solo en gastos, por ejemplo Supermercado.</summary>
    Gasto = 2,

    /// <summary>En ambos, por ejemplo Otros.</summary>
    Ambos = 3,
}
