using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Comun;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Calculadoras;

/// <summary>
/// Resultado de convertir un importe entre dos monedas.
/// </summary>
/// <param name="Monto">Importe ya convertido.</param>
/// <param name="Tasa">Tasa aplicada.</param>
/// <param name="EsAproximada">
/// Indica que no habia tasa exacta para esa fecha y se uso la anterior mas cercana.
/// </param>
public record ResultadoConversion(decimal Monto, decimal Tasa, bool EsAproximada);

/// <summary>
/// Convierte importes entre monedas usando la tasa vigente en una fecha concreta.
/// </summary>
/// <remarks>
/// <para>
/// <b>La fecha importa.</b> Un gasto en dolares del 15 de marzo debe convertirse con la tasa
/// del 15 de marzo, no con la de hoy. Si se usara siempre la actual, los informes historicos
/// cambiarian solos cada vez que se moviera el tipo de cambio y un mes ya cerrado dejaria de
/// cuadrar.
/// </para>
/// <para>
/// <b>Nunca supone una paridad de 1 a 1.</b> Si falta la tasa, primero busca la anterior mas
/// cercana y marca el resultado como aproximado; si no hay ninguna, falla con un mensaje
/// claro. Inventar una tasa produciria un importe plausible y falso, que es peor que un
/// error visible.
/// </para>
/// </remarks>
/// <param name="contexto">Acceso a las tasas de cambio.</param>
public class ConversorMonedas(IContextoRumbo contexto)
{
    /// <summary>
    /// Convierte un importe de una moneda a otra segun la tasa de una fecha.
    /// </summary>
    /// <param name="monto">Importe original.</param>
    /// <param name="monedaOrigen">Codigo ISO-4217 de partida.</param>
    /// <param name="monedaDestino">Codigo ISO-4217 de destino.</param>
    /// <param name="fecha">Fecha contable del movimiento.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El importe convertido, la tasa usada y si fue aproximada.</returns>
    /// <exception cref="ExcepcionDominio">
    /// Si no existe ninguna tasa para ese par de monedas en esa fecha ni antes.
    /// </exception>
    public async Task<ResultadoConversion> ConvertirAsync(
        decimal monto,
        string monedaOrigen,
        string monedaDestino,
        DateOnly fecha,
        CancellationToken cancelacion = default)
    {
        // Misma moneda: no hay nada que convertir y la tasa es exactamente 1.
        if (string.Equals(monedaOrigen, monedaDestino, StringComparison.OrdinalIgnoreCase))
        {
            return new ResultadoConversion(monto, 1m, false);
        }

        var directa = await BuscarTasaAsync(monedaOrigen, monedaDestino, fecha, cancelacion);

        if (directa is not null)
        {
            return new ResultadoConversion(
                Redondear(monto * directa.Value.Tasa), directa.Value.Tasa, directa.Value.Aproximada);
        }

        // Si no hay tasa directa, se prueba la inversa. Cargar USD->DOP y deducir DOP->USD
        // evita tener que mantener el doble de filas para lo mismo.
        var inversa = await BuscarTasaAsync(monedaDestino, monedaOrigen, fecha, cancelacion);

        if (inversa is not null && inversa.Value.Tasa != 0m)
        {
            var tasa = 1m / inversa.Value.Tasa;

            return new ResultadoConversion(
                Redondear(monto * tasa), tasa, inversa.Value.Aproximada);
        }

        throw new ExcepcionDominio(
            $"No hay tasa de cambio de {monedaOrigen} a {monedaDestino} para el "
            + $"{fecha:dd/MM/yyyy} ni para ninguna fecha anterior. Cárgala antes de registrar "
            + "movimientos en esa moneda.");
    }

    /// <summary>
    /// Busca la tasa de una fecha o, si no existe, la anterior mas cercana.
    /// </summary>
    /// <returns>La tasa y si hubo que recurrir a una fecha anterior.</returns>
    /// <remarks>
    /// Usar la anterior mas cercana es lo razonable: los fines de semana y los festivos no
    /// tienen cotizacion, y la del viernes sigue siendo la referencia valida el domingo.
    /// </remarks>
    private async Task<(decimal Tasa, bool Aproximada)?> BuscarTasaAsync(
        string origen,
        string destino,
        DateOnly fecha,
        CancellationToken cancelacion)
    {
        var encontrada = await contexto.TasasCambio
            .AsNoTracking()
            .Where(t => t.MonedaOrigen == origen
                        && t.MonedaDestino == destino
                        && t.Fecha <= fecha)
            .OrderByDescending(t => t.Fecha)
            .Select(t => new { t.Tasa, t.Fecha })
            .FirstOrDefaultAsync(cancelacion);

        return encontrada is null
            ? null
            : (encontrada.Tasa, encontrada.Fecha != fecha);
    }

    /// <summary>
    /// Redondea a 4 decimales, la precision con la que se guarda el dinero.
    /// </summary>
    /// <remarks>
    /// Se usa <see cref="MidpointRounding.ToEven"/>, el redondeo bancario: al repartir miles
    /// de conversiones, redondear siempre hacia arriba en los empates introduce un sesgo
    /// acumulado al alza.
    /// </remarks>
    private static decimal Redondear(decimal valor) =>
        Math.Round(valor, 4, MidpointRounding.ToEven);
}
