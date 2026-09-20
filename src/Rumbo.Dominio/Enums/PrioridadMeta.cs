namespace Rumbo.Dominio.Enums;

/// <summary>
/// Importancia relativa de una meta cuando el dinero disponible no alcanza para todas.
/// </summary>
/// <remarks>
/// El motor de recomendaciones reparte el excedente disponible respetando este orden.
/// </remarks>
public enum PrioridadMeta
{
    /// <summary>Puede esperar.</summary>
    Baja = 1,

    /// <summary>Importante, sin urgencia.</summary>
    Media = 2,

    /// <summary>Prioritaria.</summary>
    Alta = 3,

    /// <summary>Imprescindible, por ejemplo el fondo de emergencia.</summary>
    Critica = 4,
}
