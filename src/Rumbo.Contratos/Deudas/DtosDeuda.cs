namespace Rumbo.Contratos.Deudas;

/// <summary>Deuda del hogar con su avance.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Nombre">Nombre de la deuda.</param>
/// <param name="Tipo">TarjetaCredito, Prestamo, Vehiculo, Hipoteca...</param>
/// <param name="Acreedor">A quien se le debe.</param>
/// <param name="MontoOriginal">Lo que se debia al principio.</param>
/// <param name="SaldoActual">Lo que se debe hoy.</param>
/// <param name="MontoPagado">Capital ya devuelto.</param>
/// <param name="PorcentajePagado">Que parte del capital se ha devuelto.</param>
/// <param name="Moneda">Codigo ISO-4217.</param>
/// <param name="TasaInteres">Tasa anual, si se conoce.</param>
/// <param name="PagoMinimo">Pago minimo exigido.</param>
/// <param name="PagoMensual">Cuota habitual.</param>
/// <param name="DiaVencimiento">Dia del mes en que vence.</param>
/// <param name="FechaInicio">Cuando se contrajo.</param>
/// <param name="FechaEstimadaLiquidacion">Cuando se terminaria de pagar.</param>
/// <param name="MesesEstimadosRestantes">
/// A la cuota actual, cuantos meses quedarian. Nulo si no hay cuota con la que estimarlo.
/// </param>
/// <param name="InteresTotalPagado">Intereses ya pagados, acumulados.</param>
/// <param name="Estado">Activa, Saldada o Refinanciada.</param>
/// <param name="CuentaVinculadaId">Cuenta desde la que se suele pagar.</param>
/// <param name="ResponsableUsuarioId">Quien la asume.</param>
/// <param name="Notas">Notas libres.</param>
public record DeudaDetalle(
    Guid Id,
    string Nombre,
    string Tipo,
    string? Acreedor,
    decimal MontoOriginal,
    decimal SaldoActual,
    decimal MontoPagado,
    decimal PorcentajePagado,
    string Moneda,
    decimal? TasaInteres,
    decimal? PagoMinimo,
    decimal? PagoMensual,
    int? DiaVencimiento,
    DateOnly FechaInicio,
    DateOnly? FechaEstimadaLiquidacion,
    int? MesesEstimadosRestantes,
    decimal InteresTotalPagado,
    string Estado,
    Guid? CuentaVinculadaId,
    Guid? ResponsableUsuarioId,
    string? Notas);

/// <summary>Pago registrado contra una deuda.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Fecha">Fecha del pago.</param>
/// <param name="MontoTotal">Lo que salio de la cuenta.</param>
/// <param name="MontoCapital">Parte que reduce la deuda.</param>
/// <param name="MontoInteres">Parte que se lleva el banco.</param>
/// <param name="MontoCargos">Comisiones y cargos.</param>
/// <param name="Moneda">Codigo ISO-4217.</param>
/// <param name="SaldoPosterior">Lo que quedaba debiendo tras el pago.</param>
/// <param name="MovimientoId">Movimiento que sacó el dinero de la cuenta.</param>
/// <param name="Notas">Notas libres.</param>
public record PagoDeudaDto(
    Guid Id,
    DateOnly Fecha,
    decimal MontoTotal,
    decimal MontoCapital,
    decimal MontoInteres,
    decimal MontoCargos,
    string Moneda,
    decimal SaldoPosterior,
    Guid? MovimientoId,
    string? Notas);

/// <summary>Datos para crear o modificar una deuda.</summary>
/// <param name="Nombre">Nombre de la deuda.</param>
/// <param name="Tipo">TarjetaCredito, Prestamo, PrestamoPersonal, Vehiculo, Hipoteca u Otra.</param>
/// <param name="Acreedor">A quien se le debe.</param>
/// <param name="MontoOriginal">Lo que se debia al principio.</param>
/// <param name="SaldoActual">
/// Lo que se debe hoy. Solo se usa al crear: despues lo mueven los pagos.
/// </param>
/// <param name="Moneda">Codigo ISO-4217. Si se omite, la moneda base del espacio.</param>
/// <param name="TasaInteres">Tasa anual, si se conoce.</param>
/// <param name="PagoMinimo">Pago minimo exigido.</param>
/// <param name="PagoMensual">Cuota habitual.</param>
/// <param name="DiaVencimiento">Dia del mes en que vence.</param>
/// <param name="FechaInicio">Cuando se contrajo.</param>
/// <param name="CuentaVinculadaId">Cuenta desde la que se suele pagar.</param>
/// <param name="ResponsableUsuarioId">Quien la asume.</param>
/// <param name="Notas">Notas libres.</param>
public record SolicitudGuardarDeuda(
    string Nombre,
    string Tipo,
    string? Acreedor,
    decimal MontoOriginal,
    decimal SaldoActual,
    string? Moneda,
    decimal? TasaInteres,
    decimal? PagoMinimo,
    decimal? PagoMensual,
    int? DiaVencimiento,
    DateOnly FechaInicio,
    Guid? CuentaVinculadaId,
    Guid? ResponsableUsuarioId,
    string? Notas);

/// <summary>Datos para registrar un pago de deuda.</summary>
/// <param name="CuentaOrigenId">Cuenta de la que sale el dinero.</param>
/// <param name="CategoriaId">Categoria con la que se clasifica el gasto.</param>
/// <param name="MontoCapital">Parte que reduce la deuda.</param>
/// <param name="MontoInteres">Parte que se lleva el banco.</param>
/// <param name="MontoCargos">Comisiones y cargos.</param>
/// <param name="Fecha">Fecha del pago.</param>
/// <param name="Descripcion">Descripcion del movimiento.</param>
/// <param name="Notas">Notas libres.</param>
/// <remarks>
/// El importe total no se envia: es capital + interes + cargos. Enviar las dos cosas dejaria
/// abierta la puerta a que no cuadraran.
/// </remarks>
public record SolicitudPagarDeuda(
    Guid CuentaOrigenId,
    Guid? CategoriaId,
    decimal MontoCapital,
    decimal MontoInteres,
    decimal MontoCargos,
    DateOnly Fecha,
    string? Descripcion,
    string? Notas);

/// <summary>Datos para cambiar el estado de una deuda.</summary>
/// <param name="Estado">Activa, Saldada o Refinanciada.</param>
public record SolicitudCambiarEstadoDeuda(string Estado);
