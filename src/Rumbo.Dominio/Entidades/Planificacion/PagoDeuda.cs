using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Entidades.Financiero;

namespace Rumbo.Dominio.Entidades.Planificacion;

/// <summary>
/// Pago realizado contra una deuda, separando la parte que reduce el saldo de la que se va en
/// intereses.
/// </summary>
/// <remarks>
/// Distinguir capital de intereses es lo que permite explicar por que una deuda baja mas
/// despacio de lo que parece: de una cuota de RD$10,000 quiza solo RD$6,000 reducen el saldo.
/// Sin ese desglose, el usuario no entiende sus propios numeros.
/// </remarks>
public class PagoDeuda : EntidadDeEspacio
{
    /// <summary>Deuda a la que se aplica el pago.</summary>
    public Guid DeudaId { get; set; }

    /// <summary>Deuda a la que se aplica el pago.</summary>
    public Deuda? Deuda { get; set; }

    /// <summary>Movimiento que ejecuto el pago.</summary>
    public Guid? MovimientoId { get; set; }

    /// <summary>Movimiento que ejecuto el pago.</summary>
    public Movimiento? Movimiento { get; set; }

    /// <summary>Importe total pagado.</summary>
    public decimal MontoTotal { get; set; }

    /// <summary>Parte del pago que reduce el saldo pendiente.</summary>
    public decimal MontoCapital { get; set; }

    /// <summary>Parte del pago que se va en intereses.</summary>
    public decimal MontoInteres { get; set; }

    /// <summary>Otros cargos incluidos en el pago, como mora o seguros.</summary>
    public decimal MontoCargos { get; set; }

    /// <summary>Codigo ISO-4217 de la moneda.</summary>
    public required string Moneda { get; set; }

    /// <summary>Fecha del pago.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Saldo de la deuda despues de aplicar este pago.</summary>
    /// <remarks>
    /// Se guarda para tener un historial fiable aunque despues se corrija algun importe.
    /// </remarks>
    public decimal SaldoPosterior { get; set; }

    /// <summary>Notas libres.</summary>
    public string? Notas { get; set; }
}
