using Rumbo.Contratos.Invitaciones;

namespace Rumbo.Aplicacion.Contratos;

/// <summary>
/// Gestiona los codigos de invitacion, que son la unica via de alta en Rumbo.
/// </summary>
public interface IServicioInvitaciones
{
    /// <summary>
    /// Invita a alguien a crear su propio espacio. Solo para administradores de plataforma.
    /// </summary>
    /// <param name="solicitud">Correo, nombre del espacio y tipo.</param>
    /// <param name="emitidaPorUsuarioId">Administrador que la emite.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La invitacion creada, con el codigo en claro.</returns>
    Task<InvitacionCreada> InvitarPropietarioAsync(
        SolicitudInvitarPropietario solicitud,
        Guid emitidaPorUsuarioId,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Invita a alguien a unirse al espacio activo.
    /// </summary>
    /// <param name="solicitud">Correo y rol que tendra.</param>
    /// <param name="espacioId">Espacio al que se invita.</param>
    /// <param name="emitidaPorUsuarioId">Persona que invita.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La invitacion creada, con el codigo en claro.</returns>
    Task<InvitacionCreada> InvitarMiembroAsync(
        SolicitudInvitarMiembro solicitud,
        Guid espacioId,
        Guid emitidaPorUsuarioId,
        CancellationToken cancelacion = default);

    /// <summary>Lista las invitaciones de un espacio.</summary>
    /// <param name="espacioId">Espacio del que se listan.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Las invitaciones, sin sus codigos.</returns>
    Task<IReadOnlyList<InvitacionResumen>> ListarDelEspacioAsync(
        Guid espacioId,
        CancellationToken cancelacion = default);

    /// <summary>Lista todas las invitaciones de tipo Propietario de la plataforma.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Las invitaciones, sin sus codigos.</returns>
    Task<IReadOnlyList<InvitacionResumen>> ListarDePlataformaAsync(
        CancellationToken cancelacion = default);

    /// <summary>Anula una invitacion que todavia no se ha usado.</summary>
    /// <param name="invitacionId">Invitacion que se quiere anular.</param>
    /// <param name="espacioId">
    /// Espacio desde el que se anula, o <c>null</c> si la anula un administrador de
    /// plataforma. Sirve para impedir que alguien anule invitaciones de otro hogar.
    /// </param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando queda anulada.</returns>
    Task RevocarAsync(
        Guid invitacionId,
        Guid? espacioId,
        CancellationToken cancelacion = default);
}
