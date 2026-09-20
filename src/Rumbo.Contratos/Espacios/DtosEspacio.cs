namespace Rumbo.Contratos.Espacios;

/// <summary>Miembro de un espacio, tal como se muestra en el listado.</summary>
/// <param name="UsuarioId">Identificador de la persona.</param>
/// <param name="NombreCompleto">Nombre para mostrar.</param>
/// <param name="Correo">Correo de la persona.</param>
/// <param name="Rol">Propietario, Administrador o Miembro.</param>
/// <param name="Estado">Activa, Suspendida o Revocada.</param>
/// <param name="FechaIngreso">Cuando se unio al espacio.</param>
/// <remarks>
/// El listado se construye desde la tabla de membresias. Por eso el administrador de
/// plataforma nunca aparece aqui: no tiene ninguna fila en ella.
/// </remarks>
public record MiembroEspacio(
    Guid UsuarioId,
    string NombreCompleto,
    string Correo,
    string Rol,
    string Estado,
    DateTimeOffset FechaIngreso);

/// <summary>Datos para cambiar el nombre o el tipo de un espacio.</summary>
/// <param name="Nombre">Nombre nuevo.</param>
/// <param name="Tipo">Personal, Pareja, Familia o Negocio.</param>
public record SolicitudActualizarEspacio(string Nombre, string Tipo);

/// <summary>Datos para cambiar el rol de un miembro.</summary>
/// <param name="Rol">Rol nuevo: Administrador o Miembro.</param>
public record SolicitudCambiarRol(string Rol);
