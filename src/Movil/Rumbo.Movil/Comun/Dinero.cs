using System.Globalization;

namespace Rumbo.Movil.Comun;

/// <summary>
/// Formatea importes y porcentajes.
/// </summary>
/// <remarks>
/// El formateo ocurre en el MOVIL, no en la API. El servidor manda numeros crudos y cada
/// cliente los presenta como le corresponda; asi un importe nunca viaja ya convertido en
/// texto con el separador equivocado.
///
/// Esta clase existe para no repetir la cultura en diez modelos de vista, que es la forma
/// habitual de acabar con una pantalla mostrando 1,234.50 y otra 1.234,50.
/// </remarks>
public static class Dinero
{
    /// <summary>Cultura dominicana: RD$ 50,000.00 y nombres de mes en espanol.</summary>
    public static readonly CultureInfo Cultura = new("es-DO");

    /// <summary>Convierte un importe en texto.</summary>
    /// <param name="valor">Importe.</param>
    /// <returns>El texto, o un guion si no hay dato.</returns>
    public static string Formatear(decimal? valor) =>
        valor is null ? "—" : valor.Value.ToString("C2", Cultura);

    /// <summary>Convierte un porcentaje en texto.</summary>
    /// <param name="valor">Porcentaje, de 0 a 100.</param>
    /// <returns>El texto con el simbolo.</returns>
    public static string Porcentaje(decimal? valor) =>
        valor is null ? "—" : valor.Value.ToString("0.#", Cultura) + " %";

    /// <summary>Lee un importe escrito por una persona.</summary>
    /// <param name="texto">Lo que se escribio.</param>
    /// <returns>El importe, o cero si no se entiende.</returns>
    /// <remarks>
    /// Se prueban la cultura dominicana y la invariante. Alguien puede escribir 1500.50 o
    /// 1500,50 segun como tenga configurado el teclado del telefono, y rechazarlo por eso
    /// seria incomprensible para quien lo escribe.
    /// </remarks>
    public static decimal Leer(string? texto)
    {
        var limpio = texto?.Trim() ?? string.Empty;

        if (decimal.TryParse(limpio, NumberStyles.Number, Cultura, out var conCultura))
        {
            return conCultura;
        }

        return decimal.TryParse(
            limpio, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariante)
            ? invariante
            : 0m;
    }
}
