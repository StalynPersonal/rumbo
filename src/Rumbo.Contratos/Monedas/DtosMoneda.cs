namespace Rumbo.Contratos.Monedas;

/// <summary>Moneda del catalogo.</summary>
/// <param name="Codigo">Codigo ISO-4217 de tres letras.</param>
/// <param name="Nombre">Nombre en espanol.</param>
/// <param name="Simbolo">Simbolo para mostrar importes.</param>
/// <param name="Decimales">Decimales con que se expresa habitualmente.</param>
/// <param name="Activa">Si puede elegirse al crear cuentas o registrar movimientos.</param>
public record MonedaDto(string Codigo, string Nombre, string Simbolo, int Decimales, bool Activa);

/// <summary>Tasa de cambio entre dos monedas en una fecha.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="MonedaOrigen">Codigo de partida.</param>
/// <param name="MonedaDestino">Codigo de destino.</param>
/// <param name="Fecha">Fecha a la que aplica.</param>
/// <param name="Tasa">Unidades de destino por cada unidad de origen.</param>
/// <param name="Origen">Manual, Api o Semilla.</param>
public record TasaCambioDto(
    Guid Id,
    string MonedaOrigen,
    string MonedaDestino,
    DateOnly Fecha,
    decimal Tasa,
    string Origen);

/// <summary>Datos para registrar una tasa de cambio.</summary>
/// <param name="MonedaOrigen">Codigo de partida, por ejemplo USD.</param>
/// <param name="MonedaDestino">Codigo de destino, por ejemplo DOP.</param>
/// <param name="Fecha">Fecha a la que aplica.</param>
/// <param name="Tasa">
/// Cuantas unidades de destino equivalen a UNA de origen. Para USD a DOP con el dólar a 60
/// pesos, la tasa es 60.
/// </param>
/// <remarks>
/// Registrar la misma fecha y par de monedas dos veces ACTUALIZA la tasa existente en lugar
/// de crear otra: dos valores distintos para el mismo día harían que el mismo movimiento se
/// convirtiera de forma diferente según cuál se leyera.
/// </remarks>
public record SolicitudGuardarTasa(
    string MonedaOrigen,
    string MonedaDestino,
    DateOnly Fecha,
    decimal Tasa);

/// <summary>Resultado de convertir un importe, con su explicación.</summary>
/// <param name="MontoOriginal">Importe de partida.</param>
/// <param name="MonedaOrigen">Moneda de partida.</param>
/// <param name="MontoConvertido">Importe resultante.</param>
/// <param name="MonedaDestino">Moneda de destino.</param>
/// <param name="Tasa">Tasa aplicada.</param>
/// <param name="Fecha">Fecha de la conversión.</param>
/// <param name="EsAproximada">
/// Indica que no había tasa para esa fecha exacta y se usó la anterior más cercana.
/// </param>
public record ResultadoConversionDto(
    decimal MontoOriginal,
    string MonedaOrigen,
    decimal MontoConvertido,
    string MonedaDestino,
    decimal Tasa,
    DateOnly Fecha,
    bool EsAproximada);
