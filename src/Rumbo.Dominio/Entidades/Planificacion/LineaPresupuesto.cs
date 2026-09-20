using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Entidades.Financiero;

namespace Rumbo.Dominio.Entidades.Planificacion;

/// <summary>
/// Importe asignado a una categoria concreta dentro de un presupuesto.
/// </summary>
/// <remarks>
/// Los umbrales viven en la linea, y no solo en la configuracion del espacio, porque no todas
/// las partidas merecen la misma vigilancia: pasarse un 10 por ciento en Restaurantes no es lo
/// mismo que pasarse un 10 por ciento en Alquiler. Cuando estan a <c>null</c> se usan los
/// valores por defecto del espacio.
/// </remarks>
public class LineaPresupuesto : EntidadDeEspacio
{
    /// <summary>Presupuesto al que pertenece.</summary>
    public Guid PresupuestoId { get; set; }

    /// <summary>Presupuesto al que pertenece.</summary>
    public Presupuesto? Presupuesto { get; set; }

    /// <summary>Categoria que se limita.</summary>
    public Guid CategoriaId { get; set; }

    /// <summary>Categoria que se limita.</summary>
    public Categoria? Categoria { get; set; }

    /// <summary>Importe previsto para la categoria en el periodo.</summary>
    public decimal MontoAsignado { get; set; }

    /// <summary>Porcentaje de consumo a partir del cual se avisa. <c>null</c> usa el del espacio.</summary>
    public decimal? UmbralAviso { get; set; }

    /// <summary>Porcentaje a partir del cual el aviso es critico.</summary>
    public decimal? UmbralCritico { get; set; }

    /// <summary>Porcentaje a partir del cual se considera excedido.</summary>
    public decimal? UmbralExcedido { get; set; }

    /// <summary>Notas libres.</summary>
    public string? Notas { get; set; }
}
