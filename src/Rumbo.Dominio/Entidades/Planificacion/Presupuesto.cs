using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Enums;

namespace Rumbo.Dominio.Entidades.Planificacion;

/// <summary>
/// Plan de gasto para un periodo: cuanto se pretende gastar en cada categoria.
/// </summary>
/// <remarks>
/// El presupuesto no mueve dinero ni lo reserva: es una intencion contra la que se comparan
/// los gastos reales del periodo. El gasto real siempre sale de los movimientos.
/// </remarks>
public class Presupuesto : EntidadDeEspacio
{
    /// <summary>Nombre del presupuesto, por ejemplo "Presupuesto de septiembre".</summary>
    public required string Nombre { get; set; }

    /// <summary>Duracion del ciclo.</summary>
    public TipoPeriodoPresupuesto TipoPeriodo { get; set; } = TipoPeriodoPresupuesto.Mensual;

    /// <summary>Primer dia del periodo, incluido.</summary>
    public DateOnly InicioPeriodo { get; set; }

    /// <summary>Ultimo dia del periodo, incluido.</summary>
    public DateOnly FinPeriodo { get; set; }

    /// <summary>Codigo ISO-4217 de la moneda de los importes.</summary>
    public required string Moneda { get; set; }

    /// <summary>Situacion del presupuesto.</summary>
    public EstadoPresupuesto Estado { get; set; } = EstadoPresupuesto.Activo;

    /// <summary>Notas libres.</summary>
    public string? Notas { get; set; }

    /// <summary>Partidas por categoria.</summary>
    public ICollection<LineaPresupuesto> Lineas { get; set; } = [];
}
