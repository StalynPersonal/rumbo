using System.Globalization;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Usuarios;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;
using Rumbo.Infraestructura.Persistencia;

namespace Rumbo.Infraestructura.Identidad;

/// <summary>
/// Perfil de la persona autenticada.
/// </summary>
/// <remarks>
/// Vive en infraestructura porque trabaja con <see cref="UserManager{TUser}"/>.
/// </remarks>
/// <param name="usuarios">Gestor de usuarios de Identity.</param>
/// <param name="contexto">Contexto de base de datos.</param>
public class ServicioUsuarios(
    UserManager<Usuario> usuarios,
    ContextoRumbo contexto) : IServicioUsuarios
{
    /// <inheritdoc />
    public async Task<PerfilUsuario> ObtenerPerfilAsync(
        Guid usuarioId,
        CancellationToken cancelacion = default)
    {
        var usuario = await usuarios.FindByIdAsync(usuarioId.ToString())
            ?? throw new ExcepcionNoEncontrado("el usuario");

        return await ProyectarAsync(usuario, cancelacion);
    }

    /// <inheritdoc />
    public async Task<PerfilUsuario> ActualizarPerfilAsync(
        Guid usuarioId,
        SolicitudActualizarPerfil solicitud,
        CancellationToken cancelacion = default)
    {
        var usuario = await usuarios.FindByIdAsync(usuarioId.ToString())
            ?? throw new ExcepcionNoEncontrado("el usuario");

        var nombre = solicitud.NombreCompleto?.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ExcepcionDominio("El nombre no puede estar vacío.");
        }

        var cultura = (solicitud.CulturaPreferida ?? string.Empty).Trim();

        if (!string.IsNullOrWhiteSpace(cultura) && !EsCulturaValida(cultura))
        {
            throw new ExcepcionDominio(
                $"«{cultura}» no es una cultura válida. Usa un código como es-DO o en-US.");
        }

        usuario.NombreCompleto = nombre;

        if (!string.IsNullOrWhiteSpace(cultura))
        {
            usuario.CulturaPreferida = cultura;
        }

        var resultado = await usuarios.UpdateAsync(usuario);

        if (!resultado.Succeeded)
        {
            throw new ExcepcionDominio(
                string.Join(" ", resultado.Errors.Select(e => e.Description)));
        }

        return await ProyectarAsync(usuario, cancelacion);
    }

    /// <summary>Comprueba que la cultura existe en el sistema.</summary>
    /// <param name="cultura">Codigo de cultura.</param>
    /// <returns><c>true</c> si es valida.</returns>
    /// <remarks>
    /// Se valida porque una cultura inventada haria fallar el formateo de importes y fechas
    /// en la aplicacion movil, y el error aparecería lejos de su causa.
    /// </remarks>
    private static bool EsCulturaValida(string cultura)
    {
        try
        {
            _ = CultureInfo.GetCultureInfo(cultura);

            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    /// <summary>Convierte el usuario en su DTO de perfil.</summary>
    /// <param name="usuario">Usuario de Identity.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El perfil.</returns>
    private async Task<PerfilUsuario> ProyectarAsync(Usuario usuario, CancellationToken cancelacion)
    {
        var espacios = await contexto.MembresiasEspacio
            .AsNoTracking()
            .CountAsync(
                m => m.UsuarioId == usuario.Id && m.Estado == EstadoMembresia.Activa,
                cancelacion);

        var esAdministrador = await usuarios.IsInRoleAsync(
            usuario, RolesPlataforma.AdministradorPlataforma);

        return new PerfilUsuario(
            usuario.Id,
            usuario.Email ?? string.Empty,
            usuario.NombreCompleto,
            usuario.CulturaPreferida,
            usuario.FechaCreacion,
            usuario.UltimoAcceso,
            esAdministrador,
            espacios);
    }
}
