namespace Rumbo.Contratos.Metas;

/// <summary>Meta de ahorro con su proyeccion.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Nombre">Nombre de la meta.</param>
/// <param name="Descripcion">Para que es.</param>
/// <param name="MontoObjetivo">Lo que se quiere reunir.</param>
/// <param name="MontoActual">Lo reunido.</param>
/// <param name="MontoFaltante">Lo que queda. Nunca negativo.</param>
/// <param name="PorcentajeCompletado">Progreso, entre 0 y 100.</param>
/// <param name="Moneda">Codigo ISO-4217.</param>
/// <param name="FechaObjetivo">Fecha limite, si la tiene.</param>
/// <param name="DiasRestantes">Dias hasta la fecha objetivo.</param>
/// <param name="MesesRestantes">Meses completos hasta la fecha objetivo.</param>
/// <param name="AporteMensualNecesario">Lo que habria que aportar al mes.</param>
/// <param name="AporteSemanalNecesario">Lo mismo repartido por semanas.</param>
/// <param name="AporteMensualMinimo">Lo que el usuario se comprometio a aportar.</param>
/// <param name="VaAtrasada">
/// Si el aporte necesario supera al que el usuario se fijo: al ritmo prometido no llegaria.
/// </param>
/// <param name="PlazoVencido">Si la fecha pasó sin alcanzarla.</param>
/// <param name="Prioridad">Baja, Media, Alta o Critica.</param>
/// <param name="Estado">Activa, Pausada, Alcanzada o Cancelada.</param>
/// <param name="CuentaVinculadaId">Cuenta de ahorro donde se acumula.</param>
/// <param name="Icono">Icono que la representa.</param>
public record MetaDetalle(
    Guid Id,
    string Nombre,
    string? Descripcion,
    decimal MontoObjetivo,
    decimal MontoActual,
    decimal MontoFaltante,
    decimal PorcentajeCompletado,
    string Moneda,
    DateOnly? FechaObjetivo,
    int? DiasRestantes,
    int? MesesRestantes,
    decimal? AporteMensualNecesario,
    decimal? AporteSemanalNecesario,
    decimal? AporteMensualMinimo,
    bool VaAtrasada,
    bool PlazoVencido,
    string Prioridad,
    string Estado,
    Guid? CuentaVinculadaId,
    string? Icono);

/// <summary>Datos para crear o modificar una meta.</summary>
/// <param name="Nombre">Nombre de la meta.</param>
/// <param name="Descripcion">Para que es.</param>
/// <param name="MontoObjetivo">Lo que se quiere reunir.</param>
/// <param name="Moneda">Codigo ISO-4217. Si se omite, la moneda base del espacio.</param>
/// <param name="FechaObjetivo">Fecha limite, opcional.</param>
/// <param name="Prioridad">Baja, Media, Alta o Critica.</param>
/// <param name="AporteMensualMinimo">Lo que la persona se compromete a aportar.</param>
/// <param name="CuentaVinculadaId">Cuenta de ahorro donde se acumulara.</param>
/// <param name="Icono">Icono que la representa.</param>
public record SolicitudGuardarMeta(
    string Nombre,
    string? Descripcion,
    decimal MontoObjetivo,
    string? Moneda,
    DateOnly? FechaObjetivo,
    string Prioridad,
    decimal? AporteMensualMinimo,
    Guid? CuentaVinculadaId,
    string? Icono);

/// <summary>Aporte registrado a una meta.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Monto">Importe aportado.</param>
/// <param name="Moneda">Codigo ISO-4217.</param>
/// <param name="Fecha">Fecha del aporte.</param>
/// <param name="MovimientoId">Movimiento que movio el dinero.</param>
/// <param name="AportadoPorUsuarioId">Quien aporto.</param>
/// <param name="OrigenRecomendacion">Si nacio de aceptar una sugerencia del sistema.</param>
public record AporteMetaDto(
    Guid Id,
    decimal Monto,
    string Moneda,
    DateOnly Fecha,
    Guid? MovimientoId,
    Guid? AportadoPorUsuarioId,
    bool OrigenRecomendacion);

/// <summary>
/// Datos para aportar a una meta desde una cuenta.
/// </summary>
/// <param name="CuentaOrigenId">Cuenta de la que sale el dinero.</param>
/// <param name="Monto">Importe del aporte.</param>
/// <param name="Fecha">Fecha del aporte.</param>
/// <param name="Descripcion">Descripcion del traspaso.</param>
/// <param name="OrigenRecomendacion">Si el usuario acepto una sugerencia del sistema.</param>
/// <remarks>
/// Aportar a una meta es una <b>transferencia</b> hacia su cuenta de ahorro, no un gasto.
/// Ahorrar no es gastar: contarlo como gasto haria que ahorrar pareciera empobrecer.
/// </remarks>
public record SolicitudAportarAMeta(
    Guid CuentaOrigenId,
    decimal Monto,
    DateOnly Fecha,
    string? Descripcion,
    bool OrigenRecomendacion);

/// <summary>Datos para cambiar el estado de una meta.</summary>
/// <param name="Estado">Activa, Pausada, Alcanzada o Cancelada.</param>
public record SolicitudCambiarEstadoMeta(string Estado);
