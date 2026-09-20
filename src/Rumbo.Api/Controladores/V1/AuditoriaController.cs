using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Api.Autorizacion;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Auditoria;
using Rumbo.Contratos.Comun;
using Rumbo.Dominio.Autorizacion;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// Historial de quién hizo qué y cuándo dentro del espacio.
/// </summary>
/// <remarks>
/// <para>
/// En un hogar compartido es una función de convivencia tanto como de seguridad: permite
/// responder «¿quién cambió este gasto?» sin discutirlo de memoria.
/// </para>
/// <para>
/// <b>No devuelve el detalle de los cambios.</b> Ese campo guarda los valores anteriores y
/// nuevos, que en un movimiento son importes: exponerlo convertiría el historial en una
/// segunda vía para leer las finanzas, saltándose los permisos del módulo correspondiente.
/// </para>
/// </remarks>
/// <param name="auditoria">Servicio de auditoría.</param>
[ApiController]
[Route("api/v1/auditoria")]
[Produces("application/json")]
[Authorize]
public class AuditoriaController(IServicioAuditoria auditoria) : ControllerBase
{
    /// <summary>Consulta el historial del espacio activo.</summary>
    /// <param name="filtro">Criterios de búsqueda.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Una página del historial, de lo más reciente a lo más antiguo.</returns>
    /// <remarks>
    /// Reservado a quien administra el hogar: el historial revela los hábitos de cada
    /// persona, y no todos los miembros tienen por qué poder auditarse entre sí.
    /// </remarks>
    [HttpGet]
    [RequierePermiso(Permisos.Auditoria.Leer)]
    [ProducesResponseType<ResultadoPaginado<EntradaAuditoria>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResultadoPaginado<EntradaAuditoria>>> Consultar(
        [FromQuery] FiltroAuditoria filtro,
        CancellationToken cancelacion) =>
        Ok(await auditoria.ConsultarAsync(filtro, cancelacion));
}
