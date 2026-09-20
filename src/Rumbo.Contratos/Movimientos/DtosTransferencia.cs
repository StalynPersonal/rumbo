namespace Rumbo.Contratos.Movimientos;

/// <summary>
/// Datos para traspasar dinero entre dos cuentas del mismo espacio.
/// </summary>
/// <param name="CuentaOrigenId">Cuenta de la que sale el dinero.</param>
/// <param name="CuentaDestinoId">Cuenta a la que llega.</param>
/// <param name="Monto">Importe que sale, en la moneda de la cuenta origen.</param>
/// <param name="MontoDestino">
/// Importe que llega, solo si las cuentas usan monedas distintas y se quiere fijar la
/// cantidad exacta que entro. Si se omite, se calcula con la tasa de la fecha.
/// </param>
/// <param name="Fecha">Fecha contable del traspaso.</param>
/// <param name="Descripcion">Descripcion del traspaso.</param>
/// <param name="Comision">
/// Comision cobrada por la entidad. Se registra como un GASTO aparte, porque si lo es: ese
/// dinero sale del hogar de verdad, a diferencia del traspaso en si.
/// </param>
/// <param name="CategoriaComisionId">
/// Categoria con la que registrar la comision. Obligatoria si hay comision.
/// </param>
/// <param name="MetaId">
/// Meta a la que se destina el traspaso, si es un aporte al ahorro. Etiquetar el movimiento
/// es lo que permite responder despues de donde salio el dinero de una meta.
/// </param>
/// <param name="RealizadoPorUsuarioId">Persona que ordena el traspaso.</param>
public record SolicitudTransferir(
    Guid CuentaOrigenId,
    Guid CuentaDestinoId,
    decimal Monto,
    decimal? MontoDestino,
    DateOnly Fecha,
    string Descripcion,
    decimal Comision,
    Guid? CategoriaComisionId,
    Guid? MetaId,
    Guid? RealizadoPorUsuarioId);

/// <summary>Traspaso ya registrado.</summary>
/// <param name="Id">Identificador del traspaso.</param>
/// <param name="CuentaOrigenId">Cuenta de la que salio el dinero.</param>
/// <param name="NombreCuentaOrigen">Nombre de esa cuenta.</param>
/// <param name="CuentaDestinoId">Cuenta a la que llego.</param>
/// <param name="NombreCuentaDestino">Nombre de esa cuenta.</param>
/// <param name="MontoOrigen">Importe que salio.</param>
/// <param name="MontoDestino">Importe que llego.</param>
/// <param name="TasaCambioAplicada">Tasa usada si las monedas eran distintas.</param>
/// <param name="Fecha">Fecha contable.</param>
/// <param name="Descripcion">Descripcion.</param>
/// <param name="Comision">Comision cobrada.</param>
/// <param name="MovimientoOrigenId">Asiento de salida.</param>
/// <param name="MovimientoDestinoId">Asiento de entrada.</param>
/// <remarks>
/// Un traspaso son SIEMPRE dos movimientos. No aparece en los totales de ingresos ni de
/// gastos: mover RD$10,000 de la cuenta de nomina a la de ahorro no es gastar RD$10,000.
/// </remarks>
public record TransferenciaResumen(
    Guid Id,
    Guid CuentaOrigenId,
    string NombreCuentaOrigen,
    Guid CuentaDestinoId,
    string NombreCuentaDestino,
    decimal MontoOrigen,
    decimal MontoDestino,
    decimal TasaCambioAplicada,
    DateOnly Fecha,
    string Descripcion,
    decimal Comision,
    Guid MovimientoOrigenId,
    Guid MovimientoDestinoId);
