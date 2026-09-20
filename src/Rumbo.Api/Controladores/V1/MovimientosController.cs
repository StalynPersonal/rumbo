using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Api.Autorizacion;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Comun;
using Rumbo.Contratos.Movimientos;
using Rumbo.Dominio.Autorizacion;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// El libro mayor: ingresos, gastos, ajustes y transferencias.
/// </summary>
/// <remarks>
/// <para>
/// Es el núcleo de Rumbo. Presupuestos, metas, viajes e informes se calculan sobre lo que se
/// registra aquí.
/// </para>
/// <para>
/// <b>Las transferencias tienen su propia ruta</b> porque son dos asientos y no uno. Ninguno
/// cuenta como ingreso ni como gasto: mover dinero entre cuentas propias no es gastarlo.
/// </para>
/// </remarks>
/// <param name="movimientos">Servicio del libro mayor.</param>
[ApiController]
[Route("api/v1/movimientos")]
[Produces("application/json")]
[Authorize]
public class MovimientosController(IServicioMovimientos movimientos) : ControllerBase
{
    /// <summary>Consulta el libro mayor con filtros y paginación.</summary>
    /// <param name="filtro">Criterios de búsqueda.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Una página de movimientos.</returns>
    [HttpGet]
    [RequierePermiso(Permisos.Movimientos.Leer)]
    [ProducesResponseType<ResultadoPaginado<MovimientoResumen>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginado<MovimientoResumen>>> Listar(
        [FromQuery] FiltroMovimientos filtro,
        CancellationToken cancelacion) =>
        Ok(await movimientos.ListarAsync(filtro, cancelacion));

    /// <summary>Devuelve un movimiento concreto.</summary>
    /// <param name="id">Movimiento buscado.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El movimiento.</returns>
    [HttpGet("{id:guid}")]
    [RequierePermiso(Permisos.Movimientos.Leer)]
    [ProducesResponseType<MovimientoResumen>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovimientoResumen>> Obtener(
        Guid id,
        CancellationToken cancelacion) =>
        Ok(await movimientos.ObtenerAsync(id, cancelacion));

    /// <summary>Registra un ingreso, un gasto o un ajuste.</summary>
    /// <param name="solicitud">Datos del movimiento.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El movimiento registrado.</returns>
    /// <remarks>
    /// El importe siempre es positivo: la dirección la determina el tipo, no el signo. Las
    /// transferencias no se registran aquí.
    /// </remarks>
    [HttpPost]
    [RequierePermiso(Permisos.Movimientos.Escribir)]
    [ProducesResponseType<MovimientoResumen>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MovimientoResumen>> Registrar(
        [FromBody] SolicitudRegistrarMovimiento solicitud,
        CancellationToken cancelacion)
    {
        var creado = await movimientos.RegistrarAsync(solicitud, cancelacion);

        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    /// <summary>Modifica un movimiento.</summary>
    /// <param name="id">Movimiento que se modifica.</param>
    /// <param name="solicitud">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El movimiento actualizado.</returns>
    /// <remarks>
    /// El saldo se ajusta por la diferencia dentro de la misma transacción. No se puede
    /// cambiar el tipo ni la cuenta.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [RequierePermiso(Permisos.Movimientos.Escribir)]
    [ProducesResponseType<MovimientoResumen>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovimientoResumen>> Actualizar(
        Guid id,
        [FromBody] SolicitudActualizarMovimiento solicitud,
        CancellationToken cancelacion) =>
        Ok(await movimientos.ActualizarAsync(id, solicitud, cancelacion));

    /// <summary>Elimina un movimiento y revierte su efecto sobre el saldo.</summary>
    /// <param name="id">Movimiento que se elimina.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Sin contenido.</returns>
    /// <remarks>
    /// El borrado es lógico: la fila permanece para la auditoría, pero deja de contar en el
    /// saldo y en los informes.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [RequierePermiso(Permisos.Movimientos.Eliminar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        await movimientos.EliminarAsync(id, cancelacion);

        return NoContent();
    }

    /// <summary>Traspasa dinero entre dos cuentas del espacio.</summary>
    /// <param name="solicitud">Datos del traspaso.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El traspaso registrado.</returns>
    /// <remarks>
    /// Crea <b>dos</b> movimientos, la salida y la entrada, en una sola transacción. Un
    /// traspaso nunca aparece en los totales de ingresos ni de gastos. Si hubo comisión, esa
    /// sí se registra como gasto aparte, porque ese dinero sí sale del hogar.
    /// </remarks>
    [HttpPost("transferencias")]
    [RequierePermiso(Permisos.Movimientos.Escribir)]
    [ProducesResponseType<TransferenciaResumen>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransferenciaResumen>> Transferir(
        [FromBody] SolicitudTransferir solicitud,
        CancellationToken cancelacion)
    {
        var creada = await movimientos.TransferirAsync(solicitud, cancelacion);

        return CreatedAtAction(nameof(Listar), new { }, creada);
    }

    /// <summary>Elimina un traspaso completo y revierte los dos saldos.</summary>
    /// <param name="id">Traspaso que se elimina.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Sin contenido.</returns>
    /// <remarks>
    /// Se deshacen siempre las dos patas a la vez. Borrar solo una dejaría dinero
    /// apareciendo o desapareciendo de la nada.
    /// </remarks>
    [HttpDelete("transferencias/{id:guid}")]
    [RequierePermiso(Permisos.Movimientos.Eliminar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EliminarTransferencia(Guid id, CancellationToken cancelacion)
    {
        await movimientos.EliminarTransferenciaAsync(id, cancelacion);

        return NoContent();
    }
}
