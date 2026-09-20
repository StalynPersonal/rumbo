namespace Rumbo.Dominio.Enums;

/// <summary>
/// Naturaleza de una cuenta donde se guarda o se mueve dinero.
/// </summary>
/// <remarks>
/// La tarjeta de credito se modela como cuenta y no como deuda suelta porque los gastos se
/// cargan contra ella igual que contra una cuenta bancaria. La deuda asociada se lleva aparte
/// en la entidad <c>Deuda</c>.
/// </remarks>
public enum TipoCuenta
{
    /// <summary>Cuenta corriente o de nomina.</summary>
    Bancaria = 1,

    /// <summary>Linea de credito. Su saldo representa deuda, no dinero disponible.</summary>
    TarjetaCredito = 2,

    /// <summary>Dinero fisico.</summary>
    Efectivo = 3,

    /// <summary>Cuenta destinada a acumular, habitualmente ligada a una meta.</summary>
    Ahorro = 4,

    /// <summary>Instrumentos de inversion cuyo valor puede variar.</summary>
    Inversion = 5,

    /// <summary>Cualquier otro deposito de valor.</summary>
    Otra = 6,
}
