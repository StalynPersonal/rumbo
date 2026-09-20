namespace Rumbo.Contratos.Recurrentes;

/// <summary>Obligacion recurrente tal como se muestra en los listados.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Nombre">Nombre del servicio u obligacion.</param>
/// <param name="CategoriaId">Categoria con la que se registrara el gasto.</param>
/// <param name="NombreCategoria">Nombre de esa categoria.</param>
/// <param name="CuentaId">Cuenta desde la que se suele pagar.</param>
/// <param name="NombreCuenta">Nombre de esa cuenta.</param>
/// <param name="MontoEstimado">Importe previsto.</param>
/// <param name="Moneda">Codigo ISO-4217.</param>
/// <param name="EsMontoFijo">Si el importe es siempre el mismo o variable.</param>
/// <param name="Frecuencia">Cada cuanto se repite.</param>
/// <param name="ProximaFechaPago">Fecha del proximo vencimiento.</param>
/// <param name="UltimaFechaPago">Fecha del ultimo pago registrado.</param>
/// <param name="DiasAvisoPrevio">Dias de antelacion con que se avisa.</param>
/// <param name="Estado">Activa, Pausada o Finalizada.</param>
/// <param name="Reparto">Personal o Compartido.</param>
/// <param name="DiasParaVencer">
/// Dias que faltan para el proximo pago. Negativo si ya venció.
/// </param>
public record GastoRecurrenteDto(
    Guid Id,
    string Nombre,
    Guid CategoriaId,
    string NombreCategoria,
    Guid CuentaId,
    string NombreCuenta,
    decimal MontoEstimado,
    string Moneda,
    bool EsMontoFijo,
    string Frecuencia,
    DateOnly ProximaFechaPago,
    DateOnly? UltimaFechaPago,
    int DiasAvisoPrevio,
    string Estado,
    string Reparto,
    int DiasParaVencer);

/// <summary>Datos para crear o modificar un gasto recurrente.</summary>
/// <param name="Nombre">Nombre del servicio, por ejemplo "Internet".</param>
/// <param name="CategoriaId">Categoria de gasto.</param>
/// <param name="CuentaId">Cuenta desde la que se paga.</param>
/// <param name="MontoEstimado">Importe previsto.</param>
/// <param name="Moneda">Codigo ISO-4217. Si se omite, se usa la de la cuenta.</param>
/// <param name="EsMontoFijo">
/// Si el importe es siempre el mismo, como un alquiler, o variable, como la electricidad.
/// En los variables, las proyecciones usan el promedio de los ultimos pagos.
/// </param>
/// <param name="Frecuencia">Semanal, Quincenal, Mensual, Bimestral, Trimestral, Semestral o Anual.</param>
/// <param name="ProximaFechaPago">Fecha del proximo vencimiento.</param>
/// <param name="DiasAvisoPrevio">Dias de antelacion del aviso.</param>
/// <param name="Reparto">Personal o Compartido.</param>
/// <param name="Notas">Notas libres.</param>
public record SolicitudGuardarGastoRecurrente(
    string Nombre,
    Guid CategoriaId,
    Guid CuentaId,
    decimal MontoEstimado,
    string? Moneda,
    bool EsMontoFijo,
    string Frecuencia,
    DateOnly ProximaFechaPago,
    int DiasAvisoPrevio,
    string Reparto,
    string? Notas);

/// <summary>Ingreso recurrente tal como se muestra en los listados.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Nombre">Nombre del ingreso, por ejemplo "Salario de María".</param>
/// <param name="CategoriaId">Categoria con la que se registrara.</param>
/// <param name="NombreCategoria">Nombre de esa categoria.</param>
/// <param name="CuentaId">Cuenta en la que se ingresa.</param>
/// <param name="NombreCuenta">Nombre de esa cuenta.</param>
/// <param name="MontoEstimado">Importe previsto.</param>
/// <param name="Moneda">Codigo ISO-4217.</param>
/// <param name="EsMontoFijo">Si el importe es fijo o variable.</param>
/// <param name="Frecuencia">Cada cuanto se repite.</param>
/// <param name="ProximaFechaCobro">Fecha del proximo cobro.</param>
/// <param name="RecibidoPorUsuarioId">Persona que percibe el ingreso.</param>
/// <param name="Estado">Activa, Pausada o Finalizada.</param>
/// <param name="DiasParaCobrar">Dias que faltan para el proximo cobro.</param>
public record IngresoRecurrenteDto(
    Guid Id,
    string Nombre,
    Guid CategoriaId,
    string NombreCategoria,
    Guid CuentaId,
    string NombreCuenta,
    decimal MontoEstimado,
    string Moneda,
    bool EsMontoFijo,
    string Frecuencia,
    DateOnly ProximaFechaCobro,
    Guid? RecibidoPorUsuarioId,
    string Estado,
    int DiasParaCobrar);

/// <summary>Datos para crear o modificar un ingreso recurrente.</summary>
/// <param name="Nombre">Nombre del ingreso.</param>
/// <param name="CategoriaId">Categoria de ingreso.</param>
/// <param name="CuentaId">Cuenta en la que se recibe.</param>
/// <param name="MontoEstimado">Importe previsto.</param>
/// <param name="Moneda">Codigo ISO-4217. Si se omite, se usa la de la cuenta.</param>
/// <param name="EsMontoFijo">Si el importe es fijo o variable.</param>
/// <param name="Frecuencia">Cada cuanto se repite.</param>
/// <param name="ProximaFechaCobro">Fecha del proximo cobro.</param>
/// <param name="RecibidoPorUsuarioId">Persona que lo percibe.</param>
/// <param name="Notas">Notas libres.</param>
public record SolicitudGuardarIngresoRecurrente(
    string Nombre,
    Guid CategoriaId,
    Guid CuentaId,
    decimal MontoEstimado,
    string? Moneda,
    bool EsMontoFijo,
    string Frecuencia,
    DateOnly ProximaFechaCobro,
    Guid? RecibidoPorUsuarioId,
    string? Notas);

/// <summary>
/// Datos para confirmar que una obligacion recurrente se pago.
/// </summary>
/// <param name="Monto">
/// Importe realmente pagado. Si se omite, se usa el estimado. En los gastos variables casi
/// nunca coinciden.
/// </param>
/// <param name="Fecha">Fecha del pago. Si se omite, se usa la de vencimiento.</param>
/// <param name="PagadoPorUsuarioId">Persona que pago.</param>
/// <remarks>
/// Registrar el pago crea el movimiento real y adelanta la proxima fecha segun la
/// frecuencia. Rumbo <b>no</b> genera movimientos por su cuenta al llegar la fecha: si lo
/// hiciera, el saldo dejaria de reflejar la realidad en cuanto un pago se retrasara.
/// </remarks>
public record SolicitudRegistrarPagoRecurrente(
    decimal? Monto,
    DateOnly? Fecha,
    Guid? PagadoPorUsuarioId);
