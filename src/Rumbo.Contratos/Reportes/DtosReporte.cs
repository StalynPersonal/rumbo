namespace Rumbo.Contratos.Reportes;

/// <summary>Filtros comunes de los informes.</summary>
/// <param name="Desde">Fecha inicial, inclusive.</param>
/// <param name="Hasta">Fecha final, inclusive.</param>
/// <param name="CuentaId">Limitar a una cuenta.</param>
/// <param name="CategoriaId">Limitar a una categoria.</param>
/// <param name="UsuarioId">Limitar a quien puso el dinero.</param>
/// <param name="Reparto">Personal o Compartido.</param>
public record FiltroReporte(
    DateOnly? Desde,
    DateOnly? Hasta,
    Guid? CuentaId,
    Guid? CategoriaId,
    Guid? UsuarioId,
    string? Reparto);

/// <summary>Un punto de la serie mensual.</summary>
/// <param name="Anio">Ano del mes.</param>
/// <param name="Mes">Numero de mes, de 1 a 12.</param>
/// <param name="Etiqueta">Nombre legible del mes, por ejemplo «septiembre 2026».</param>
/// <param name="Ingresos">Total de ingresos del mes.</param>
/// <param name="Gastos">Total de gastos del mes.</param>
/// <param name="Balance">Ingresos menos gastos.</param>
public record PuntoMensual(
    int Anio,
    int Mes,
    string Etiqueta,
    decimal Ingresos,
    decimal Gastos,
    decimal Balance);

/// <summary>Gasto acumulado de una categoria.</summary>
/// <param name="CategoriaId">Categoria.</param>
/// <param name="Nombre">Nombre de la categoria.</param>
/// <param name="NombrePadre">Categoria de primer nivel a la que pertenece.</param>
/// <param name="Total">Importe acumulado.</param>
/// <param name="Porcentaje">Que parte del total representa.</param>
/// <param name="CantidadMovimientos">Cuantos asientos lo componen.</param>
public record TotalPorCategoria(
    Guid CategoriaId,
    string Nombre,
    string? NombrePadre,
    decimal Total,
    decimal Porcentaje,
    int CantidadMovimientos);

/// <summary>Aportacion de cada persona al gasto compartido.</summary>
/// <param name="UsuarioId">Persona.</param>
/// <param name="Nombre">Nombre de la persona.</param>
/// <param name="TotalPagado">Lo que puso de su bolsillo en gastos compartidos.</param>
/// <param name="ParteQueLeToca">Lo que le correspondería a partes iguales.</param>
/// <param name="Diferencia">
/// Positivo si puso de mas y el hogar le debe; negativo si puso de menos.
/// </param>
public record AporteDeMiembro(
    Guid UsuarioId,
    string Nombre,
    decimal TotalPagado,
    decimal ParteQueLeToca,
    decimal Diferencia);

/// <summary>Resumen de ingresos y gastos de un periodo.</summary>
/// <param name="Desde">Primer dia del periodo analizado.</param>
/// <param name="Hasta">Ultimo dia del periodo analizado.</param>
/// <param name="TotalIngresos">Ingresos del periodo.</param>
/// <param name="TotalGastos">Gastos del periodo.</param>
/// <param name="Balance">Ingresos menos gastos.</param>
/// <param name="TasaDeAhorro">Que parte de lo ingresado quedo sin gastar, en porcentaje.</param>
/// <param name="CantidadMovimientos">Cuantos asientos entraron en el calculo.</param>
/// <param name="Moneda">Moneda base en la que se consolida todo.</param>
/// <remarks>
/// Las transferencias <b>nunca</b> entran: mover dinero entre cuentas propias no es ingresar
/// ni gastar.
/// </remarks>
public record ResumenPeriodo(
    DateOnly Desde,
    DateOnly Hasta,
    decimal TotalIngresos,
    decimal TotalGastos,
    decimal Balance,
    decimal TasaDeAhorro,
    int CantidadMovimientos,
    string Moneda);

/// <summary>Informe de gastos por categoria.</summary>
/// <param name="Resumen">Totales del periodo.</param>
/// <param name="Categorias">Desglose, de mayor a menor gasto.</param>
public record ReporteCategorias(
    ResumenPeriodo Resumen,
    IReadOnlyList<TotalPorCategoria> Categorias);

/// <summary>Informe de evolucion mensual.</summary>
/// <param name="Resumen">Totales del periodo completo.</param>
/// <param name="Meses">Serie mensual, del mes mas antiguo al mas reciente.</param>
/// <param name="PromedioIngresos">Media mensual de ingresos.</param>
/// <param name="PromedioGastos">Media mensual de gastos.</param>
public record ReporteMensual(
    ResumenPeriodo Resumen,
    IReadOnlyList<PuntoMensual> Meses,
    decimal PromedioIngresos,
    decimal PromedioGastos);

/// <summary>Informe de reparto del gasto compartido entre los miembros.</summary>
/// <param name="Desde">Primer dia analizado.</param>
/// <param name="Hasta">Ultimo dia analizado.</param>
/// <param name="TotalCompartido">Gasto compartido del periodo.</param>
/// <param name="Miembros">Lo que puso cada persona y su desbalance.</param>
/// <param name="Moneda">Moneda base.</param>
/// <remarks>
/// Rumbo <b>registra y reporta</b> el desbalance, pero no liquida entre personas ni mueve
/// dinero por su cuenta. Quien salda y como es una conversacion del hogar.
/// </remarks>
public record ReporteReparto(
    DateOnly Desde,
    DateOnly Hasta,
    decimal TotalCompartido,
    IReadOnlyList<AporteDeMiembro> Miembros,
    string Moneda);
