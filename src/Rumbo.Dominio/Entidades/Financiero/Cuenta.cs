using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Enums;

namespace Rumbo.Dominio.Entidades.Financiero;

/// <summary>
/// Deposito de valor sobre el que se registran movimientos: una cuenta bancaria, una tarjeta
/// de credito, el efectivo o una cuenta de ahorro.
/// </summary>
/// <remarks>
/// No confundir persona con cuenta. Una persona puede tener varias cuentas, y una cuenta puede
/// ser compartida por el hogar.
/// </remarks>
public class Cuenta : EntidadDeEspacio
{
    /// <summary>Nombre visible, por ejemplo "Cuenta de nomina de Juan".</summary>
    public required string Nombre { get; set; }

    /// <summary>Naturaleza de la cuenta.</summary>
    public TipoCuenta Tipo { get; set; }

    /// <summary>Codigo ISO-4217 de la moneda en que opera la cuenta.</summary>
    public required string Moneda { get; set; }

    /// <summary>Saldo con el que se dio de alta la cuenta en Rumbo.</summary>
    /// <remarks>
    /// Es el punto de partida del libro mayor: el saldo real siempre es este importe mas la
    /// suma de todos los movimientos posteriores.
    /// </remarks>
    public decimal SaldoInicial { get; set; }

    /// <summary>Saldo actual de la cuenta.</summary>
    /// <remarks>
    /// <para>
    /// Es una INSTANTANEA, no la fuente de verdad. Se actualiza dentro de la misma transaccion
    /// de base de datos que el movimiento que lo provoca, para que nunca queden a medias.
    /// </para>
    /// <para>
    /// Se guarda por rendimiento: el panel muestra los saldos de todas las cuentas en cada
    /// apertura, y recalcularlos sumando anos de movimientos seria cada vez mas lento. La
    /// verdad sigue siendo el libro mayor, y el endpoint de reconciliacion recalcula este
    /// valor desde los movimientos y avisa si hay desviacion.
    /// </para>
    /// </remarks>
    public decimal SaldoActual { get; set; }

    /// <summary>
    /// Persona duena de la cuenta, o <c>null</c> si es una cuenta conjunta sin dueno unico.
    /// </summary>
    public Guid? PropietarioUsuarioId { get; set; }

    /// <summary>Indica si todos los miembros del espacio la consideran suya.</summary>
    public bool EsCompartida { get; set; }

    /// <summary>Indica si la cuenta sigue en uso.</summary>
    /// <remarks>
    /// Desactivar es preferible a borrar: los movimientos historicos siguen apuntando a ella.
    /// </remarks>
    public bool Activa { get; set; } = true;

    /// <summary>Entidad financiera, por ejemplo "Banco Popular".</summary>
    public string? Institucion { get; set; }

    /// <summary>Ultimos digitos del numero de cuenta o tarjeta, para reconocerla.</summary>
    /// <remarks>
    /// Solo los ultimos digitos: guardar el numero completo seria almacenar un dato sensible
    /// sin ninguna necesidad funcional.
    /// </remarks>
    public string? UltimosDigitos { get; set; }

    /// <summary>Notas libres del usuario.</summary>
    public string? Notas { get; set; }

    /// <summary>Limite de credito. Solo aplica a <see cref="TipoCuenta.TarjetaCredito"/>.</summary>
    public decimal? LimiteCredito { get; set; }

    /// <summary>Dia del mes en que cierra el estado de cuenta de la tarjeta.</summary>
    public int? DiaCorte { get; set; }

    /// <summary>Dia del mes en que vence el pago de la tarjeta.</summary>
    public int? DiaPago { get; set; }

    /// <summary>Orden en que se muestra la cuenta en las listas.</summary>
    public int Orden { get; set; }

    /// <summary>
    /// Marca de version que gestiona SQL Server para detectar escrituras simultaneas.
    /// </summary>
    /// <remarks>
    /// Si dos miembros del hogar registran un gasto sobre la misma cuenta a la vez, el segundo
    /// guardado falla con un error de concurrencia en lugar de pisar el saldo del primero. El
    /// servicio lo reintenta con el valor actualizado.
    /// </remarks>
    public byte[]? Version { get; set; }

    /// <summary>Movimientos registrados sobre esta cuenta.</summary>
    public ICollection<Movimiento> Movimientos { get; set; } = [];
}
