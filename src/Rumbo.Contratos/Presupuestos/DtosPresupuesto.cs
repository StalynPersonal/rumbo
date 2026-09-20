namespace Rumbo.Contratos.Presupuestos;

/// <summary>Partida de presupuesto con su consumo real.</summary>
/// <param name="Id">Identificador de la linea.</param>
/// <param name="CategoriaId">Categoria que se limita.</param>
/// <param name="NombreCategoria">Nombre de la categoria.</param>
/// <param name="MontoAsignado">Lo presupuestado.</param>
/// <param name="MontoGastado">Lo gastado realmente.</param>
/// <param name="MontoDisponible">Lo que queda. Negativo si se excedio.</param>
/// <param name="PorcentajeConsumido">Cuanto se lleva gastado.</param>
/// <param name="Nivel">Normal, Aviso, Critico o Excedido.</param>
/// <param name="RitmoDiarioNecesario">Cuanto se puede gastar al dia con lo que queda.</param>
/// <param name="ProyeccionAlCierre">Lo que se gastara si se mantiene el ritmo.</param>
public record LineaPresupuestoDto(
    Guid Id,
    Guid CategoriaId,
    string NombreCategoria,
    decimal MontoAsignado,
    decimal MontoGastado,
    decimal MontoDisponible,
    decimal PorcentajeConsumido,
    string Nivel,
    decimal? RitmoDiarioNecesario,
    decimal? ProyeccionAlCierre);

/// <summary>Presupuesto con el estado de todas sus partidas.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Nombre">Nombre del presupuesto.</param>
/// <param name="TipoPeriodo">Mensual, Trimestral, Anual o Personalizado.</param>
/// <param name="InicioPeriodo">Primer dia del periodo.</param>
/// <param name="FinPeriodo">Ultimo dia del periodo.</param>
/// <param name="Moneda">Codigo ISO-4217.</param>
/// <param name="Estado">Borrador, Activo o Cerrado.</param>
/// <param name="TotalAsignado">Suma de lo presupuestado.</param>
/// <param name="TotalGastado">Suma de lo gastado.</param>
/// <param name="TotalDisponible">Lo que queda en total.</param>
/// <param name="PorcentajeConsumido">Consumo global del presupuesto.</param>
/// <param name="EstaVigente">Si la fecha de hoy cae dentro del periodo.</param>
/// <param name="Lineas">Las partidas con su estado.</param>
public record PresupuestoDetalle(
    Guid Id,
    string Nombre,
    string TipoPeriodo,
    DateOnly InicioPeriodo,
    DateOnly FinPeriodo,
    string Moneda,
    string Estado,
    decimal TotalAsignado,
    decimal TotalGastado,
    decimal TotalDisponible,
    decimal PorcentajeConsumido,
    bool EstaVigente,
    IReadOnlyList<LineaPresupuestoDto> Lineas);

/// <summary>Partida que se quiere presupuestar.</summary>
/// <param name="CategoriaId">Categoria que se limita.</param>
/// <param name="MontoAsignado">Importe previsto.</param>
/// <param name="UmbralAviso">Porcentaje de aviso. Si se omite, el del espacio.</param>
/// <param name="UmbralCritico">Porcentaje critico. Si se omite, el del espacio.</param>
/// <param name="UmbralExcedido">Porcentaje de exceso. Si se omite, el del espacio.</param>
public record LineaPresupuestoSolicitud(
    Guid CategoriaId,
    decimal MontoAsignado,
    decimal? UmbralAviso,
    decimal? UmbralCritico,
    decimal? UmbralExcedido);

/// <summary>Datos para crear o modificar un presupuesto.</summary>
/// <param name="Nombre">Nombre del presupuesto.</param>
/// <param name="TipoPeriodo">Mensual, Trimestral, Anual o Personalizado.</param>
/// <param name="InicioPeriodo">Primer dia del periodo.</param>
/// <param name="FinPeriodo">Ultimo dia del periodo.</param>
/// <param name="Moneda">Codigo ISO-4217. Si se omite, la moneda base del espacio.</param>
/// <param name="Notas">Notas libres.</param>
/// <param name="Lineas">Partidas por categoria.</param>
public record SolicitudGuardarPresupuesto(
    string Nombre,
    string TipoPeriodo,
    DateOnly InicioPeriodo,
    DateOnly FinPeriodo,
    string? Moneda,
    string? Notas,
    IReadOnlyList<LineaPresupuestoSolicitud> Lineas);
