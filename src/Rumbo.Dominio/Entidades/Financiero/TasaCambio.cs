using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Enums;

namespace Rumbo.Dominio.Entidades.Financiero;

/// <summary>
/// Valor de conversion entre dos monedas en una fecha concreta.
/// </summary>
/// <remarks>
/// <para>
/// Tabla GLOBAL, compartida por todos los espacios.
/// </para>
/// <para>
/// La clave del diseno es que las tasas se guardan POR FECHA. Un movimiento en dolares del
/// 15 de marzo debe convertirse con la tasa del 15 de marzo, no con la de hoy. Si se usara
/// siempre la tasa actual, los informes historicos cambiarian solos cada vez que se moviera el
/// tipo de cambio, y un mes ya cerrado dejaria de cuadrar.
/// </para>
/// </remarks>
public class TasaCambio : EntidadBase
{
    /// <summary>Codigo ISO-4217 de la moneda de partida.</summary>
    public required string MonedaOrigen { get; set; }

    /// <summary>Codigo ISO-4217 de la moneda de destino.</summary>
    public required string MonedaDestino { get; set; }

    /// <summary>Fecha a la que aplica esta tasa.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>
    /// Unidades de <see cref="MonedaDestino"/> que equivalen a una unidad de
    /// <see cref="MonedaOrigen"/>.
    /// </summary>
    /// <remarks>
    /// Se usan 8 decimales porque algunas divisas requieren mucha precision y un redondeo
    /// temprano se acumula al convertir miles de movimientos.
    /// </remarks>
    public decimal Tasa { get; set; }

    /// <summary>De donde salio este valor.</summary>
    public OrigenTasaCambio Origen { get; set; } = OrigenTasaCambio.Manual;

    /// <summary>Instante en que se registro.</summary>
    public DateTimeOffset FechaCreacion { get; set; }
}
