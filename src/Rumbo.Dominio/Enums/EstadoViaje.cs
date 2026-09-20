namespace Rumbo.Dominio.Enums;

/// <summary>
/// Fase en la que se encuentra un viaje.
/// </summary>
public enum EstadoViaje
{
    /// <summary>Todavia se esta ahorrando para el.</summary>
    Planificado = 1,

    /// <summary>Ya comenzo: los gastos que se registren son gastos reales del viaje.</summary>
    EnCurso = 2,

    /// <summary>Termino. Permite comparar presupuesto contra gasto real.</summary>
    Finalizado = 3,

    /// <summary>No se realizara.</summary>
    Cancelado = 4,
}
