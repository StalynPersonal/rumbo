using Rumbo.Dominio.Enums;

namespace Rumbo.Dominio.Entidades.Financiero;

/// <summary>
/// Divisa admitida por el sistema, identificada por su codigo ISO-4217.
/// </summary>
/// <remarks>
/// Es una tabla GLOBAL: no pertenece a ningun espacio y no lleva filtro de aislamiento. Todos
/// los espacios comparten el mismo catalogo de monedas.
/// </remarks>
public class Moneda
{
    /// <summary>Codigo ISO-4217 de tres letras, por ejemplo DOP, USD o EUR.</summary>
    public required string Codigo { get; set; }

    /// <summary>Nombre en espanol, por ejemplo "Peso dominicano".</summary>
    public required string Nombre { get; set; }

    /// <summary>Simbolo para mostrar importes, por ejemplo "RD$".</summary>
    public required string Simbolo { get; set; }

    /// <summary>Cantidad de decimales con que se expresa habitualmente.</summary>
    public int Decimales { get; set; } = 2;

    /// <summary>Indica si puede seleccionarse al crear cuentas o registrar movimientos.</summary>
    public bool Activa { get; set; } = true;
}
