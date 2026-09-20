using Rumbo.Contratos.Categorias;

namespace Rumbo.Aplicacion.Contratos;

/// <summary>Gestion del arbol de categorias del espacio.</summary>
public interface IServicioCategorias
{
    /// <summary>Devuelve el arbol completo de categorias.</summary>
    /// <param name="incluirInactivas">Si se incluyen las desactivadas.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Las categorias de primer nivel con sus hijas.</returns>
    Task<IReadOnlyList<CategoriaArbol>> ListarAsync(
        bool incluirInactivas = false,
        CancellationToken cancelacion = default);

    /// <summary>Crea una categoria.</summary>
    /// <param name="solicitud">Datos de la categoria.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La categoria creada.</returns>
    Task<CategoriaArbol> CrearAsync(
        SolicitudCrearCategoria solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Modifica una categoria.</summary>
    /// <param name="categoriaId">Categoria que se modifica.</param>
    /// <param name="solicitud">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La categoria actualizada.</returns>
    Task<CategoriaArbol> ActualizarAsync(
        Guid categoriaId,
        SolicitudActualizarCategoria solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Elimina una categoria.</summary>
    /// <param name="categoriaId">Categoria que se elimina.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando queda eliminada.</returns>
    /// <remarks>
    /// No se puede eliminar una categoria del sistema, ni una con movimientos, ni una con
    /// subcategorias. En esos casos hay que desactivarla.
    /// </remarks>
    Task EliminarAsync(Guid categoriaId, CancellationToken cancelacion = default);
}
