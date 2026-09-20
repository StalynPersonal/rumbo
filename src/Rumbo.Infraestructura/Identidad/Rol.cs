using Microsoft.AspNetCore.Identity;

namespace Rumbo.Infraestructura.Identidad;

/// <summary>
/// Rol a nivel de PLATAFORMA, no de espacio.
/// </summary>
/// <remarks>
/// <para>
/// De momento solo existe uno: <c>AdministradorPlataforma</c>, quien reparte las invitaciones
/// para crear espacios nuevos.
/// </para>
/// <para>
/// No confundir con <c>RolEspacio</c> (Propietario, Administrador, Miembro), que describe lo
/// que alguien puede hacer DENTRO de un hogar y vive en <c>MembresiaEspacio</c>. Son dos ejes
/// independientes: el administrador de plataforma no pertenece a ningun espacio, y un
/// propietario de espacio no administra la plataforma.
/// </para>
/// </remarks>
public class Rol : IdentityRole<Guid>
{
    /// <summary>Descripcion en espanol de lo que permite el rol.</summary>
    public string? Descripcion { get; set; }
}
