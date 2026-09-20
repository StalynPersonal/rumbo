using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Rumbo.Contratos.Administracion;
using Rumbo.Contratos.Comun;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Infraestructura.Identidad;

/// <summary>
/// Parte del servicio de administracion dedicada a las cuentas de usuario.
/// </summary>
public partial class ServicioAdministracion
{
    /// <inheritdoc />
    public async Task<ResultadoPaginado<UsuarioAdminResumen>> ListarUsuariosAsync(
        string? busqueda = null,
        int pagina = 1,
        int tamanoPagina = 50,
        CancellationToken cancelacion = default)
    {
        var numero = Math.Max(1, pagina);
        var tamano = Math.Clamp(tamanoPagina, 1, TamanoMaximoPagina);

        var consulta = contexto.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();

            consulta = consulta.Where(u =>
                EF.Functions.Like(u.Email!, $"%{texto}%")
                || EF.Functions.Like(u.NombreCompleto, $"%{texto}%"));
        }

        var total = await consulta.CountAsync(cancelacion);

        var pagados = await consulta
            .OrderByDescending(u => u.FechaCreacion)
            .Skip((numero - 1) * tamano)
            .Take(tamano)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.NombreCompleto,
                u.Activo,
                u.FechaCreacion,
                u.UltimoAcceso,
            })
            .ToListAsync(cancelacion);

        var identificadores = pagados.Select(u => u.Id).ToList();

        var espaciosPorUsuario = await contexto.MembresiasEspacio
            .AsNoTracking()
            .Where(m => identificadores.Contains(m.UsuarioId) && m.Estado == EstadoMembresia.Activa)
            .GroupBy(m => m.UsuarioId)
            .Select(g => new { UsuarioId = g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.UsuarioId, x => x.Cantidad, cancelacion);

        var administradores = await ObtenerAdministradoresAsync(identificadores, cancelacion);

        var elementos = pagados
            .Select(u => new UsuarioAdminResumen(
                u.Id,
                u.Email ?? string.Empty,
                u.NombreCompleto,
                u.Activo,
                administradores.Contains(u.Id),
                u.FechaCreacion,
                u.UltimoAcceso,
                espaciosPorUsuario.TryGetValue(u.Id, out var cantidad) ? cantidad : 0))
            .ToList();

        return new ResultadoPaginado<UsuarioAdminResumen>(elementos, numero, tamano, total);
    }

    /// <inheritdoc />
    public async Task<UsuarioAdminResumen> CambiarEstadoUsuarioAsync(
        Guid usuarioId,
        bool activo,
        Guid ejecutadoPorUsuarioId,
        CancellationToken cancelacion = default)
    {
        // Un administrador no puede deshabilitarse a si mismo: si fuera el unico, la
        // plataforma se quedaria sin nadie capaz de emitir invitaciones y no habria forma de
        // recuperarla sin tocar la base de datos a mano.
        if (usuarioId == ejecutadoPorUsuarioId && !activo)
        {
            throw new ExcepcionDominio(
                "No puedes deshabilitar tu propia cuenta. Pídeselo a otro administrador.");
        }

        var usuario = await usuarios.FindByIdAsync(usuarioId.ToString())
            ?? throw new ExcepcionNoEncontrado("el usuario");

        usuario.Activo = activo;

        var resultado = await usuarios.UpdateAsync(usuario);

        if (!resultado.Succeeded)
        {
            throw new ExcepcionDominio(
                string.Join(" ", resultado.Errors.Select(e => e.Description)));
        }

        if (!activo)
        {
            // Deshabilitar debe cortar el acceso de inmediato: se revocan sus sesiones y se
            // olvida lo cacheado sobre sus membresias.
            var ahora = fechaHora.AhoraUtc;

            await contexto.TokensRenovacion
                .Where(t => t.UsuarioId == usuarioId && t.FechaRevocacion == null)
                .ExecuteUpdateAsync(
                    a => a.SetProperty(t => t.FechaRevocacion, ahora), cancelacion);

            var espacios = await contexto.MembresiasEspacio
                .AsNoTracking()
                .Where(m => m.UsuarioId == usuarioId)
                .Select(m => m.EspacioId)
                .ToListAsync(cancelacion);

            foreach (var espacioId in espacios)
            {
                cacheMembresias.Invalidar(usuarioId, espacioId);
            }
        }

        registro.LogWarning(
            "El administrador {Ejecutor} dejó la cuenta {UsuarioId} como {Estado}.",
            ejecutadoPorUsuarioId, usuarioId, activo ? "activa" : "deshabilitada");

        var pagina = await ListarUsuariosAsync(usuario.Email, 1, 50, cancelacion);

        return pagina.Elementos.FirstOrDefault(u => u.Id == usuarioId)
               ?? throw new ExcepcionNoEncontrado("el usuario");
    }

    /// <summary>Devuelve cuales de esos usuarios tienen el rol de plataforma.</summary>
    /// <param name="identificadores">Usuarios a comprobar.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El conjunto de administradores.</returns>
    /// <remarks>
    /// Se resuelve con UNA consulta sobre la tabla de roles en lugar de preguntar a Identity
    /// uno por uno: con cincuenta usuarios por pagina serian cincuenta consultas.
    /// </remarks>
    private async Task<HashSet<Guid>> ObtenerAdministradoresAsync(
        List<Guid> identificadores,
        CancellationToken cancelacion)
    {
        var rol = await contexto.Roles
            .AsNoTracking()
            .Where(r => r.Name == RolesPlataforma.AdministradorPlataforma)
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancelacion);

        if (rol == Guid.Empty)
        {
            return [];
        }

        var administradores = await contexto.UserRoles
            .AsNoTracking()
            .Where(ur => ur.RoleId == rol && identificadores.Contains(ur.UserId))
            .Select(ur => ur.UserId)
            .ToListAsync(cancelacion);

        return [.. administradores];
    }
}
