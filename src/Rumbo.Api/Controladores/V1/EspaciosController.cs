using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Api.Autorizacion;
using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Espacios;
using Rumbo.Dominio.Autorizacion;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// El espacio activo: sus datos, sus miembros y sus preferencias.
/// </summary>
/// <remarks>
/// <para>
/// Todas las rutas operan sobre <c>actual</c>, es decir, sobre el espacio que indica el token
/// de quien llama. <b>No existe ninguna ruta que reciba un identificador de espacio.</b> Si
/// existiera, seria el punto exacto por donde alguien intentaria entrar en otro hogar
/// cambiando un valor en la URL.
/// </para>
/// <para>
/// Para trabajar sobre otro espacio hay que cambiar de espacio activo con
/// <c>POST /api/v1/autenticacion/cambiar-espacio</c>, que verifica la membresia y emite un
/// token nuevo.
/// </para>
/// </remarks>
/// <param name="espacios">Servicio de espacios.</param>
/// <param name="contextoEspacio">Espacio activo de la peticion.</param>
/// <param name="usuarioActual">Identidad de quien llama.</param>
[ApiController]
[Route("api/v1/espacios")]
[Produces("application/json")]
[Authorize]
public class EspaciosController(
    IServicioEspacios espacios,
    IContextoEspacio contextoEspacio,
    IUsuarioActual usuarioActual) : ControllerBase
{
    /// <summary>Devuelve los datos del espacio activo.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Nombre, tipo, moneda base y cantidad de miembros.</returns>
    [HttpGet("actual")]
    [RequierePermiso(Permisos.Espacio.Leer)]
    [ProducesResponseType<EspacioDetalle>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EspacioDetalle>> Obtener(CancellationToken cancelacion) =>
        Ok(await espacios.ObtenerAsync(EspacioActivo(), cancelacion));

    /// <summary>Cambia el nombre o el tipo del espacio activo.</summary>
    /// <param name="solicitud">Nombre y tipo nuevos.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Los datos actualizados.</returns>
    [HttpPut("actual")]
    [RequierePermiso(Permisos.Espacio.Escribir)]
    [ProducesResponseType<EspacioDetalle>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EspacioDetalle>> Actualizar(
        [FromBody] SolicitudActualizarEspacio solicitud,
        CancellationToken cancelacion) =>
        Ok(await espacios.ActualizarAsync(EspacioActivo(), solicitud, cancelacion));

    /// <summary>Lista las personas que pertenecen al espacio activo.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Los miembros con su rol y estado.</returns>
    /// <remarks>
    /// El administrador de plataforma no aparece aquí, y no por un filtro: la lista se
    /// construye desde la tabla de membresías, donde él no tiene ninguna fila.
    /// </remarks>
    [HttpGet("actual/miembros")]
    [RequierePermiso(Permisos.Espacio.Leer)]
    [ProducesResponseType<IReadOnlyList<MiembroEspacio>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MiembroEspacio>>> ListarMiembros(
        CancellationToken cancelacion) =>
        Ok(await espacios.ListarMiembrosAsync(EspacioActivo(), cancelacion));

    /// <summary>Cambia el rol de un miembro.</summary>
    /// <param name="usuarioId">Persona cuyo rol cambia.</param>
    /// <param name="solicitud">Rol nuevo.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El miembro actualizado.</returns>
    [HttpPut("actual/miembros/{usuarioId:guid}/rol")]
    [RequierePermiso(Permisos.Espacio.GestionarMiembros)]
    [ProducesResponseType<MiembroEspacio>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MiembroEspacio>> CambiarRol(
        Guid usuarioId,
        [FromBody] SolicitudCambiarRol solicitud,
        CancellationToken cancelacion) =>
        Ok(await espacios.CambiarRolAsync(
            EspacioActivo(), usuarioId, solicitud, UsuarioActual(), cancelacion));

    /// <summary>Suspende, reactiva o expulsa a un miembro.</summary>
    /// <param name="usuarioId">Persona afectada.</param>
    /// <param name="solicitud">Estado nuevo.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El miembro actualizado.</returns>
    /// <remarks>
    /// La membresía nunca se borra: cambia de estado. Los movimientos que esa persona
    /// registró siguen apuntando a ella, y borrarla dejaría el historial sin autor.
    /// </remarks>
    [HttpPut("actual/miembros/{usuarioId:guid}/estado")]
    [RequierePermiso(Permisos.Espacio.GestionarMiembros)]
    [ProducesResponseType<MiembroEspacio>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MiembroEspacio>> CambiarEstadoMiembro(
        Guid usuarioId,
        [FromBody] SolicitudCambiarEstadoMiembro solicitud,
        CancellationToken cancelacion) =>
        Ok(await espacios.CambiarEstadoMiembroAsync(
            EspacioActivo(), usuarioId, solicitud, UsuarioActual(), cancelacion));

    /// <summary>Devuelve las preferencias del espacio activo.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Umbrales, día de inicio de mes y opciones del motor de recomendaciones.</returns>
    [HttpGet("actual/configuracion")]
    [RequierePermiso(Permisos.Espacio.Leer)]
    [ProducesResponseType<ConfiguracionEspacioDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ConfiguracionEspacioDto>> ObtenerConfiguracion(
        CancellationToken cancelacion) =>
        Ok(await espacios.ObtenerConfiguracionAsync(EspacioActivo(), cancelacion));

    /// <summary>Cambia las preferencias del espacio activo.</summary>
    /// <param name="solicitud">Preferencias nuevas.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Las preferencias actualizadas.</returns>
    [HttpPut("actual/configuracion")]
    [RequierePermiso(Permisos.Espacio.Escribir)]
    [ProducesResponseType<ConfiguracionEspacioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConfiguracionEspacioDto>> ActualizarConfiguracion(
        [FromBody] SolicitudActualizarConfiguracion solicitud,
        CancellationToken cancelacion) =>
        Ok(await espacios.ActualizarConfiguracionAsync(EspacioActivo(), solicitud, cancelacion));

    /// <summary>Espacio activo de la petición, tomado del token verificado.</summary>
    /// <returns>Identificador del espacio.</returns>
    private Guid EspacioActivo() => contextoEspacio.ObtenerEspacioObligatorio();

    /// <summary>Usuario que realiza la petición.</summary>
    /// <returns>Identificador del usuario.</returns>
    private Guid UsuarioActual() => usuarioActual.ObtenerUsuarioObligatorio();
}
