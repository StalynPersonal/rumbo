using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Entidades.Financiero;

namespace Rumbo.Dominio.Entidades.Planificacion;

/// <summary>
/// Viaje planificado, con su presupuesto por partidas y su fondo de ahorro.
/// </summary>
/// <remarks>
/// <para>
/// Un viaje tiene DOS caras que conviene no confundir:
/// </para>
/// <list type="bullet">
/// <item><description>
/// El <b>presupuesto</b>: cuanto se piensa gastar, desglosado en vuelos, hospedaje, comida...
/// </description></item>
/// <item><description>
/// El <b>fondo</b>: cuanto dinero se lleva ahorrado para pagarlo. Eso es una
/// <see cref="Meta"/>, referenciada desde <see cref="MetaId"/>.
/// </description></item>
/// </list>
/// <para>
/// Los gastos del viaje SI son gastos normales del hogar, solo que etiquetados con
/// <c>ViajeId</c> para poder agruparlos y compararlos con lo presupuestado.
/// </para>
/// </remarks>
public class Viaje : EntidadDeEspacio
{
    /// <summary>Nombre del viaje, por ejemplo "Viaje a Colombia".</summary>
    public required string Nombre { get; set; }

    /// <summary>Destino.</summary>
    public string? Destino { get; set; }

    /// <summary>Descripcion libre.</summary>
    public string? Descripcion { get; set; }

    /// <summary>Fecha prevista de salida.</summary>
    public DateOnly FechaInicio { get; set; }

    /// <summary>Fecha prevista de regreso.</summary>
    public DateOnly FechaFin { get; set; }

    /// <summary>Presupuesto total previsto.</summary>
    public decimal PresupuestoTotal { get; set; }

    /// <summary>Codigo ISO-4217 de la moneda del presupuesto.</summary>
    public required string Moneda { get; set; }

    /// <summary>Cantidad de personas que viajan.</summary>
    public int NumeroViajeros { get; set; } = 1;

    /// <summary>Fase en la que se encuentra.</summary>
    public EstadoViaje Estado { get; set; } = EstadoViaje.Planificado;

    /// <summary>Meta de ahorro que financia el viaje.</summary>
    public Guid? MetaId { get; set; }

    /// <summary>Meta de ahorro que financia el viaje.</summary>
    public Meta? Meta { get; set; }

    /// <summary>Desglose del presupuesto por partidas.</summary>
    public ICollection<LineaPresupuestoViaje> Lineas { get; set; } = [];

    /// <summary>Gastos reales imputados al viaje.</summary>
    public ICollection<Movimiento> Movimientos { get; set; } = [];

    /// <summary>Duracion del viaje en dias, contando el dia de salida y el de regreso.</summary>
    public int DuracionEnDias => FechaFin.DayNumber - FechaInicio.DayNumber + 1;
}
