using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Categorias;
using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Categorias;

/// <summary>
/// Gestion del arbol de categorias del espacio activo.
/// </summary>
/// <param name="contexto">Acceso a los datos.</param>
public partial class ServicioCategorias(IContextoRumbo contexto) : IServicioCategorias
{
    /// <summary>Profundidad maxima del arbol.</summary>
    /// <remarks>
    /// Dos niveles. Una jerarquia sin limite complica cada informe, porque hay que decidir
    /// hasta donde agregar, y en la practica nadie necesita un tercer nivel para llevar las
    /// cuentas de casa.
    /// </remarks>
    private const int NivelesMaximos = 2;

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoriaArbol>> ListarAsync(
        bool incluirInactivas = false,
        CancellationToken cancelacion = default)
    {
        var consulta = contexto.Categorias.AsNoTracking();

        if (!incluirInactivas)
        {
            consulta = consulta.Where(c => c.Activa);
        }

        // Se traen todas de una vez y el arbol se arma en memoria. Son unas ochenta filas
        // por espacio: una consulta por nivel seria mas codigo y mas lenta.
        var todas = await consulta
            .OrderBy(c => c.Orden)
            .ThenBy(c => c.Nombre)
            .ToListAsync(cancelacion);

        var porPadre = todas
            .Where(c => c.CategoriaPadreId.HasValue)
            .GroupBy(c => c.CategoriaPadreId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        return [.. todas
            .Where(c => c.CategoriaPadreId is null)
            .Select(c => Proyectar(c, porPadre))];
    }

    /// <summary>Arma el nodo del arbol con sus hijas.</summary>
    /// <param name="categoria">Categoria que se proyecta.</param>
    /// <param name="porPadre">Hijas agrupadas por identificador del padre.</param>
    /// <returns>El nodo del arbol.</returns>
    private static CategoriaArbol Proyectar(
        Categoria categoria,
        Dictionary<Guid, List<Categoria>> porPadre) =>
        new(categoria.Id,
            categoria.Nombre,
            categoria.Tipo.ToString(),
            categoria.Icono,
            categoria.Color,
            categoria.EsDelSistema,
            categoria.Activa,
            categoria.Orden,
            porPadre.TryGetValue(categoria.Id, out var hijas)
                ? [.. hijas.Select(h => Proyectar(h, porPadre))]
                : []);
}
