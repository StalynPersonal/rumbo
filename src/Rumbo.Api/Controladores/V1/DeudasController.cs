using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Api.Autorizacion;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Deudas;
using Rumbo.Dominio.Autorizacion;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// Deudas del hogar y los pagos que las reducen.
/// </summary>
/// <remarks>
/// Una deuda no es una cuenta con saldo negativo: es una obligación con su calendario, su
/// interés y su cuota. Cada pago sí genera un movimiento real que sale de una cuenta.
/// </remarks>
/// <param name="deudas">Servicio de deudas.</param>
[ApiController]
[Route("api/v1/deudas")]
[Produces("application/json")]
[Authorize]
public class DeudasController(IServicioDeudas deudas) : ControllerBase
{
    /// <summary>Lista las deudas del espacio.</summary>
    /// <param name="incluirSaldadas">Si se incluyen las saldadas y refinanciadas.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Las deudas, de mayor a menor saldo pendiente.</returns>
    [HttpGet]
    [RequierePermiso(Permisos.Deudas.Leer)]
    [ProducesResponseType<IReadOnlyList<DeudaDetalle>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DeudaDetalle>>> Listar(
        [FromQuery] bool incluirSaldadas = false,
        CancellationToken cancelacion = default) =>
        Ok(await deudas.ListarAsync(incluirSaldadas, cancelacion));

    /// <summary>Devuelve una deuda.</summary>
    /// <param name="id">Deuda buscada.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La deuda con su avance.</returns>
    [HttpGet("{id:guid}")]
    [RequierePermiso(Permisos.Deudas.Leer)]
    [ProducesResponseType<DeudaDetalle>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeudaDetalle>> Obtener(
        Guid id,
        CancellationToken cancelacion) =>
        Ok(await deudas.ObtenerAsync(id, cancelacion));

    /// <summary>Crea una deuda.</summary>
    /// <param name="solicitud">Datos de la deuda.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La deuda creada.</returns>
    [HttpPost]
    [RequierePermiso(Permisos.Deudas.Escribir)]
    [ProducesResponseType<DeudaDetalle>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeudaDetalle>> Crear(
        [FromBody] SolicitudGuardarDeuda solicitud,
        CancellationToken cancelacion)
    {
        var creada = await deudas.CrearAsync(solicitud, cancelacion);

        return CreatedAtAction(nameof(Obtener), new { id = creada.Id }, creada);
    }

    /// <summary>Modifica una deuda.</summary>
    /// <param name="id">Deuda que se modifica.</param>
    /// <param name="solicitud">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La deuda actualizada.</returns>
    /// <remarks>
    /// El saldo pendiente no se puede editar: lo mueven los pagos. Si se pudiera cambiar a
    /// mano, el saldo y el historial de pagos dirían cosas distintas y no habría forma de
    /// saber cuál es la buena.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [RequierePermiso(Permisos.Deudas.Escribir)]
    [ProducesResponseType<DeudaDetalle>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeudaDetalle>> Actualizar(
        Guid id,
        [FromBody] SolicitudGuardarDeuda solicitud,
        CancellationToken cancelacion) =>
        Ok(await deudas.ActualizarAsync(id, solicitud, cancelacion));

    /// <summary>Cambia el estado de una deuda.</summary>
    /// <param name="id">Deuda afectada.</param>
    /// <param name="solicitud">Estado nuevo.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La deuda actualizada.</returns>
    [HttpPut("{id:guid}/estado")]
    [RequierePermiso(Permisos.Deudas.Escribir)]
    [ProducesResponseType<DeudaDetalle>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeudaDetalle>> CambiarEstado(
        Guid id,
        [FromBody] SolicitudCambiarEstadoDeuda solicitud,
        CancellationToken cancelacion) =>
        Ok(await deudas.CambiarEstadoAsync(id, solicitud, cancelacion));

    /// <summary>Elimina una deuda que aún no tiene pagos.</summary>
    /// <param name="id">Deuda que se elimina.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Sin contenido.</returns>
    [HttpDelete("{id:guid}")]
    [RequierePermiso(Permisos.Deudas.Escribir)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        await deudas.EliminarAsync(id, cancelacion);

        return NoContent();
    }

    /// <summary>Lista los pagos de una deuda.</summary>
    /// <param name="id">Deuda consultada.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Los pagos, del más reciente al más antiguo.</returns>
    [HttpGet("{id:guid}/pagos")]
    [RequierePermiso(Permisos.Deudas.Leer)]
    [ProducesResponseType<IReadOnlyList<PagoDeudaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PagoDeudaDto>>> ListarPagos(
        Guid id,
        CancellationToken cancelacion) =>
        Ok(await deudas.ListarPagosAsync(id, cancelacion));

    /// <summary>Registra un pago contra una deuda.</summary>
    /// <param name="id">Deuda que se paga.</param>
    /// <param name="solicitud">Cuenta, desglose del pago y fecha.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El pago registrado.</returns>
    /// <remarks>
    /// <para>
    /// El pago <b>sí</b> es un gasto, a diferencia de una transferencia: el dinero sale de la
    /// cuenta y no aparece en ningún otro sitio del hogar. Que la parte de capital reduzca
    /// además una deuda no cambia que ese dinero ya no está disponible este mes.
    /// </para>
    /// <para>
    /// El importe total no se envía: es capital + interés + cargos. El asiento, el saldo de
    /// la cuenta y el saldo de la deuda se mueven en la misma transacción.
    /// </para>
    /// </remarks>
    [HttpPost("{id:guid}/pagos")]
    [RequierePermiso(Permisos.Deudas.Escribir)]
    [ProducesResponseType<PagoDeudaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagoDeudaDto>> Pagar(
        Guid id,
        [FromBody] SolicitudPagarDeuda solicitud,
        CancellationToken cancelacion)
    {
        var pago = await deudas.PagarAsync(id, solicitud, cancelacion);

        return CreatedAtAction(nameof(ListarPagos), new { id }, pago);
    }
}
