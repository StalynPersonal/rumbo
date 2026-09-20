namespace Rumbo.Contratos.Movimientos;

/// <summary>Movimiento tal como se muestra en los listados.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Tipo">Ingreso, Gasto, Transferencia o Ajuste.</param>
/// <param name="CuentaId">Cuenta sobre la que se registro.</param>
/// <param name="NombreCuenta">Nombre de esa cuenta.</param>
/// <param name="CategoriaId">Categoria, si la tiene.</param>
/// <param name="NombreCategoria">Nombre de la categoria.</param>
/// <param name="Monto">Importe, siempre positivo.</param>
/// <param name="Signo">Direccion sobre el saldo: +1 suma, -1 resta.</param>
/// <param name="Moneda">Moneda del movimiento.</param>
/// <param name="MontoEnMonedaBase">Importe convertido a la moneda base del espacio.</param>
/// <param name="TasaEsAproximada">Si la conversion uso una tasa de una fecha anterior.</param>
/// <param name="FechaMovimiento">Fecha contable.</param>
/// <param name="Descripcion">Descripcion corta.</param>
/// <param name="Notas">Notas libres.</param>
/// <param name="MetodoPago">Medio de pago utilizado.</param>
/// <param name="Reparto">Personal o Compartido.</param>
/// <param name="PagadoPorUsuarioId">Quien puso el dinero.</param>
/// <param name="MetaId">Meta a la que se destina, si aplica.</param>
/// <param name="ViajeId">Viaje al que pertenece, si aplica.</param>
/// <param name="TransferenciaId">Traspaso del que forma parte, si aplica.</param>
/// <param name="CuentaParaIngresosYGastos">
/// Si suma a los totales de ingresos y gastos. Es <c>false</c> en transferencias y ajustes:
/// mover dinero entre cuentas propias no es ni ingresar ni gastar.
/// </param>
public record MovimientoResumen(
    Guid Id,
    string Tipo,
    Guid CuentaId,
    string NombreCuenta,
    Guid? CategoriaId,
    string? NombreCategoria,
    decimal Monto,
    int Signo,
    string Moneda,
    decimal MontoEnMonedaBase,
    bool TasaEsAproximada,
    DateOnly FechaMovimiento,
    string Descripcion,
    string? Notas,
    string? MetodoPago,
    string Reparto,
    Guid? PagadoPorUsuarioId,
    Guid? MetaId,
    Guid? ViajeId,
    Guid? TransferenciaId,
    bool CuentaParaIngresosYGastos);

/// <summary>Datos para registrar un ingreso, un gasto o un ajuste.</summary>
/// <param name="Tipo">Ingreso, Gasto o Ajuste. Las transferencias tienen su propio endpoint.</param>
/// <param name="CuentaId">Cuenta afectada.</param>
/// <param name="CategoriaId">Categoria. Obligatoria en ingresos y gastos.</param>
/// <param name="Monto">Importe, siempre positivo.</param>
/// <param name="Moneda">Moneda. Si se omite, se usa la de la cuenta.</param>
/// <param name="FechaMovimiento">Fecha contable.</param>
/// <param name="Descripcion">Descripcion corta.</param>
/// <param name="Notas">Notas libres.</param>
/// <param name="MetodoPago">Medio de pago.</param>
/// <param name="Reparto">Personal o Compartido.</param>
/// <param name="PagadoPorUsuarioId">Quien puso el dinero.</param>
/// <param name="ViajeId">Viaje al que imputarlo, si aplica.</param>
/// <param name="CategoriaViaje">
/// Partida del viaje: Vuelos, Hospedaje, Alimentacion, Transporte, Actividades, Compras,
/// Documentos, Seguro u Otros. Solo se admite acompañada de un viaje.
/// </param>
public record SolicitudRegistrarMovimiento(
    string Tipo,
    Guid CuentaId,
    Guid? CategoriaId,
    decimal Monto,
    string? Moneda,
    DateOnly FechaMovimiento,
    string Descripcion,
    string? Notas,
    string? MetodoPago,
    string Reparto,
    Guid? PagadoPorUsuarioId,
    Guid? ViajeId,
    string? CategoriaViaje = null);

/// <summary>Datos para modificar un movimiento.</summary>
/// <param name="CategoriaId">Categoria.</param>
/// <param name="Monto">Importe.</param>
/// <param name="FechaMovimiento">Fecha contable.</param>
/// <param name="Descripcion">Descripcion corta.</param>
/// <param name="Notas">Notas libres.</param>
/// <param name="MetodoPago">Medio de pago.</param>
/// <param name="Reparto">Personal o Compartido.</param>
/// <param name="PagadoPorUsuarioId">Quien puso el dinero.</param>
/// <param name="ViajeId">Viaje al que imputarlo.</param>
/// <remarks>
/// No se puede cambiar ni el tipo ni la cuenta. Cambiar la cuenta significaria mover dinero
/// de un sitio a otro, que es exactamente lo que hace una transferencia: para eso existe esa
/// operacion, que deja rastro de las dos patas.
/// </remarks>
public record SolicitudActualizarMovimiento(
    Guid? CategoriaId,
    decimal Monto,
    DateOnly FechaMovimiento,
    string Descripcion,
    string? Notas,
    string? MetodoPago,
    string Reparto,
    Guid? PagadoPorUsuarioId,
    Guid? ViajeId);

/// <summary>Filtros para consultar el libro mayor.</summary>
/// <param name="Desde">Fecha inicial, incluida.</param>
/// <param name="Hasta">Fecha final, incluida.</param>
/// <param name="CuentaId">Filtrar por cuenta.</param>
/// <param name="CategoriaId">Filtrar por categoria.</param>
/// <param name="Tipo">Filtrar por tipo de movimiento.</param>
/// <param name="Reparto">Filtrar por Personal o Compartido.</param>
/// <param name="PagadoPorUsuarioId">Filtrar por quien pago.</param>
/// <param name="ViajeId">Filtrar por viaje.</param>
/// <param name="MetaId">Filtrar por meta.</param>
/// <param name="Busqueda">Texto a buscar en la descripcion.</param>
/// <param name="Pagina">Numero de pagina, empezando en 1.</param>
/// <param name="TamanoPagina">Elementos por pagina.</param>
public record FiltroMovimientos(
    DateOnly? Desde = null,
    DateOnly? Hasta = null,
    Guid? CuentaId = null,
    Guid? CategoriaId = null,
    string? Tipo = null,
    string? Reparto = null,
    Guid? PagadoPorUsuarioId = null,
    Guid? ViajeId = null,
    Guid? MetaId = null,
    string? Busqueda = null,
    int Pagina = 1,
    int TamanoPagina = 50);
