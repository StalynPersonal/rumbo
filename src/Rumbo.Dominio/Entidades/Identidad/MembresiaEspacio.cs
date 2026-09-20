using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Enums;

namespace Rumbo.Dominio.Entidades.Identidad;

/// <summary>
/// Relacion entre un usuario y un espacio, con el rol que ejerce dentro de el.
/// </summary>
/// <remarks>
/// <para>
/// Esta tabla es la que responde a la pregunta "quien puede entrar aqui y con que permisos".
/// El middleware de resolucion de espacio la consulta en cada peticion: no basta con que el
/// token diga que el usuario pertenece al espacio, hay que comprobarlo contra la base de datos
/// para que revocar un acceso surta efecto de inmediato.
/// </para>
/// <para>
/// Una misma persona puede pertenecer a varios espacios con roles distintos: Propietaria de su
/// hogar y Miembro del espacio de un negocio, por ejemplo.
/// </para>
/// <para>
/// Es tambien la razon por la que el <c>AdministradorPlataforma</c> es invisible dentro de los
/// espacios: no tiene ninguna fila aqui, y el listado de miembros se construye desde esta tabla.
/// </para>
/// </remarks>
public class MembresiaEspacio : EntidadAuditable
{
    /// <summary>Usuario que pertenece al espacio.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Espacio al que pertenece.</summary>
    public Guid EspacioId { get; set; }

    /// <summary>Espacio al que pertenece.</summary>
    public Espacio? Espacio { get; set; }

    /// <summary>Rol que ejerce el usuario en ESTE espacio.</summary>
    public RolEspacio Rol { get; set; } = RolEspacio.Miembro;

    /// <summary>Situacion de la membresia. Solo <c>Activa</c> permite acceder.</summary>
    public EstadoMembresia Estado { get; set; } = EstadoMembresia.Activa;

    /// <summary>Fecha en que la persona se unio al espacio.</summary>
    public DateTimeOffset FechaIngreso { get; set; }
}
