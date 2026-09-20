namespace Rumbo.Infraestructura.Identidad;

/// <summary>
/// Nombres de las reclamaciones propias que Rumbo incluye en el JWT.
/// </summary>
/// <remarks>
/// Centralizarlos evita el error de escribir el nombre como texto suelto en un sitio y con
/// otra grafia en otro: la comprobacion no fallaria al compilar, simplemente nadie pasaria
/// nunca ese filtro y el sistema parecerria funcionar mal sin motivo.
/// </remarks>
public static class ClaimsRumbo
{
    /// <summary>
    /// Espacio activo de la sesion.
    /// </summary>
    /// <remarks>
    /// Va FIRMADO dentro del token. El cliente no puede cambiarlo sin invalidar la firma, y
    /// esa es la razon por la que el espacio nunca se acepta desde el cuerpo o una cabecera.
    /// </remarks>
    public const string Espacio = "esp";

    /// <summary>Rol del usuario dentro del espacio activo.</summary>
    public const string RolEspacio = "rol_esp";

    /// <summary>Permiso concreto concedido. Puede aparecer varias veces.</summary>
    public const string Permiso = "perm";
}
