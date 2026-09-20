namespace Rumbo.Contratos.Comun;

/// <summary>
/// Pagina de resultados de un listado.
/// </summary>
/// <typeparam name="T">Tipo de los elementos.</typeparam>
/// <param name="Elementos">Elementos de esta pagina.</param>
/// <param name="Pagina">Numero de pagina, empezando en 1.</param>
/// <param name="TamanoPagina">Cuantos elementos caben por pagina.</param>
/// <param name="TotalElementos">Total de elementos que cumplen el filtro.</param>
/// <remarks>
/// Todos los listados se paginan desde el principio. Un hogar con tres anos de movimientos
/// acumula decenas de miles de filas, y devolverlas de golpe agotaria la memoria del telefono.
/// </remarks>
public record ResultadoPaginado<T>(
    IReadOnlyList<T> Elementos,
    int Pagina,
    int TamanoPagina,
    int TotalElementos)
{
    /// <summary>Cantidad total de paginas disponibles.</summary>
    public int TotalPaginas => TamanoPagina <= 0
        ? 0
        : (int)Math.Ceiling(TotalElementos / (double)TamanoPagina);

    /// <summary>Indica si existe una pagina siguiente.</summary>
    public bool HayPaginaSiguiente => Pagina < TotalPaginas;
}
