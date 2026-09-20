using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Enums;

namespace Rumbo.Dominio.Entidades.Financiero;

/// <summary>
/// Obligacion que se repite: internet, luz, alquiler, una suscripcion o la cuota de un
/// prestamo.
/// </summary>
/// <remarks>
/// <para>
/// No es un movimiento, sino la PLANTILLA de un movimiento futuro. Sirve para avisar de lo que
/// viene y para que el motor de recomendaciones sepa cuanto dinero esta ya comprometido antes
/// de sugerir ahorrar.
/// </para>
/// <para>
/// Por defecto NO genera movimientos sola: cuando llega la fecha, avisa y el usuario confirma.
/// Crear movimientos automaticos significaria que el saldo de Rumbo deja de reflejar la
/// realidad en cuanto un pago se retrase o cambie de importe.
/// </para>
/// </remarks>
public class GastoRecurrente : EntidadDeEspacio
{
    /// <summary>Nombre del servicio u obligacion, por ejemplo "Internet Claro".</summary>
    public required string Nombre { get; set; }

    /// <summary>Categoria con la que se registrara el gasto.</summary>
    public Guid CategoriaId { get; set; }

    /// <summary>Categoria con la que se registrara el gasto.</summary>
    public Categoria? Categoria { get; set; }

    /// <summary>Cuenta desde la que se suele pagar.</summary>
    public Guid CuentaId { get; set; }

    /// <summary>Cuenta desde la que se suele pagar.</summary>
    public Cuenta? Cuenta { get; set; }

    /// <summary>Importe previsto.</summary>
    public decimal MontoEstimado { get; set; }

    /// <summary>Codigo ISO-4217 de la moneda.</summary>
    public required string Moneda { get; set; }

    /// <summary>
    /// Indica si el importe es siempre el mismo (alquiler) o variable (electricidad).
    /// </summary>
    /// <remarks>
    /// El motor de recomendaciones trata distinto ambos casos: para los variables usa el
    /// promedio de los ultimos pagos en lugar del importe estimado.
    /// </remarks>
    public bool EsMontoFijo { get; set; } = true;

    /// <summary>Cada cuanto se repite.</summary>
    public Frecuencia Frecuencia { get; set; } = Frecuencia.Mensual;

    /// <summary>Fecha del proximo vencimiento.</summary>
    public DateOnly ProximaFechaPago { get; set; }

    /// <summary>Fecha del ultimo pago registrado.</summary>
    public DateOnly? UltimaFechaPago { get; set; }

    /// <summary>Dias de antelacion con que se avisa.</summary>
    public int DiasAvisoPrevio { get; set; } = 3;

    /// <summary>Situacion de la recurrencia.</summary>
    public EstadoRecurrencia Estado { get; set; } = EstadoRecurrencia.Activa;

    /// <summary>Si es compartido por el hogar o personal de alguien.</summary>
    public TipoReparto Reparto { get; set; } = TipoReparto.Compartido;

    /// <summary>Notas libres.</summary>
    public string? Notas { get; set; }
}
