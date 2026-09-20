using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Administracion;
using Rumbo.Contratos.Comun;
using Rumbo.Contratos.Correo;
using Rumbo.Contratos.Invitaciones;
using Rumbo.Infraestructura.Identidad;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// Gestion de la plataforma: dar de alta propietarios de espacios nuevos.
/// </summary>
/// <remarks>
/// <para>
/// Reservado al rol <c>AdministradorPlataforma</c>, que NO pertenece a ningun espacio y por
/// tanto no aparece en el listado de miembros de ninguno: no tiene fila en
/// <c>MembresiasEspacio</c>, que es de donde se construye ese listado.
/// </para>
/// <para>
/// <b>Este rol no puede leer datos financieros.</b> Aqui solo hay gestion de altas. No existe
/// ningun endpoint que devuelva movimientos, saldos, metas ni viajes de un hogar, y el
/// administrador tampoco los obtendria por los controladores normales, porque la
/// autorizacion de esos exige un rol dentro del espacio, que el no tiene.
/// </para>
/// </remarks>
/// <param name="invitaciones">Servicio de invitaciones.</param>
/// <param name="correo">Servicio de configuracion de correo.</param>
/// <param name="administracion">Servicio de gestion de la plataforma.</param>
/// <param name="usuarioActual">Identidad de quien llama.</param>
[ApiController]
[Route("api/v1/administracion")]
[Produces("application/json")]
[Authorize(Roles = RolesPlataforma.AdministradorPlataforma)]
public class AdministracionController(
    IServicioInvitaciones invitaciones,
    IServicioConfiguracionCorreo correo,
    IServicioAdministracion administracion,
    IUsuarioActual usuarioActual) : ControllerBase
{
    /// <summary>Invita a alguien a crear su propio espacio.</summary>
    /// <param name="solicitud">Correo, nombre del espacio y tipo.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La invitacion creada, con su codigo.</returns>
    /// <remarks>
    /// Es la unica forma de que entre alguien nuevo en Rumbo: no hay registro publico.
    /// </remarks>
    [HttpPost("invitaciones")]
    [ProducesResponseType<InvitacionCreada>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InvitacionCreada>> InvitarPropietario(
        [FromBody] SolicitudInvitarPropietario solicitud,
        CancellationToken cancelacion)
    {
        var creada = await invitaciones.InvitarPropietarioAsync(
            solicitud, usuarioActual.ObtenerUsuarioObligatorio(), cancelacion);

        return CreatedAtAction(nameof(ListarInvitaciones), new { }, creada);
    }

    /// <summary>Lista las invitaciones de propietario emitidas.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Las invitaciones, SIN sus codigos.</returns>
    [HttpGet("invitaciones")]
    [ProducesResponseType<IReadOnlyList<InvitacionResumen>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InvitacionResumen>>> ListarInvitaciones(
        CancellationToken cancelacion) =>
        Ok(await invitaciones.ListarDePlataformaAsync(cancelacion));

    /// <summary>Anula una invitacion de propietario pendiente.</summary>
    /// <param name="id">Invitacion que se quiere anular.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Sin contenido.</returns>
    [HttpDelete("invitaciones/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevocarInvitacion(Guid id, CancellationToken cancelacion)
    {
        await invitaciones.RevocarAsync(id, espacioId: null, cancelacion);

        return NoContent();
    }

    /// <summary>Devuelve el servidor de correo de la plataforma.</summary>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La configuración, sin la contraseña.</returns>
    /// <remarks>
    /// Este servidor envía lo que no pertenece a ningún hogar: la invitación a un futuro
    /// propietario, cuyo espacio todavía no existe, y el restablecimiento de contraseña, que
    /// pertenece a la persona y no a un espacio.
    /// </remarks>
    [HttpGet("correo")]
    [ProducesResponseType<ConfiguracionCorreoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ConfiguracionCorreoDto>> ObtenerCorreo(
        CancellationToken cancelacion) =>
        Ok(await correo.ObtenerPlataformaAsync(cancelacion));

    /// <summary>Guarda el servidor de correo de la plataforma.</summary>
    /// <param name="solicitud">Datos del servidor.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La configuración guardada, sin la contraseña.</returns>
    /// <remarks>
    /// Dejar la contraseña vacía conserva la que ya estuviera guardada. La contraseña se
    /// cifra antes de almacenarse y la API no la devuelve nunca.
    /// </remarks>
    [HttpPut("correo")]
    [ProducesResponseType<ConfiguracionCorreoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ConfiguracionCorreoDto>> GuardarCorreo(
        [FromBody] SolicitudGuardarCorreo solicitud,
        CancellationToken cancelacion) =>
        Ok(await correo.GuardarPlataformaAsync(solicitud, cancelacion));

    /// <summary>Comprueba que el servidor de la plataforma funciona.</summary>
    /// <param name="solicitud">Dirección de prueba, opcional.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El resultado de la prueba.</returns>
    [HttpPost("correo/probar")]
    [ProducesResponseType<ResultadoPruebaCorreo>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResultadoPruebaCorreo>> ProbarCorreo(
        [FromBody] SolicitudProbarCorreo solicitud,
        CancellationToken cancelacion) =>
        Ok(await correo.ProbarPlataformaAsync(solicitud, cancelacion));

    /// <summary>Lista los espacios de la plataforma.</summary>
    /// <param name="busqueda">Texto a buscar en el nombre.</param>
    /// <param name="pagina">Número de página.</param>
    /// <param name="tamanoPagina">Elementos por página.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Una página de espacios.</returns>
    /// <remarks>
    /// <b>Sin ningún dato financiero</b>: nombre, tipo, estado, número de miembros y correo
    /// del propietario, para poder contactarlo. Ni saldos, ni movimientos, ni metas.
    /// </remarks>
    [HttpGet("espacios")]
    [ProducesResponseType<ResultadoPaginado<EspacioAdminResumen>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResultadoPaginado<EspacioAdminResumen>>> ListarEspacios(
        [FromQuery] string? busqueda,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 50,
        CancellationToken cancelacion = default) =>
        Ok(await administracion.ListarEspaciosAsync(busqueda, pagina, tamanoPagina, cancelacion));

    /// <summary>Suspende o reactiva un espacio.</summary>
    /// <param name="id">Espacio afectado.</param>
    /// <param name="solicitud">Estado nuevo y motivo.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El espacio actualizado.</returns>
    /// <remarks>
    /// Suspender corta el acceso de <b>todos</b> sus miembros en su siguiente petición, sin
    /// borrar nada. Es reversible: los datos siguen intactos.
    /// </remarks>
    [HttpPut("espacios/{id:guid}/estado")]
    [ProducesResponseType<EspacioAdminResumen>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EspacioAdminResumen>> CambiarEstadoEspacio(
        Guid id,
        [FromBody] SolicitudCambiarEstadoEspacio solicitud,
        CancellationToken cancelacion) =>
        Ok(await administracion.CambiarEstadoEspacioAsync(id, solicitud, cancelacion));

    /// <summary>Lista las cuentas de usuario.</summary>
    /// <param name="busqueda">Texto a buscar en el correo o el nombre.</param>
    /// <param name="pagina">Número de página.</param>
    /// <param name="tamanoPagina">Elementos por página.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Una página de usuarios.</returns>
    [HttpGet("usuarios")]
    [ProducesResponseType<ResultadoPaginado<UsuarioAdminResumen>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResultadoPaginado<UsuarioAdminResumen>>> ListarUsuarios(
        [FromQuery] string? busqueda,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 50,
        CancellationToken cancelacion = default) =>
        Ok(await administracion.ListarUsuariosAsync(busqueda, pagina, tamanoPagina, cancelacion));

    /// <summary>Habilita o deshabilita una cuenta.</summary>
    /// <param name="id">Usuario afectado.</param>
    /// <param name="activo">Si la cuenta queda habilitada.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El usuario actualizado.</returns>
    /// <remarks>
    /// Deshabilitar revoca todas sus sesiones de inmediato. Un administrador no puede
    /// deshabilitarse a sí mismo: si fuera el único, la plataforma se quedaría sin nadie
    /// capaz de emitir invitaciones.
    /// </remarks>
    [HttpPut("usuarios/{id:guid}/estado")]
    [ProducesResponseType<UsuarioAdminResumen>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioAdminResumen>> CambiarEstadoUsuario(
        Guid id,
        [FromQuery] bool activo,
        CancellationToken cancelacion) =>
        Ok(await administracion.CambiarEstadoUsuarioAsync(
            id, activo, usuarioActual.ObtenerUsuarioObligatorio(), cancelacion));

    /// <summary>Devuelve los recuentos agregados de la plataforma.</summary>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Las métricas.</returns>
    /// <remarks>
    /// Son <b>recuentos, nunca importes</b>. Saber cuántos hogares hay es gestión; saber
    /// cuánto dinero mueven sería entrar en sus finanzas.
    /// </remarks>
    [HttpGet("metricas")]
    [ProducesResponseType<MetricasPlataforma>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MetricasPlataforma>> ObtenerMetricas(
        CancellationToken cancelacion) =>
        Ok(await administracion.ObtenerMetricasAsync(cancelacion));
}
