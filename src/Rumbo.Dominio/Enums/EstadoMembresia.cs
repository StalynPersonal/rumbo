namespace Rumbo.Dominio.Enums;

/// <summary>
/// Situacion de la pertenencia de un usuario a un espacio.
/// </summary>
/// <remarks>
/// El middleware de resolucion de espacio solo admite membresias Activas. Suspender es
/// preferible a borrar: los movimientos que esa persona registro siguen referenciandola.
/// </remarks>
public enum EstadoMembresia
{
    /// <summary>El usuario puede acceder al espacio.</summary>
    Activa = 1,

    /// <summary>Se le retiro el acceso temporalmente, sin borrar su historial.</summary>
    Suspendida = 2,

    /// <summary>Se le retiro el acceso de forma definitiva.</summary>
    Revocada = 3,
}
