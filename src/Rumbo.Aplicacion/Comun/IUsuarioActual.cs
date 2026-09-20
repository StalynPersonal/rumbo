using Rumbo.Dominio.Enums;

namespace Rumbo.Aplicacion.Comun;

/// <summary>
/// Da acceso a la identidad de quien realiza la peticion actual.
/// </summary>
/// <remarks>
/// Aisla a la capa de aplicacion de ASP.NET Core: los servicios no necesitan conocer
/// <c>HttpContext</c> ni <c>ClaimsPrincipal</c>, lo que ademas los hace faciles de probar.
/// </remarks>
public interface IUsuarioActual
{
    /// <summary>Usuario autenticado, o <c>null</c> si la peticion es anonima.</summary>
    Guid? UsuarioId { get; }

    /// <summary>Correo del usuario autenticado.</summary>
    string? Correo { get; }

    /// <summary>Indica si la peticion viene de un usuario autenticado.</summary>
    bool EstaAutenticado { get; }

    /// <summary>Rol del usuario dentro del espacio activo.</summary>
    RolEspacio? RolEnEspacio { get; }

    /// <summary>
    /// Indica si es un administrador de plataforma.
    /// </summary>
    /// <remarks>
    /// Ese rol gestiona espacios, usuarios e invitaciones, pero NO puede leer datos
    /// financieros de ningun espacio.
    /// </remarks>
    bool EsAdministradorPlataforma { get; }

    /// <summary>
    /// Devuelve el usuario autenticado o lanza una excepcion si la peticion es anonima.
    /// </summary>
    /// <returns>Identificador del usuario.</returns>
    /// <exception cref="InvalidOperationException">Si no hay usuario autenticado.</exception>
    Guid ObtenerUsuarioObligatorio();
}
