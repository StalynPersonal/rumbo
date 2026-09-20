using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Api.Autorizacion;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Movimientos;
using Rumbo.Contratos.Recurrentes;
using Rumbo.Dominio.Autorizacion;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// Obligaciones e ingresos que se repiten: servicios, alquiler, salarios.
/// </summary>
/// <remarks>
/// <para>
/// No son movimientos, sino <b>plantillas</b> de movimientos futuros. Sirven para avisar de
/// lo que viene y para que el motor de recomendaciones sepa cuánto dinero está ya
/// comprometido antes de sugerir ahorrar.
/// </para>
/// <para>
/// <b>Rumbo nunca crea el movimiento por su cuenta.</b> Cuando llega la fecha, avisa; el
/// movimiento se registra al confirmar el pago con <c>POST .../pagar</c>. Generarlo
/// automáticamente haría que el saldo dejara de reflejar la realidad en cuanto un pago se
/// retrasara o cambiara de importe, que es lo habitual en un recibo de luz.
/// </para>
/// </remarks>
/// <param name="recurrentes">Servicio de recurrentes.</param>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
[Authorize]
public class RecurrentesController(IServicioRecurrentes recurrentes) : ControllerBase
{
    /// <summary>Lista los gastos recurrentes, por proximidad de vencimiento.</summary>
    /// <param name="incluirInactivos">Si se incluyen los pausados y finalizados.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Los gastos recurrentes, con los días que faltan para vencer.</returns>
    [HttpGet("gastos-recurrentes")]
    [RequierePermiso(Permisos.Movimientos.Leer)]
    [ProducesResponseType<IReadOnlyList<GastoRecurrenteDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GastoRecurrenteDto>>> ListarGastos(
        [FromQuery] bool incluirInactivos = false,
        CancellationToken cancelacion = default) =>
        Ok(await recurrentes.ListarGastosAsync(incluirInactivos, cancelacion));

    /// <summary>Crea un gasto recurrente.</summary>
    /// <param name="solicitud">Datos de la obligación.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El gasto recurrente creado.</returns>
    [HttpPost("gastos-recurrentes")]
    [RequierePermiso(Permisos.Movimientos.Escribir)]
    [ProducesResponseType<GastoRecurrenteDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GastoRecurrenteDto>> CrearGasto(
        [FromBody] SolicitudGuardarGastoRecurrente solicitud,
        CancellationToken cancelacion)
    {
        var creado = await recurrentes.CrearGastoAsync(solicitud, cancelacion);

        return CreatedAtAction(nameof(ListarGastos), new { }, creado);
    }

    /// <summary>Modifica un gasto recurrente.</summary>
    /// <param name="id">Gasto recurrente que se modifica.</param>
    /// <param name="solicitud">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El gasto recurrente actualizado.</returns>
    [HttpPut("gastos-recurrentes/{id:guid}")]
    [RequierePermiso(Permisos.Movimientos.Escribir)]
    [ProducesResponseType<GastoRecurrenteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GastoRecurrenteDto>> ActualizarGasto(
        Guid id,
        [FromBody] SolicitudGuardarGastoRecurrente solicitud,
        CancellationToken cancelacion) =>
        Ok(await recurrentes.ActualizarGastoAsync(id, solicitud, cancelacion));

    /// <summary>Pausa, reactiva o finaliza un gasto recurrente.</summary>
    /// <param name="id">Gasto recurrente afectado.</param>
    /// <param name="estado">Activa, Pausada o Finalizada.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El gasto recurrente actualizado.</returns>
    [HttpPut("gastos-recurrentes/{id:guid}/estado")]
    [RequierePermiso(Permisos.Movimientos.Escribir)]
    [ProducesResponseType<GastoRecurrenteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GastoRecurrenteDto>> CambiarEstadoGasto(
        Guid id,
        [FromQuery] string estado,
        CancellationToken cancelacion) =>
        Ok(await recurrentes.CambiarEstadoGastoAsync(id, estado, cancelacion));

    /// <summary>Elimina un gasto recurrente.</summary>
    /// <param name="id">Gasto recurrente que se elimina.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Sin contenido.</returns>
    [HttpDelete("gastos-recurrentes/{id:guid}")]
    [RequierePermiso(Permisos.Movimientos.Eliminar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EliminarGasto(Guid id, CancellationToken cancelacion)
    {
        await recurrentes.EliminarGastoAsync(id, cancelacion);

        return NoContent();
    }

    /// <summary>Confirma que la obligación se pagó.</summary>
    /// <param name="id">Gasto recurrente que se paga.</param>
    /// <param name="solicitud">Importe y fecha reales.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El movimiento creado.</returns>
    /// <remarks>
    /// Crea el movimiento real y adelanta la próxima fecha según la frecuencia. El importe
    /// real manda sobre el estimado: en un recibo de luz casi nunca coinciden.
    /// </remarks>
    [HttpPost("gastos-recurrentes/{id:guid}/pagar")]
    [RequierePermiso(Permisos.Movimientos.Escribir)]
    [ProducesResponseType<MovimientoResumen>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovimientoResumen>> PagarGasto(
        Guid id,
        [FromBody] SolicitudRegistrarPagoRecurrente solicitud,
        CancellationToken cancelacion) =>
        Ok(await recurrentes.RegistrarPagoAsync(id, solicitud, cancelacion));

    /// <summary>Lista los ingresos recurrentes.</summary>
    /// <param name="incluirInactivos">Si se incluyen los pausados y finalizados.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Los ingresos recurrentes, con los días que faltan para cobrar.</returns>
    [HttpGet("ingresos-recurrentes")]
    [RequierePermiso(Permisos.Movimientos.Leer)]
    [ProducesResponseType<IReadOnlyList<IngresoRecurrenteDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<IngresoRecurrenteDto>>> ListarIngresos(
        [FromQuery] bool incluirInactivos = false,
        CancellationToken cancelacion = default) =>
        Ok(await recurrentes.ListarIngresosAsync(incluirInactivos, cancelacion));

    /// <summary>Crea un ingreso recurrente.</summary>
    /// <param name="solicitud">Datos del ingreso.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El ingreso recurrente creado.</returns>
    /// <remarks>
    /// Sirve sobre todo para que el motor de recomendaciones pueda proyectar el ingreso
    /// esperado de los próximos meses aunque todavía haya poco historial registrado.
    /// </remarks>
    [HttpPost("ingresos-recurrentes")]
    [RequierePermiso(Permisos.Movimientos.Escribir)]
    [ProducesResponseType<IngresoRecurrenteDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IngresoRecurrenteDto>> CrearIngreso(
        [FromBody] SolicitudGuardarIngresoRecurrente solicitud,
        CancellationToken cancelacion)
    {
        var creado = await recurrentes.CrearIngresoAsync(solicitud, cancelacion);

        return CreatedAtAction(nameof(ListarIngresos), new { }, creado);
    }

    /// <summary>Modifica un ingreso recurrente.</summary>
    /// <param name="id">Ingreso recurrente que se modifica.</param>
    /// <param name="solicitud">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El ingreso recurrente actualizado.</returns>
    [HttpPut("ingresos-recurrentes/{id:guid}")]
    [RequierePermiso(Permisos.Movimientos.Escribir)]
    [ProducesResponseType<IngresoRecurrenteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IngresoRecurrenteDto>> ActualizarIngreso(
        Guid id,
        [FromBody] SolicitudGuardarIngresoRecurrente solicitud,
        CancellationToken cancelacion) =>
        Ok(await recurrentes.ActualizarIngresoAsync(id, solicitud, cancelacion));

    /// <summary>Elimina un ingreso recurrente.</summary>
    /// <param name="id">Ingreso recurrente que se elimina.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Sin contenido.</returns>
    [HttpDelete("ingresos-recurrentes/{id:guid}")]
    [RequierePermiso(Permisos.Movimientos.Eliminar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EliminarIngreso(Guid id, CancellationToken cancelacion)
    {
        await recurrentes.EliminarIngresoAsync(id, cancelacion);

        return NoContent();
    }

    /// <summary>Confirma que el ingreso se recibió.</summary>
    /// <param name="id">Ingreso recurrente que se cobra.</param>
    /// <param name="solicitud">Importe y fecha reales.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El movimiento creado.</returns>
    [HttpPost("ingresos-recurrentes/{id:guid}/cobrar")]
    [RequierePermiso(Permisos.Movimientos.Escribir)]
    [ProducesResponseType<MovimientoResumen>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovimientoResumen>> CobrarIngreso(
        Guid id,
        [FromBody] SolicitudRegistrarPagoRecurrente solicitud,
        CancellationToken cancelacion) =>
        Ok(await recurrentes.RegistrarCobroAsync(id, solicitud, cancelacion));
}
