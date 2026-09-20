namespace Rumbo.Contratos.Recomendaciones;

/// <summary>Sugerencia del motor de recomendaciones.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Tipo">Clase de sugerencia.</param>
/// <param name="Titulo">Titulo corto.</param>
/// <param name="Cuerpo">Explicacion en espanol.</param>
/// <param name="Insumos">
/// Datos y formula con los que se calculo, en JSON. Es lo que permite que la aplicacion
/// responda «¿de dónde sale este número?» sin que el usuario tenga que fiarse.
/// </param>
/// <param name="MontoSugerido">Importe propuesto, si lo hay.</param>
/// <param name="Moneda">Codigo ISO-4217 del importe.</param>
/// <param name="ConfianzaBaja">
/// Si se calculo con poco historial. La aplicacion debe mostrarlo como estimacion.
/// </param>
/// <param name="MetaId">Meta relacionada.</param>
/// <param name="ViajeId">Viaje relacionado.</param>
/// <param name="FechaGeneracion">Cuando se calculo.</param>
/// <param name="FechaExpiracion">Cuando deja de tener sentido mostrarla.</param>
/// <param name="Estado">Pendiente, Aceptada, Descartada o Expirada.</param>
public record RecomendacionDto(
    Guid Id,
    string Tipo,
    string Titulo,
    string Cuerpo,
    string? Insumos,
    decimal? MontoSugerido,
    string? Moneda,
    bool ConfianzaBaja,
    Guid? MetaId,
    Guid? ViajeId,
    DateTimeOffset FechaGeneracion,
    DateTimeOffset? FechaExpiracion,
    string Estado);

/// <summary>Respuesta del usuario a una sugerencia.</summary>
/// <param name="Estado">Aceptada o Descartada.</param>
/// <remarks>
/// Aceptar una recomendacion NO mueve dinero por si solo: marca la decision. El movimiento
/// se crea con la operacion correspondiente, que el usuario confirma aparte. El sistema
/// nunca transfiere por su cuenta.
/// </remarks>
public record SolicitudResponderRecomendacion(string Estado);
