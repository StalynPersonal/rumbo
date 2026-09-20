using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Enums;

namespace Rumbo.Dominio.Entidades.Planificacion;

/// <summary>
/// Importe previsto para una partida concreta de un viaje: vuelos, hospedaje, comida...
/// </summary>
/// <remarks>
/// Usa <see cref="CategoriaViaje"/> y no las categorias generales del espacio porque un viaje
/// se presupuesta con un vocabulario propio. "Vuelos" o "Visado" no tienen sentido en el gasto
/// corriente del hogar, y mezclarlos ensuciaria las categorias del dia a dia.
/// </remarks>
public class LineaPresupuestoViaje : EntidadDeEspacio
{
    /// <summary>Viaje al que pertenece.</summary>
    public Guid ViajeId { get; set; }

    /// <summary>Viaje al que pertenece.</summary>
    public Viaje? Viaje { get; set; }

    /// <summary>Partida que se presupuesta.</summary>
    public CategoriaViaje Categoria { get; set; }

    /// <summary>Importe previsto para la partida.</summary>
    public decimal MontoPlanificado { get; set; }

    /// <summary>Notas libres, por ejemplo "vuelo directo con equipaje".</summary>
    public string? Notas { get; set; }

    /// <summary>Orden en que se muestra.</summary>
    public int Orden { get; set; }
}
