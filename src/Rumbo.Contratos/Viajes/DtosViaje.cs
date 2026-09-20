namespace Rumbo.Contratos.Viajes;

/// <summary>Partida del presupuesto de un viaje con su gasto real.</summary>
/// <param name="Id">Identificador de la partida.</param>
/// <param name="Categoria">Vuelos, Hospedaje, Alimentacion, Transporte...</param>
/// <param name="MontoPlanificado">Importe previsto.</param>
/// <param name="MontoGastado">Lo gastado realmente en esa partida.</param>
/// <param name="MontoDisponible">Lo que queda. Negativo si se excedio.</param>
/// <param name="PorcentajeConsumido">Cuanto se lleva gastado de lo previsto.</param>
/// <param name="Notas">Notas libres.</param>
/// <param name="Orden">Posicion en la que se muestra.</param>
public record LineaViajeDto(
    Guid Id,
    string Categoria,
    decimal MontoPlanificado,
    decimal MontoGastado,
    decimal MontoDisponible,
    decimal PorcentajeConsumido,
    string? Notas,
    int Orden);

/// <summary>Viaje con su presupuesto, su fondo y su gasto real.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Nombre">Nombre del viaje.</param>
/// <param name="Destino">Destino.</param>
/// <param name="Descripcion">Descripcion libre.</param>
/// <param name="FechaInicio">Fecha prevista de salida.</param>
/// <param name="FechaFin">Fecha prevista de regreso.</param>
/// <param name="DuracionEnDias">Dias que dura, contando salida y regreso.</param>
/// <param name="PresupuestoTotal">Lo que se piensa gastar.</param>
/// <param name="Moneda">Codigo ISO-4217.</param>
/// <param name="NumeroViajeros">Cuantas personas viajan.</param>
/// <param name="CostoPorViajero">Presupuesto repartido entre los viajeros.</param>
/// <param name="Estado">Planificado, EnCurso, Finalizado o Cancelado.</param>
/// <param name="MetaId">Meta de ahorro que lo financia.</param>
/// <param name="FondoActual">Lo que se lleva ahorrado en esa meta.</param>
/// <param name="PorcentajeFinanciado">Que parte del presupuesto cubre el fondo.</param>
/// <param name="TotalGastado">Lo gastado realmente en el viaje.</param>
/// <param name="TotalPlanificadoEnLineas">Suma de las partidas del desglose.</param>
/// <param name="DiasHastaLaSalida">Dias que faltan. Negativo si ya paso.</param>
/// <param name="Lineas">El desglose por partidas.</param>
public record ViajeDetalle(
    Guid Id,
    string Nombre,
    string? Destino,
    string? Descripcion,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    int DuracionEnDias,
    decimal PresupuestoTotal,
    string Moneda,
    int NumeroViajeros,
    decimal CostoPorViajero,
    string Estado,
    Guid? MetaId,
    decimal FondoActual,
    decimal PorcentajeFinanciado,
    decimal TotalGastado,
    decimal TotalPlanificadoEnLineas,
    int DiasHastaLaSalida,
    IReadOnlyList<LineaViajeDto> Lineas);

/// <summary>Partida que se quiere presupuestar dentro de un viaje.</summary>
/// <param name="Categoria">Vuelos, Hospedaje, Alimentacion, Transporte...</param>
/// <param name="MontoPlanificado">Importe previsto.</param>
/// <param name="Notas">Notas libres.</param>
public record LineaViajeSolicitud(
    string Categoria,
    decimal MontoPlanificado,
    string? Notas);

/// <summary>Datos para crear o modificar un viaje.</summary>
/// <param name="Nombre">Nombre del viaje.</param>
/// <param name="Destino">Destino.</param>
/// <param name="Descripcion">Descripcion libre.</param>
/// <param name="FechaInicio">Fecha prevista de salida.</param>
/// <param name="FechaFin">Fecha prevista de regreso.</param>
/// <param name="Moneda">Codigo ISO-4217. Si se omite, la moneda base del espacio.</param>
/// <param name="NumeroViajeros">Cuantas personas viajan.</param>
/// <param name="MetaId">Meta de ahorro que lo financia, si ya existe.</param>
/// <param name="Lineas">Desglose por partidas.</param>
/// <remarks>
/// El presupuesto total no se envia: es la suma de las partidas. Permitir las dos cosas
/// dejaria abierta la puerta a que no cuadraran, y entonces habria que decidir cual manda.
/// </remarks>
public record SolicitudGuardarViaje(
    string Nombre,
    string? Destino,
    string? Descripcion,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    string? Moneda,
    int NumeroViajeros,
    Guid? MetaId,
    IReadOnlyList<LineaViajeSolicitud> Lineas);

/// <summary>Datos para cambiar el estado de un viaje.</summary>
/// <param name="Estado">Planificado, EnCurso, Finalizado o Cancelado.</param>
public record SolicitudCambiarEstadoViaje(string Estado);

/// <summary>Uno de los tres escenarios de la respuesta de viabilidad.</summary>
/// <param name="Nombre">Conservador, Esperado u Optimista.</param>
/// <param name="AporteMensualSupuesto">Cuanto se supone que se apartara al mes.</param>
/// <param name="PorcentajeDelDisponible">Que parte del excedente supone ese aporte.</param>
/// <param name="AhorroProyectado">Lo reunido el dia de salida con ese ritmo.</param>
/// <param name="Faltante">Lo que faltaria. Cero si alcanza.</param>
/// <param name="PorcentajeCubierto">Parte del viaje que quedaria cubierta.</param>
/// <param name="Alcanza">Si con ese ritmo se llega.</param>
/// <param name="MesesQueFaltarian">Meses extra necesarios si no alcanza.</param>
public record EscenarioViajeDto(
    string Nombre,
    decimal AporteMensualSupuesto,
    decimal PorcentajeDelDisponible,
    decimal AhorroProyectado,
    decimal Faltante,
    decimal PorcentajeCubierto,
    bool Alcanza,
    int? MesesQueFaltarian);

/// <summary>
/// Respuesta a «¿podemos permitirnos este viaje?».
/// </summary>
/// <param name="ViajeId">Viaje analizado.</param>
/// <param name="Nombre">Nombre del viaje.</param>
/// <param name="CostoTotal">Lo que costaria.</param>
/// <param name="FondoActual">Lo ya ahorrado para el.</param>
/// <param name="Faltante">Lo que falta por reunir hoy.</param>
/// <param name="MesesHastaLaSalida">Meses completos que quedan.</param>
/// <param name="DiasHastaLaSalida">Dias que quedan.</param>
/// <param name="AporteMensualNecesario">Lo que habria que apartar cada mes.</param>
/// <param name="DisponibleMensual">Excedente mensual del hogar, sin compromisos.</param>
/// <param name="EsfuerzoRequerido">Que parte del excedente se comeria el viaje.</param>
/// <param name="Veredicto">Si, Ajustado o No.</param>
/// <param name="Explicacion">El porque, en una frase.</param>
/// <param name="Escenarios">Los tres escenarios, del mas prudente al mas optimista.</param>
/// <param name="FechaViableMasCercana">Cuando se podria viajar sin forzar nada.</param>
/// <param name="ConfianzaBaja">Si el calculo se apoya en poco historial.</param>
/// <param name="Moneda">Moneda de todas las cifras.</param>
/// <remarks>
/// Es una <b>proyeccion para decidir</b>, no una reserva ni una promesa. No mueve dinero, no
/// crea aportes y no compromete nada: la decision sigue siendo de las personas.
/// </remarks>
public record ViabilidadViajeDto(
    Guid ViajeId,
    string Nombre,
    decimal CostoTotal,
    decimal FondoActual,
    decimal Faltante,
    int MesesHastaLaSalida,
    int DiasHastaLaSalida,
    decimal AporteMensualNecesario,
    decimal DisponibleMensual,
    decimal EsfuerzoRequerido,
    string Veredicto,
    string Explicacion,
    IReadOnlyList<EscenarioViajeDto> Escenarios,
    DateOnly? FechaViableMasCercana,
    bool ConfianzaBaja,
    string Moneda);
