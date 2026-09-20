using Rumbo.Dominio.Comun;

namespace Rumbo.Dominio.Entidades.Financiero;

/// <summary>
/// Traspaso de dinero entre dos cuentas del mismo espacio. Agrupa los dos movimientos que lo
/// componen.
/// </summary>
/// <remarks>
/// <para>
/// El dinero no entra ni sale del hogar: solo cambia de sitio. Por eso una transferencia
/// nunca debe contarse como gasto. Si se contara, mover RD$10,000 de la cuenta de nomina a la
/// cuenta de ahorro apareceria como un gasto de RD$10,000 y el informe del mes seria falso.
/// </para>
/// <para>
/// <b>Por que dos movimientos y no uno.</b> Con un unico registro que tuviera cuenta origen y
/// cuenta destino, calcular el saldo de una cuenta exigiria sumar los movimientos normales,
/// restar las transferencias donde figura como origen y sumar aquellas donde figura como
/// destino. Toda consulta tendria que acordarse de las tres partes. Con dos asientos, el saldo
/// es siempre la suma de los movimientos de esa cuenta, sin excepciones.
/// </para>
/// <para>
/// Un aporte a una meta de ahorro es exactamente esto: una transferencia hacia la cuenta de
/// ahorro, etiquetada con la meta. Ahorrar no es gastar.
/// </para>
/// </remarks>
public class Transferencia : EntidadDeEspacio
{
    /// <summary>Movimiento de salida, en la cuenta origen.</summary>
    public Guid MovimientoOrigenId { get; set; }

    /// <summary>Movimiento de entrada, en la cuenta destino.</summary>
    public Guid MovimientoDestinoId { get; set; }

    /// <summary>Cuenta de la que sale el dinero.</summary>
    public Guid CuentaOrigenId { get; set; }

    /// <summary>Cuenta a la que llega el dinero.</summary>
    public Guid CuentaDestinoId { get; set; }

    /// <summary>Importe que sale, en la moneda de la cuenta origen.</summary>
    public decimal MontoOrigen { get; set; }

    /// <summary>Importe que llega, en la moneda de la cuenta destino.</summary>
    /// <remarks>
    /// Coincide con <see cref="MontoOrigen"/> salvo que las cuentas tengan monedas distintas.
    /// En ese caso la diferencia queda explicada por <see cref="TasaCambioAplicada"/>.
    /// </remarks>
    public decimal MontoDestino { get; set; }

    /// <summary>Tasa aplicada cuando las dos cuentas usan monedas distintas.</summary>
    public decimal TasaCambioAplicada { get; set; } = 1m;

    /// <summary>Fecha contable del traspaso.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Descripcion del traspaso.</summary>
    public required string Descripcion { get; set; }

    /// <summary>Comision cobrada por la entidad, si la hubo.</summary>
    /// <remarks>
    /// La comision SI es un gasto real del hogar y se registra como un movimiento de gasto
    /// aparte. Aqui se guarda solo como dato informativo del traspaso.
    /// </remarks>
    public decimal Comision { get; set; }
}
