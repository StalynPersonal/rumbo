namespace Rumbo.Infraestructura.Identidad;

/// <summary>
/// Nombres de los roles de plataforma, en un solo sitio.
/// </summary>
/// <remarks>
/// Centralizarlos evita el error clasico de escribir el nombre del rol como texto suelto en un
/// atributo de autorizacion y equivocarse en una letra: la comprobacion no fallaria al
/// compilar, simplemente nadie pasaria nunca ese filtro.
/// </remarks>
public static class RolesPlataforma
{
    /// <summary>
    /// Gestiona espacios, usuarios e invitaciones. No puede leer datos financieros.
    /// </summary>
    public const string AdministradorPlataforma = "AdministradorPlataforma";

    /// <summary>Todos los roles de plataforma definidos.</summary>
    public static readonly string[] Todos = [AdministradorPlataforma];
}
