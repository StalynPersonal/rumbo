using Rumbo.Contratos.Monedas;

namespace Rumbo.Aplicacion.Contratos;

/// <summary>
/// Catalogo de monedas y tasas de cambio.
/// </summary>
/// <remarks>
/// Son datos GLOBALES, compartidos por todos los espacios: el catalogo ISO-4217 y las
/// cotizaciones son informacion publica, no de un hogar concreto.
/// </remarks>
public interface IServicioMonedas
{
    /// <summary>Lista las monedas disponibles.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El catalogo.</returns>
    Task<IReadOnlyList<MonedaDto>> ListarMonedasAsync(CancellationToken cancelacion = default);

    /// <summary>Lista las tasas registradas para un par de monedas.</summary>
    /// <param name="monedaOrigen">Codigo de partida.</param>
    /// <param name="monedaDestino">Codigo de destino.</param>
    /// <param name="desde">Fecha inicial, opcional.</param>
    /// <param name="hasta">Fecha final, opcional.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Las tasas, de la mas reciente a la mas antigua.</returns>
    Task<IReadOnlyList<TasaCambioDto>> ListarTasasAsync(
        string monedaOrigen,
        string monedaDestino,
        DateOnly? desde = null,
        DateOnly? hasta = null,
        CancellationToken cancelacion = default);

    /// <summary>Registra o actualiza una tasa de cambio.</summary>
    /// <param name="solicitud">Par de monedas, fecha y valor.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La tasa guardada.</returns>
    /// <remarks>
    /// Si ya existe una tasa para ese par y esa fecha, se ACTUALIZA. Tener dos valores para
    /// el mismo dia haria que el mismo movimiento se convirtiera distinto segun cual se
    /// leyera.
    /// </remarks>
    Task<TasaCambioDto> GuardarTasaAsync(
        SolicitudGuardarTasa solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Elimina una tasa registrada.</summary>
    /// <param name="tasaId">Tasa que se elimina.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando queda eliminada.</returns>
    /// <remarks>
    /// Los movimientos ya registrados NO cambian: guardan su importe convertido y la tasa
    /// que se les aplico, congelados en el momento del registro.
    /// </remarks>
    Task EliminarTasaAsync(Guid tasaId, CancellationToken cancelacion = default);

    /// <summary>
    /// Convierte un importe entre dos monedas, mostrando la tasa usada.
    /// </summary>
    /// <param name="monto">Importe de partida.</param>
    /// <param name="monedaOrigen">Codigo de partida.</param>
    /// <param name="monedaDestino">Codigo de destino.</param>
    /// <param name="fecha">Fecha de la conversion.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El importe convertido y su explicacion.</returns>
    /// <remarks>
    /// Util para previsualizar en la aplicacion antes de registrar un movimiento en otra
    /// moneda.
    /// </remarks>
    Task<ResultadoConversionDto> ConvertirAsync(
        decimal monto,
        string monedaOrigen,
        string monedaDestino,
        DateOnly fecha,
        CancellationToken cancelacion = default);
}
