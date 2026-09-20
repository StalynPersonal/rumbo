using Rumbo.Contratos.Usuarios;

namespace Rumbo.Aplicacion.Contratos;

/// <summary>Perfil de la persona autenticada.</summary>
public interface IServicioUsuarios
{
    /// <summary>Devuelve el perfil de una persona.</summary>
    /// <param name="usuarioId">Usuario consultado.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Su perfil.</returns>
    Task<PerfilUsuario> ObtenerPerfilAsync(
        Guid usuarioId,
        CancellationToken cancelacion = default);

    /// <summary>Cambia el nombre y la cultura de la persona.</summary>
    /// <param name="usuarioId">Usuario que se modifica.</param>
    /// <param name="solicitud">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El perfil actualizado.</returns>
    /// <remarks>
    /// El correo no se toca aqui: es la credencial de acceso y cambiarlo sin verificar la
    /// direccion nueva permitiria secuestrar una cuenta.
    /// </remarks>
    Task<PerfilUsuario> ActualizarPerfilAsync(
        Guid usuarioId,
        SolicitudActualizarPerfil solicitud,
        CancellationToken cancelacion = default);
}
