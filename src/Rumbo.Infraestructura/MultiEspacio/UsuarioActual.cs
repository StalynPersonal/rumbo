using Rumbo.Aplicacion.Comun;
using Rumbo.Dominio.Enums;

namespace Rumbo.Infraestructura.MultiEspacio;

/// <summary>
/// Implementacion de la identidad del solicitante para una peticion.
/// </summary>
/// <remarks>
/// Igual que <see cref="ContextoEspacio"/>, es <i>scoped</i> y lo rellena el middleware a
/// partir del token ya validado. Mantenerlo aqui, y no leer <c>HttpContext</c> desde los
/// servicios, permite que la capa de aplicacion no dependa de ASP.NET Core y que las pruebas
/// puedan simular cualquier usuario sin montar una peticion HTTP.
/// </remarks>
public class UsuarioActual : IUsuarioActual
{
    /// <inheritdoc />
    public Guid? UsuarioId { get; private set; }

    /// <inheritdoc />
    public string? Correo { get; private set; }

    /// <inheritdoc />
    public bool EstaAutenticado => UsuarioId.HasValue;

    /// <inheritdoc />
    public RolEspacio? RolEnEspacio { get; private set; }

    /// <inheritdoc />
    public bool EsAdministradorPlataforma { get; private set; }

    /// <inheritdoc />
    public Guid ObtenerUsuarioObligatorio() =>
        UsuarioId ?? throw new InvalidOperationException(
            "La peticion no tiene usuario autenticado, pero la operacion lo requiere.");

    /// <summary>Fija la identidad del solicitante.</summary>
    /// <param name="usuarioId">Usuario autenticado.</param>
    /// <param name="correo">Correo del usuario.</param>
    /// <param name="rolEnEspacio">Rol dentro del espacio activo, si lo hay.</param>
    /// <param name="esAdministradorPlataforma">Si tiene el rol de plataforma.</param>
    public void Establecer(
        Guid usuarioId,
        string? correo,
        RolEspacio? rolEnEspacio,
        bool esAdministradorPlataforma)
    {
        UsuarioId = usuarioId;
        Correo = correo;
        RolEnEspacio = rolEnEspacio;
        EsAdministradorPlataforma = esAdministradorPlataforma;
    }
}
