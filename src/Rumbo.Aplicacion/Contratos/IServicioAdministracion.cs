using Rumbo.Contratos.Administracion;
using Rumbo.Contratos.Comun;

namespace Rumbo.Aplicacion.Contratos;

/// <summary>
/// Gestion de la plataforma: espacios, usuarios y metricas.
/// </summary>
/// <remarks>
/// <b>Ninguna operacion de este servicio devuelve datos financieros.</b> El administrador de
/// plataforma gestiona altas y estados; las finanzas de cada hogar son de ese hogar. Las
/// metricas son recuentos, nunca importes: saber cuantos espacios hay es gestion, saber
/// cuanto dinero mueven seria entrar en sus cuentas.
/// </remarks>
public interface IServicioAdministracion
{
    /// <summary>Lista los espacios de la plataforma.</summary>
    /// <param name="busqueda">Texto a buscar en el nombre.</param>
    /// <param name="pagina">Numero de pagina, empezando en 1.</param>
    /// <param name="tamanoPagina">Elementos por pagina.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Una pagina de espacios, sin datos financieros.</returns>
    Task<ResultadoPaginado<EspacioAdminResumen>> ListarEspaciosAsync(
        string? busqueda = null,
        int pagina = 1,
        int tamanoPagina = 50,
        CancellationToken cancelacion = default);

    /// <summary>Suspende o reactiva un espacio.</summary>
    /// <param name="espacioId">Espacio afectado.</param>
    /// <param name="solicitud">Estado nuevo y motivo.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El espacio actualizado.</returns>
    /// <remarks>
    /// Suspender corta el acceso de TODOS sus miembros de inmediato, sin borrar nada. Es
    /// reversible: los datos siguen intactos.
    /// </remarks>
    Task<EspacioAdminResumen> CambiarEstadoEspacioAsync(
        Guid espacioId,
        SolicitudCambiarEstadoEspacio solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Lista las cuentas de usuario de la plataforma.</summary>
    /// <param name="busqueda">Texto a buscar en el correo o el nombre.</param>
    /// <param name="pagina">Numero de pagina.</param>
    /// <param name="tamanoPagina">Elementos por pagina.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Una pagina de usuarios.</returns>
    Task<ResultadoPaginado<UsuarioAdminResumen>> ListarUsuariosAsync(
        string? busqueda = null,
        int pagina = 1,
        int tamanoPagina = 50,
        CancellationToken cancelacion = default);

    /// <summary>Habilita o deshabilita una cuenta de usuario.</summary>
    /// <param name="usuarioId">Usuario afectado.</param>
    /// <param name="activo">Si la cuenta queda habilitada.</param>
    /// <param name="ejecutadoPorUsuarioId">Administrador que realiza el cambio.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El usuario actualizado.</returns>
    /// <remarks>
    /// Un administrador no puede deshabilitarse a si mismo: si fuera el unico, la plataforma
    /// se quedaria sin nadie capaz de emitir invitaciones.
    /// </remarks>
    Task<UsuarioAdminResumen> CambiarEstadoUsuarioAsync(
        Guid usuarioId,
        bool activo,
        Guid ejecutadoPorUsuarioId,
        CancellationToken cancelacion = default);

    /// <summary>Devuelve los recuentos agregados de la plataforma.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Las metricas.</returns>
    Task<MetricasPlataforma> ObtenerMetricasAsync(CancellationToken cancelacion = default);
}
