namespace Rumbo.Dominio.Enums;

/// <summary>
/// Procedencia de una tasa de cambio.
/// </summary>
/// <remarks>
/// Importa para la transparencia: al explicar un reporte hay que poder decir de donde salio
/// el tipo de cambio aplicado.
/// </remarks>
public enum OrigenTasaCambio
{
    /// <summary>La introdujo una persona.</summary>
    Manual = 1,

    /// <summary>La trajo un proveedor externo de tasas.</summary>
    Api = 2,

    /// <summary>Valor inicial cargado al crear la base de datos.</summary>
    Semilla = 3,
}
