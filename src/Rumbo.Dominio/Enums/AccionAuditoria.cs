namespace Rumbo.Dominio.Enums;

/// <summary>
/// Operacion registrada en el historial de auditoria.
/// </summary>
public enum AccionAuditoria
{
    /// <summary>Se creo un registro.</summary>
    Creacion = 1,

    /// <summary>Se modifico un registro.</summary>
    Actualizacion = 2,

    /// <summary>Se marco un registro como eliminado.</summary>
    Eliminacion = 3,

    /// <summary>Un usuario inicio sesion.</summary>
    InicioSesion = 4,

    /// <summary>Un usuario cerro sesion.</summary>
    CierreSesion = 5,

    /// <summary>Un usuario cambio su contrasena.</summary>
    CambioClave = 6,

    /// <summary>Un usuario cambio de espacio activo.</summary>
    CambioEspacio = 7,

    /// <summary>Se rechazo un intento de acceso.</summary>
    AccesoDenegado = 8,

    /// <summary>Un administrador de plataforma realizo una gestion.</summary>
    AccionAdministrativa = 9,
}
