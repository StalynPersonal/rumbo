using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Api.Autorizacion;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Panel;
using Rumbo.Dominio.Autorizacion;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// Pantalla de inicio del hogar.
/// </summary>
/// <param name="panel">Servicio del panel.</param>
[ApiController]
[Route("api/v1/panel")]
[Produces("application/json")]
[Authorize]
public class PanelController(IServicioPanel panel) : ControllerBase
{
    /// <summary>Devuelve todo lo que la pantalla de inicio necesita.</summary>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Patrimonio, mes en curso, alertas, compromisos, metas y sugerencias.</returns>
    /// <remarks>
    /// <para>
    /// Una sola petición en lugar de ocho. En una conexión móvil lenta, ocho peticiones son
    /// ocho oportunidades de que la pantalla se quede a medias.
    /// </para>
    /// <para>
    /// El panel no recalcula nada por su cuenta: reutiliza los mismos servicios que las
    /// pantallas de detalle, para que jamás muestre una cifra distinta a la de ellas.
    /// </para>
    /// </remarks>
    [HttpGet]
    [RequierePermiso(Permisos.Reportes.Leer)]
    [ProducesResponseType<PanelInicio>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PanelInicio>> Obtener(CancellationToken cancelacion) =>
        Ok(await panel.ObtenerAsync(cancelacion));
}
