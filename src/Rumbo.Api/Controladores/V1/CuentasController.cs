using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Api.Autorizacion;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Cuentas;
using Rumbo.Dominio.Autorizacion;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// Cuentas del espacio: bancarias, tarjetas, efectivo, ahorro e inversión.
/// </summary>
/// <param name="cuentas">Servicio de cuentas.</param>
[ApiController]
[Route("api/v1/cuentas")]
[Produces("application/json")]
[Authorize]
public class CuentasController(IServicioCuentas cuentas) : ControllerBase
{
    /// <summary>Lista las cuentas del espacio activo.</summary>
    /// <param name="incluirInactivas">Si se incluyen las cuentas dadas de baja.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Las cuentas con su saldo.</returns>
    [HttpGet]
    [RequierePermiso(Permisos.Cuentas.Leer)]
    [ProducesResponseType<IReadOnlyList<CuentaResumen>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CuentaResumen>>> Listar(
        [FromQuery] bool incluirInactivas = false,
        CancellationToken cancelacion = default) =>
        Ok(await cuentas.ListarAsync(incluirInactivas, cancelacion));

    /// <summary>Devuelve una cuenta concreta.</summary>
    /// <param name="id">Cuenta buscada.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La cuenta con su saldo.</returns>
    [HttpGet("{id:guid}")]
    [RequierePermiso(Permisos.Cuentas.Leer)]
    [ProducesResponseType<CuentaResumen>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CuentaResumen>> Obtener(
        Guid id,
        CancellationToken cancelacion) =>
        Ok(await cuentas.ObtenerAsync(id, cancelacion));

    /// <summary>Crea una cuenta.</summary>
    /// <param name="solicitud">Datos de la cuenta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La cuenta creada.</returns>
    [HttpPost]
    [RequierePermiso(Permisos.Cuentas.Escribir)]
    [ProducesResponseType<CuentaResumen>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CuentaResumen>> Crear(
        [FromBody] SolicitudCrearCuenta solicitud,
        CancellationToken cancelacion)
    {
        var creada = await cuentas.CrearAsync(solicitud, cancelacion);

        return CreatedAtAction(nameof(Obtener), new { id = creada.Id }, creada);
    }

    /// <summary>Modifica una cuenta.</summary>
    /// <param name="id">Cuenta que se modifica.</param>
    /// <param name="solicitud">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La cuenta actualizada.</returns>
    /// <remarks>
    /// Ni el tipo, ni la moneda, ni el saldo inicial se pueden cambiar: los movimientos ya
    /// registrados dependen de ellos y cambiarlos descuadraría el historial.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [RequierePermiso(Permisos.Cuentas.Escribir)]
    [ProducesResponseType<CuentaResumen>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CuentaResumen>> Actualizar(
        Guid id,
        [FromBody] SolicitudActualizarCuenta solicitud,
        CancellationToken cancelacion) =>
        Ok(await cuentas.ActualizarAsync(id, solicitud, cancelacion));

    /// <summary>Elimina una cuenta sin movimientos.</summary>
    /// <param name="id">Cuenta que se elimina.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Sin contenido.</returns>
    /// <remarks>
    /// Una cuenta con movimientos no se puede eliminar: hay que desactivarla, para no dejar
    /// asientos apuntando a una cuenta invisible.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [RequierePermiso(Permisos.Cuentas.Eliminar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        await cuentas.EliminarAsync(id, cancelacion);

        return NoContent();
    }

    /// <summary>Recalcula el saldo desde el libro mayor y lo compara con el guardado.</summary>
    /// <param name="id">Cuenta que se reconcilia.</param>
    /// <param name="corregir">Si se aplica la corrección cuando hay desviación.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Los dos saldos y su diferencia.</returns>
    /// <remarks>
    /// Es la red de seguridad del saldo en instantánea: detecta cualquier desviación entre
    /// lo guardado y la suma real de los movimientos, y permite corregirla sin tocar la base
    /// de datos a mano.
    /// </remarks>
    [HttpPost("{id:guid}/reconciliar")]
    [RequierePermiso(Permisos.Cuentas.Escribir)]
    [ProducesResponseType<ResultadoReconciliacion>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultadoReconciliacion>> Reconciliar(
        Guid id,
        [FromQuery] bool corregir = false,
        CancellationToken cancelacion = default) =>
        Ok(await cuentas.ReconciliarAsync(id, corregir, cancelacion));
}
