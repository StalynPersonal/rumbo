using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Contratos;
using Rumbo.Infraestructura.Persistencia;

namespace Rumbo.Infraestructura.Identidad;

/// <summary>
/// Traduce identificadores de usuario a nombres, leyendo las tablas de Identity.
/// </summary>
/// <remarks>
/// Solo devuelve el nombre para mostrar. No expone correo, ni roles, ni ningun otro dato del
/// usuario: un informe necesita escribir «María» junto a una cifra y nada mas.
/// </remarks>
/// <param name="contexto">Contexto de datos.</param>
public class DirectorioUsuarios(ContextoRumbo contexto) : IDirectorioUsuarios
{
    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, string>> ObtenerNombresAsync(
        IEnumerable<Guid> usuarioIds,
        CancellationToken cancelacion = default)
    {
        var ids = usuarioIds.Distinct().ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var encontrados = await contexto.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.NombreCompleto })
            .ToListAsync(cancelacion);

        return encontrados.ToDictionary(u => u.Id, u => u.NombreCompleto);
    }
}
