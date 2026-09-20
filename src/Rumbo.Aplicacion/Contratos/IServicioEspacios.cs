using Rumbo.Contratos.Espacios;

namespace Rumbo.Aplicacion.Contratos;

/// <summary>
/// Gestion del espacio activo y de las personas que pertenecen a el.
/// </summary>
public interface IServicioEspacios
{
    /// <summary>Devuelve los datos del espacio.</summary>
    /// <param name="espacioId">Espacio activo.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Los datos del espacio.</returns>
    Task<EspacioDetalle> ObtenerAsync(Guid espacioId, CancellationToken cancelacion = default);

    /// <summary>Cambia el nombre o el tipo del espacio.</summary>
    /// <param name="espacioId">Espacio activo.</param>
    /// <param name="solicitud">Nombre y tipo nuevos.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Los datos actualizados.</returns>
    Task<EspacioDetalle> ActualizarAsync(
        Guid espacioId,
        SolicitudActualizarEspacio solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Lista las personas que pertenecen al espacio.</summary>
    /// <param name="espacioId">Espacio activo.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Los miembros con su rol y estado.</returns>
    /// <remarks>
    /// Se construye desde <c>MembresiasEspacio</c>. Por eso el administrador de plataforma
    /// nunca aparece: no tiene ninguna fila ahi.
    /// </remarks>
    Task<IReadOnlyList<MiembroEspacio>> ListarMiembrosAsync(
        Guid espacioId,
        CancellationToken cancelacion = default);

    /// <summary>Cambia el rol de un miembro.</summary>
    /// <param name="espacioId">Espacio activo.</param>
    /// <param name="usuarioObjetivoId">Persona cuyo rol cambia.</param>
    /// <param name="solicitud">Rol nuevo.</param>
    /// <param name="usuarioQueEjecutaId">Quien realiza el cambio.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El miembro con su rol actualizado.</returns>
    Task<MiembroEspacio> CambiarRolAsync(
        Guid espacioId,
        Guid usuarioObjetivoId,
        SolicitudCambiarRol solicitud,
        Guid usuarioQueEjecutaId,
        CancellationToken cancelacion = default);

    /// <summary>Suspende, reactiva o revoca el acceso de un miembro.</summary>
    /// <param name="espacioId">Espacio activo.</param>
    /// <param name="usuarioObjetivoId">Persona afectada.</param>
    /// <param name="solicitud">Estado nuevo.</param>
    /// <param name="usuarioQueEjecutaId">Quien realiza el cambio.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El miembro con su estado actualizado.</returns>
    /// <remarks>
    /// Nunca se borra la membresia: se cambia su estado. Los movimientos que esa persona
    /// registro siguen referenciandola, y borrarla dejaria el historial sin autor.
    /// </remarks>
    Task<MiembroEspacio> CambiarEstadoMiembroAsync(
        Guid espacioId,
        Guid usuarioObjetivoId,
        SolicitudCambiarEstadoMiembro solicitud,
        Guid usuarioQueEjecutaId,
        CancellationToken cancelacion = default);

    /// <summary>Devuelve las preferencias del espacio.</summary>
    /// <param name="espacioId">Espacio activo.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Las preferencias.</returns>
    Task<ConfiguracionEspacioDto> ObtenerConfiguracionAsync(
        Guid espacioId,
        CancellationToken cancelacion = default);

    /// <summary>Cambia las preferencias del espacio.</summary>
    /// <param name="espacioId">Espacio activo.</param>
    /// <param name="solicitud">Preferencias nuevas.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Las preferencias actualizadas.</returns>
    Task<ConfiguracionEspacioDto> ActualizarConfiguracionAsync(
        Guid espacioId,
        SolicitudActualizarConfiguracion solicitud,
        CancellationToken cancelacion = default);
}
