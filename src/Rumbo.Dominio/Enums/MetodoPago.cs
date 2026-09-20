namespace Rumbo.Dominio.Enums;

/// <summary>
/// Medio concreto con el que se ejecuto un movimiento.
/// </summary>
/// <remarks>
/// Es informativo, para los reportes. El movimiento real del dinero lo determina la cuenta,
/// no este campo.
/// </remarks>
public enum MetodoPago
{
    /// <summary>Dinero en mano.</summary>
    Efectivo = 1,

    /// <summary>Tarjeta ligada a una cuenta bancaria.</summary>
    TarjetaDebito = 2,

    /// <summary>Tarjeta de credito.</summary>
    TarjetaCredito = 3,

    /// <summary>Transferencia bancaria.</summary>
    Transferencia = 4,

    /// <summary>Cheque.</summary>
    Cheque = 5,

    /// <summary>Aplicacion de pago o billetera digital.</summary>
    PagoMovil = 6,

    /// <summary>Cargo recurrente domiciliado.</summary>
    DebitoAutomatico = 7,

    /// <summary>Cualquier otro medio.</summary>
    Otro = 8,
}
