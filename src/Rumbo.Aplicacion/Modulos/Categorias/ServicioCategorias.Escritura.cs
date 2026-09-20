using Microsoft.EntityFrameworkCore;

using Rumbo.Contratos.Categorias;
using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Categorias;

/// <summary>
/// Parte del servicio de categorias que crea, modifica y elimina.
/// </summary>
public partial class ServicioCategorias
{
    /// <inheritdoc />
    public async Task<CategoriaArbol> CrearAsync(
        SolicitudCrearCategoria solicitud,
        CancellationToken cancelacion = default)
    {
        var nombre = solicitud.Nombre?.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ExcepcionDominio("La categoría necesita un nombre.");
        }

        if (!Enum.TryParse<TipoCategoria>(solicitud.Tipo, ignoreCase: true, out var tipo))
        {
            throw new ExcepcionDominio("El tipo debe ser Ingreso, Gasto o Ambos.");
        }

        if (solicitud.CategoriaPadreId.HasValue)
        {
            var padre = await contexto.Categorias
                .FirstOrDefaultAsync(c => c.Id == solicitud.CategoriaPadreId.Value, cancelacion)
                ?? throw new ExcepcionNoEncontrado("la categoría padre");

            if (padre.CategoriaPadreId.HasValue)
            {
                throw new ExcepcionDominio(
                    $"Las categorías admiten {NivelesMaximos} niveles. "
                    + $"«{padre.Nombre}» ya es una subcategoría.");
            }

            if (padre.Tipo != tipo && padre.Tipo != TipoCategoria.Ambos)
            {
                throw new ExcepcionDominio(
                    $"«{padre.Nombre}» es de tipo {padre.Tipo}, así que sus subcategorías no "
                    + $"pueden ser de tipo {tipo}.");
            }
        }

        await VerificarNombreLibreAsync(nombre, solicitud.CategoriaPadreId, null, cancelacion);

        var categoria = new Categoria
        {
            Nombre = nombre,
            Tipo = tipo,
            CategoriaPadreId = solicitud.CategoriaPadreId,
            Icono = solicitud.Icono?.Trim(),
            Color = solicitud.Color?.Trim(),
            EsDelSistema = false,
            Activa = true,
            Orden = await SiguienteOrdenAsync(solicitud.CategoriaPadreId, cancelacion),
        };

        contexto.Categorias.Add(categoria);
        await contexto.SaveChangesAsync(cancelacion);

        return Proyectar(categoria, []);
    }

    /// <inheritdoc />
    public async Task<CategoriaArbol> ActualizarAsync(
        Guid categoriaId,
        SolicitudActualizarCategoria solicitud,
        CancellationToken cancelacion = default)
    {
        var categoria = await contexto.Categorias
            .FirstOrDefaultAsync(c => c.Id == categoriaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la categoría");

        var nombre = solicitud.Nombre?.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ExcepcionDominio("La categoría necesita un nombre.");
        }

        await VerificarNombreLibreAsync(
            nombre, categoria.CategoriaPadreId, categoriaId, cancelacion);

        // Ni el tipo ni el padre cambian: mover una categoria de sitio reclasificaria
        // movimientos ya registrados y alteraria informes de meses ya cerrados.
        categoria.Nombre = nombre;
        categoria.Icono = solicitud.Icono?.Trim();
        categoria.Color = solicitud.Color?.Trim();
        categoria.Activa = solicitud.Activa;
        categoria.Orden = solicitud.Orden;

        await contexto.SaveChangesAsync(cancelacion);

        return Proyectar(categoria, []);
    }

    /// <inheritdoc />
    public async Task EliminarAsync(Guid categoriaId, CancellationToken cancelacion = default)
    {
        var categoria = await contexto.Categorias
            .FirstOrDefaultAsync(c => c.Id == categoriaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la categoría");

        if (categoria.EsDelSistema)
        {
            // Las del sistema sostienen los informes predefinidos. Si se pudieran borrar,
            // esos informes se quedarian sin datos sin explicacion aparente.
            throw new ExcepcionDominio(
                $"«{categoria.Nombre}» es una categoría del sistema. Puedes renombrarla o "
                + "desactivarla, pero no eliminarla.");
        }

        var tieneMovimientos = await contexto.Movimientos
            .AnyAsync(m => m.CategoriaId == categoriaId, cancelacion);

        if (tieneMovimientos)
        {
            throw new ExcepcionDominio(
                $"«{categoria.Nombre}» tiene movimientos registrados. Desactívala si ya no "
                + "la usas: dejará de aparecer al registrar gastos y su historial se conserva.");
        }

        var tieneHijas = await contexto.Categorias
            .AnyAsync(c => c.CategoriaPadreId == categoriaId, cancelacion);

        if (tieneHijas)
        {
            throw new ExcepcionDominio(
                $"«{categoria.Nombre}» tiene subcategorías. Elimina primero las subcategorías.");
        }

        contexto.Categorias.Remove(categoria);
        await contexto.SaveChangesAsync(cancelacion);
    }

    /// <summary>Impide dos categorias con el mismo nombre bajo el mismo padre.</summary>
    /// <param name="nombre">Nombre propuesto.</param>
    /// <param name="padreId">Categoria padre.</param>
    /// <param name="excluirId">Categoria que se esta editando, para no compararla consigo misma.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando termina la comprobacion.</returns>
    /// <remarks>
    /// Si se permitiera, los informes mostrarian dos filas «Supermercado» y nadie sabria
    /// cual es cual.
    /// </remarks>
    private async Task VerificarNombreLibreAsync(
        string nombre,
        Guid? padreId,
        Guid? excluirId,
        CancellationToken cancelacion)
    {
        var repetida = await contexto.Categorias.AnyAsync(
            c => c.Nombre == nombre
                 && c.CategoriaPadreId == padreId
                 && (excluirId == null || c.Id != excluirId),
            cancelacion);

        if (repetida)
        {
            throw new ExcepcionDominio($"Ya existe una categoría llamada «{nombre}» en ese nivel.");
        }
    }

    /// <summary>Devuelve el orden siguiente dentro del nivel.</summary>
    /// <param name="padreId">Categoria padre.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El orden que corresponde a la categoria nueva.</returns>
    private async Task<int> SiguienteOrdenAsync(Guid? padreId, CancellationToken cancelacion)
    {
        var hermanas = contexto.Categorias.Where(c => c.CategoriaPadreId == padreId);

        return await hermanas.AnyAsync(cancelacion)
            ? await hermanas.MaxAsync(c => c.Orden, cancelacion) + 1
            : 0;
    }
}
