using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Entidades.Financiero;

namespace Rumbo.Dominio.Entidades.Planificacion;

/// <summary>
/// Obligacion pendiente de pago: una tarjeta, un prestamo, un vehiculo o una hipoteca.
/// </summary>
/// <remarks>
/// <para>
/// La deuda se lleva aparte de las cuentas porque responde a preguntas distintas: una cuenta
/// dice cuanto dinero hay, una deuda dice cuanto se debe, a que interes y hasta cuando.
/// </para>
/// <para>
/// En esta version no se calculan tablas de amortizacion ni estrategias de pago. Los campos
/// necesarios para hacerlo mas adelante ya estan, de modo que anadirlo no exigira migrar
/// datos.
/// </para>
/// </remarks>
public class Deuda : EntidadDeEspacio
{
    /// <summary>Nombre de la deuda, por ejemplo "Prestamo del vehiculo".</summary>
    public required string Nombre { get; set; }

    /// <summary>Naturaleza de la obligacion.</summary>
    public TipoDeuda Tipo { get; set; }

    /// <summary>Acreedor, por ejemplo "Banco Popular".</summary>
    public string? Acreedor { get; set; }

    /// <summary>Importe original de la deuda.</summary>
    public decimal MontoOriginal { get; set; }

    /// <summary>Importe que queda por pagar.</summary>
    public decimal SaldoActual { get; set; }

    /// <summary>Codigo ISO-4217 de la moneda.</summary>
    public required string Moneda { get; set; }

    /// <summary>Tasa de interes anual, en porcentaje. Por ejemplo <c>18.5</c>.</summary>
    public decimal? TasaInteres { get; set; }

    /// <summary>Pago minimo exigido cada periodo.</summary>
    public decimal? PagoMinimo { get; set; }

    /// <summary>Cuota que se paga habitualmente.</summary>
    public decimal? PagoMensual { get; set; }

    /// <summary>Dia del mes en que vence la cuota.</summary>
    public int? DiaVencimiento { get; set; }

    /// <summary>Fecha en que se contrajo.</summary>
    public DateOnly FechaInicio { get; set; }

    /// <summary>Fecha en que se espera terminar de pagarla.</summary>
    public DateOnly? FechaEstimadaLiquidacion { get; set; }

    /// <summary>Situacion de la deuda.</summary>
    public EstadoDeuda Estado { get; set; } = EstadoDeuda.Activa;

    /// <summary>
    /// Cuenta asociada. En una tarjeta de credito es la propia cuenta de tipo tarjeta.
    /// </summary>
    public Guid? CuentaVinculadaId { get; set; }

    /// <summary>Cuenta asociada.</summary>
    public Cuenta? CuentaVinculada { get; set; }

    /// <summary>Persona responsable, o <c>null</c> si es del hogar.</summary>
    public Guid? ResponsableUsuarioId { get; set; }

    /// <summary>Notas libres.</summary>
    public string? Notas { get; set; }

    /// <summary>Pagos realizados.</summary>
    public ICollection<PagoDeuda> Pagos { get; set; } = [];

    /// <summary>Porcentaje ya pagado, entre 0 y 100.</summary>
    public decimal PorcentajePagado =>
        MontoOriginal <= 0m ? 100m
        : Math.Clamp((MontoOriginal - SaldoActual) / MontoOriginal * 100m, 0m, 100m);
}
