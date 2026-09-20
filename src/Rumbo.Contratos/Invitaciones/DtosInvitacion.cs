namespace Rumbo.Contratos.Invitaciones;

/// <summary>
/// Datos para que un administrador de plataforma invite a alguien a crear su propio espacio.
/// </summary>
/// <param name="Correo">Correo de la persona invitada.</param>
/// <param name="NombreEspacioPropuesto">
/// Nombre sugerido para el espacio que se creara. La persona puede cambiarlo despues.
/// </param>
/// <param name="TipoEspacio">Personal, Pareja, Familia o Negocio.</param>
public record SolicitudInvitarPropietario(
    string Correo,
    string NombreEspacioPropuesto,
    string TipoEspacio);

/// <summary>
/// Datos para que el propietario de un espacio invite a alguien a unirse a EL.
/// </summary>
/// <param name="Correo">Correo de la persona invitada.</param>
/// <param name="Rol">Rol que tendra: Administrador o Miembro.</param>
public record SolicitudInvitarMiembro(string Correo, string Rol);

/// <summary>
/// Invitacion tal como se muestra en los listados.
/// </summary>
/// <param name="Id">Identificador de la invitacion.</param>
/// <param name="Correo">Correo al que se envio.</param>
/// <param name="Tipo">Propietario o Miembro.</param>
/// <param name="Estado">Pendiente, Aceptada, Expirada o Revocada.</param>
/// <param name="FechaExpiracion">Cuando deja de ser valida.</param>
/// <param name="FechaCreacion">Cuando se emitio.</param>
/// <remarks>
/// El codigo NO aparece aqui: se muestra una sola vez al crearla y despues solo existe su
/// hash en la base de datos. Un listado que devolviera codigos vigentes seria un regalo para
/// cualquiera con acceso de lectura.
/// </remarks>
public record InvitacionResumen(
    Guid Id,
    string Correo,
    string Tipo,
    string Estado,
    DateTimeOffset FechaExpiracion,
    DateTimeOffset FechaCreacion);

/// <summary>
/// Respuesta al crear una invitacion. Es la UNICA vez que se devuelve el codigo en claro.
/// </summary>
/// <param name="Id">Identificador de la invitacion.</param>
/// <param name="Correo">Correo al que se envio.</param>
/// <param name="Codigo">
/// Codigo de un solo uso. Se envia por correo y se muestra aqui para poder compartirlo a mano
/// si el correo no llegara. No vuelve a estar disponible.
/// </param>
/// <param name="FechaExpiracion">Cuando deja de ser valido.</param>
/// <param name="CorreoEnviado">
/// Si el envio por correo funciono. Cuando es <c>false</c>, hay que pasar el codigo por otro
/// medio.
/// </param>
public record InvitacionCreada(
    Guid Id,
    string Correo,
    string Codigo,
    DateTimeOffset FechaExpiracion,
    bool CorreoEnviado);
