using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Enums;

namespace Rumbo.Dominio.Entidades.Identidad;

/// <summary>
/// Codigo de un solo uso que permite a una persona registrarse en Rumbo o unirse a un espacio.
/// </summary>
/// <remarks>
/// <para>
/// En Rumbo NO existe el auto-registro. Toda alta nace de una invitacion: un administrador de
/// plataforma invita a un futuro propietario, y ese propietario invita despues a su pareja o
/// a su familia. Asi no hay una superficie de registro abierta al publico.
/// </para>
/// <para>
/// El codigo en claro se muestra una sola vez y se envia por correo; en la base de datos se
/// guarda unicamente su hash, igual que con una contrasena. Si alguien leyera la tabla, no
/// podria usar las invitaciones pendientes.
/// </para>
/// </remarks>
public class Invitacion : EntidadAuditable
{
    /// <summary>Si crea un espacio nuevo o suma a alguien a uno existente.</summary>
    public TipoInvitacion Tipo { get; set; }

    /// <summary>
    /// Correo de la persona invitada. Al aceptar, el correo de registro debe coincidir.
    /// </summary>
    /// <remarks>
    /// Ligar la invitacion a un correo concreto evita que un codigo reenviado a otra persona
    /// sirva para entrar.
    /// </remarks>
    public required string Correo { get; set; }

    /// <summary>Hash SHA-256 del codigo. El codigo en claro no se almacena nunca.</summary>
    public required string HashCodigo { get; set; }

    /// <summary>
    /// Espacio al que se invita. Es <c>null</c> en las invitaciones de tipo
    /// <see cref="TipoInvitacion.Propietario"/>, porque el espacio todavia no existe.
    /// </summary>
    public Guid? EspacioId { get; set; }

    /// <summary>Espacio al que se invita.</summary>
    public Espacio? Espacio { get; set; }

    /// <summary>
    /// Nombre propuesto para el espacio que se creara, en invitaciones de tipo Propietario.
    /// </summary>
    public string? NombreEspacioPropuesto { get; set; }

    /// <summary>Rol que tendra la persona al aceptar, en invitaciones de tipo Miembro.</summary>
    public RolEspacio? RolAsignado { get; set; }

    /// <summary>Usuario que emitio la invitacion.</summary>
    public Guid EmitidaPorUsuarioId { get; set; }

    /// <summary>Momento a partir del cual el codigo deja de ser valido.</summary>
    public DateTimeOffset FechaExpiracion { get; set; }

    /// <summary>Momento en que se uso, o <c>null</c> si sigue sin usarse.</summary>
    public DateTimeOffset? FechaUso { get; set; }

    /// <summary>Usuario que resulto de aceptar la invitacion.</summary>
    public Guid? AceptadaPorUsuarioId { get; set; }

    /// <summary>Situacion actual de la invitacion.</summary>
    public EstadoInvitacion Estado { get; set; } = EstadoInvitacion.Pendiente;
}
