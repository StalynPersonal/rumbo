namespace Rumbo.Dominio.Enums;

/// <summary>
/// Situacion operativa de un espacio.
/// </summary>
public enum EstadoEspacio
{
    /// <summary>Funciona con normalidad.</summary>
    Activo = 1,

    /// <summary>Un administrador de plataforma le retiro el acceso, pero sus datos se conservan.</summary>
    Suspendido = 2,
}
