using Rumbo.Dominio.Comun;

namespace Rumbo.Dominio.Entidades.Identidad;

/// <summary>
/// Preferencias ajustables de un espacio: umbrales de alerta, inicio del mes contable y
/// comportamiento del motor de recomendaciones.
/// </summary>
/// <remarks>
/// Se separa de <see cref="Espacio"/> para que la tabla principal se mantenga estable: las
/// preferencias crecen con cada version y cambian a menudo, mientras que el espacio en si casi
/// nunca cambia.
/// </remarks>
public class ConfiguracionEspacio : EntidadAuditable, IEntidadDeEspacio
{
    /// <inheritdoc />
    public Guid EspacioId { get; set; }

    /// <summary>Espacio al que pertenece esta configuracion.</summary>
    public Espacio? Espacio { get; set; }

    /// <summary>
    /// Dia del mes en que empieza el periodo contable. Normalmente 1, pero quien cobra el dia
    /// 25 puede preferir que su mes financiero empiece ese dia.
    /// </summary>
    public int DiaInicioMes { get; set; } = 1;

    /// <summary>Porcentaje de consumo del presupuesto a partir del cual se avisa.</summary>
    public decimal UmbralAvisoPresupuesto { get; set; } = 80m;

    /// <summary>Porcentaje a partir del cual el aviso pasa a ser critico.</summary>
    public decimal UmbralCriticoPresupuesto { get; set; } = 90m;

    /// <summary>Porcentaje a partir del cual se considera excedido.</summary>
    public decimal UmbralExcedidoPresupuesto { get; set; } = 100m;

    /// <summary>
    /// Meses de historial que el motor de recomendaciones usa para estimar ingresos y gastos.
    /// </summary>
    /// <remarks>
    /// Con menos de tres meses de datos las estimaciones se marcan como de confianza baja: no
    /// tiene sentido proyectar un ano a partir de dos semanas de movimientos.
    /// </remarks>
    public int MesesHistorialParaAnalisis { get; set; } = 6;

    /// <summary>Indica si el motor de recomendaciones esta activo para este espacio.</summary>
    public bool RecomendacionesActivas { get; set; } = true;

    /// <summary>Dias de antelacion con que se avisa de un pago recurrente.</summary>
    public int DiasAvisoPagoRecurrente { get; set; } = 3;
}
