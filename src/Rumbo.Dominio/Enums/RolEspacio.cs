namespace Rumbo.Dominio.Enums;

/// <summary>
/// Rol de un usuario DENTRO de un espacio concreto.
/// </summary>
/// <remarks>
/// No confundir con los roles de plataforma de ASP.NET Core Identity, como
/// <c>AdministradorPlataforma</c>. Una misma persona puede ser Propietario en un espacio y
/// Miembro en otro: el rol vive en la membresia, no en el usuario.
/// </remarks>
public enum RolEspacio
{
    /// <summary>Creo el espacio. Puede invitar, expulsar y borrar el espacio.</summary>
    Propietario = 1,

    /// <summary>Gestiona cuentas, presupuestos y miembros, pero no puede borrar el espacio.</summary>
    Administrador = 2,

    /// <summary>Registra y consulta movimientos. No administra miembros.</summary>
    Miembro = 3,
}
