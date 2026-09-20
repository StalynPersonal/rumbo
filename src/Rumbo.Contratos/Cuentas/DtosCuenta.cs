namespace Rumbo.Contratos.Cuentas;

/// <summary>Cuenta tal como se muestra en los listados.</summary>
/// <param name="Id">Identificador de la cuenta.</param>
/// <param name="Nombre">Nombre visible.</param>
/// <param name="Tipo">Bancaria, TarjetaCredito, Efectivo, Ahorro, Inversion u Otra.</param>
/// <param name="Moneda">Codigo ISO-4217.</param>
/// <param name="SaldoActual">Saldo en la moneda de la cuenta.</param>
/// <param name="SaldoEnMonedaBase">
/// El mismo saldo convertido a la moneda base del espacio, para poder sumar cuentas de
/// monedas distintas en el panel.
/// </param>
/// <param name="PropietarioUsuarioId">Persona duena, o <c>null</c> si es conjunta.</param>
/// <param name="EsCompartida">Si el hogar la considera de todos.</param>
/// <param name="Activa">Si sigue en uso.</param>
/// <param name="Institucion">Entidad financiera.</param>
/// <param name="UltimosDigitos">Ultimos digitos, para reconocerla.</param>
/// <param name="LimiteCredito">Limite, solo en tarjetas de credito.</param>
/// <param name="DisponibleTarjeta">
/// Credito que queda disponible en una tarjeta. Es <c>null</c> en el resto de cuentas.
/// </param>
/// <param name="Orden">Posicion en la que se muestra.</param>
public record CuentaResumen(
    Guid Id,
    string Nombre,
    string Tipo,
    string Moneda,
    decimal SaldoActual,
    decimal SaldoEnMonedaBase,
    Guid? PropietarioUsuarioId,
    bool EsCompartida,
    bool Activa,
    string? Institucion,
    string? UltimosDigitos,
    decimal? LimiteCredito,
    decimal? DisponibleTarjeta,
    int Orden);

/// <summary>Datos para crear una cuenta.</summary>
/// <param name="Nombre">Nombre visible.</param>
/// <param name="Tipo">Tipo de cuenta.</param>
/// <param name="Moneda">Codigo ISO-4217.</param>
/// <param name="SaldoInicial">
/// Saldo con el que se da de alta. Es el punto de partida del libro mayor: el saldo real
/// siempre sera este importe mas la suma de los movimientos posteriores.
/// </param>
/// <param name="PropietarioUsuarioId">Persona duena, o <c>null</c> si es conjunta.</param>
/// <param name="EsCompartida">Si el hogar la considera de todos.</param>
/// <param name="Institucion">Entidad financiera.</param>
/// <param name="UltimosDigitos">Ultimos digitos del numero, nunca el numero completo.</param>
/// <param name="Notas">Notas libres.</param>
/// <param name="LimiteCredito">Limite, solo en tarjetas de credito.</param>
/// <param name="DiaCorte">Dia de cierre del estado de cuenta, entre 1 y 28.</param>
/// <param name="DiaPago">Dia de vencimiento del pago, entre 1 y 28.</param>
public record SolicitudCrearCuenta(
    string Nombre,
    string Tipo,
    string Moneda,
    decimal SaldoInicial,
    Guid? PropietarioUsuarioId,
    bool EsCompartida,
    string? Institucion,
    string? UltimosDigitos,
    string? Notas,
    decimal? LimiteCredito,
    int? DiaCorte,
    int? DiaPago);

/// <summary>Datos para modificar una cuenta.</summary>
/// <param name="Nombre">Nombre visible.</param>
/// <param name="PropietarioUsuarioId">Persona duena.</param>
/// <param name="EsCompartida">Si el hogar la considera de todos.</param>
/// <param name="Activa">Si sigue en uso.</param>
/// <param name="Institucion">Entidad financiera.</param>
/// <param name="UltimosDigitos">Ultimos digitos.</param>
/// <param name="Notas">Notas libres.</param>
/// <param name="LimiteCredito">Limite de la tarjeta.</param>
/// <param name="DiaCorte">Dia de cierre.</param>
/// <param name="DiaPago">Dia de vencimiento.</param>
/// <param name="Orden">Posicion en la que se muestra.</param>
/// <remarks>
/// Ni el tipo ni la moneda ni el saldo inicial se pueden cambiar: los movimientos ya
/// registrados dependen de ellos y cambiarlos descuadraria el historial. Si hacen falta
/// distintos, lo correcto es crear otra cuenta.
/// </remarks>
public record SolicitudActualizarCuenta(
    string Nombre,
    Guid? PropietarioUsuarioId,
    bool EsCompartida,
    bool Activa,
    string? Institucion,
    string? UltimosDigitos,
    string? Notas,
    decimal? LimiteCredito,
    int? DiaCorte,
    int? DiaPago,
    int Orden);

/// <summary>Resultado de reconciliar el saldo de una cuenta con su libro mayor.</summary>
/// <param name="CuentaId">Cuenta reconciliada.</param>
/// <param name="SaldoRegistrado">Saldo que tenia guardado la cuenta.</param>
/// <param name="SaldoCalculado">Saldo que resulta de sumar el libro mayor.</param>
/// <param name="Desviacion">Diferencia entre ambos. Deberia ser cero.</param>
/// <param name="Corregido">Si se aplico la correccion.</param>
/// <param name="CantidadMovimientos">Movimientos considerados en el calculo.</param>
public record ResultadoReconciliacion(
    Guid CuentaId,
    decimal SaldoRegistrado,
    decimal SaldoCalculado,
    decimal Desviacion,
    bool Corregido,
    int CantidadMovimientos);
