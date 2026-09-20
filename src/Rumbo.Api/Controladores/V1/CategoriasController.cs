using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Api.Autorizacion;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Categorias;
using Rumbo.Dominio.Autorizacion;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// Categorías con las que se clasifican los movimientos.
/// </summary>
/// <remarks>
/// Cada espacio nace con un árbol de categorías predeterminadas, y puede añadir las suyas.
/// La jerarquía admite dos niveles.
/// </remarks>
/// <param name="categorias">Servicio de categorías.</param>
[ApiController]
[Route("api/v1/categorias")]
[Produces("application/json")]
[Authorize]
public class CategoriasController(IServicioCategorias categorias) : ControllerBase
{
    /// <summary>Devuelve el árbol de categorías.</summary>
    /// <param name="incluirInactivas">Si se incluyen las desactivadas.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Las categorías de primer nivel con sus hijas.</returns>
    [HttpGet]
    [RequierePermiso(Permisos.Categorias.Leer)]
    [ProducesResponseType<IReadOnlyList<CategoriaArbol>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoriaArbol>>> Listar(
        [FromQuery] bool incluirInactivas = false,
        CancellationToken cancelacion = default) =>
        Ok(await categorias.ListarAsync(incluirInactivas, cancelacion));

    /// <summary>Crea una categoría.</summary>
    /// <param name="solicitud">Datos de la categoría.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La categoría creada.</returns>
    [HttpPost]
    [RequierePermiso(Permisos.Categorias.Escribir)]
    [ProducesResponseType<CategoriaArbol>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CategoriaArbol>> Crear(
        [FromBody] SolicitudCrearCategoria solicitud,
        CancellationToken cancelacion)
    {
        var creada = await categorias.CrearAsync(solicitud, cancelacion);

        return CreatedAtAction(nameof(Listar), new { }, creada);
    }

    /// <summary>Modifica una categoría.</summary>
    /// <param name="id">Categoría que se modifica.</param>
    /// <param name="solicitud">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La categoría actualizada.</returns>
    /// <remarks>
    /// El tipo y el padre no se pueden cambiar: mover una categoría de sitio reclasificaría
    /// movimientos ya registrados y alteraría informes de meses cerrados.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [RequierePermiso(Permisos.Categorias.Escribir)]
    [ProducesResponseType<CategoriaArbol>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoriaArbol>> Actualizar(
        Guid id,
        [FromBody] SolicitudActualizarCategoria solicitud,
        CancellationToken cancelacion) =>
        Ok(await categorias.ActualizarAsync(id, solicitud, cancelacion));

    /// <summary>Elimina una categoría propia sin movimientos ni subcategorías.</summary>
    /// <param name="id">Categoría que se elimina.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Sin contenido.</returns>
    [HttpDelete("{id:guid}")]
    [RequierePermiso(Permisos.Categorias.Escribir)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        await categorias.EliminarAsync(id, cancelacion);

        return NoContent();
    }
}
