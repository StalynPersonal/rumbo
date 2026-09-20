using Rumbo.Contratos.Comun;
using Rumbo.Contratos.Movimientos;

namespace Rumbo.Aplicacion.Contratos;

/// <summary>
/// Registro y consulta del libro mayor.
/// </summary>
/// <remarks>
/// Es el servicio central de Rumbo. Todo lo demas (presupuestos, metas, viajes, informes) se
/// calcula sobre lo que se registra aqui.
/// </remarks>
public interface IServicioMovimientos
{
    /// <summary>Consulta el libro mayor con filtros y paginacion.</summary>
    /// <param name="filtro">Criterios de busqueda.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Una pagina de movimientos.</returns>
    Task<ResultadoPaginado<MovimientoResumen>> ListarAsync(
        FiltroMovimientos filtro,
        CancellationToken cancelacion = default);

    /// <summary>Devuelve un movimiento concreto.</summary>
    /// <param name="movimientoId">Movimiento buscado.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El movimiento.</returns>
    Task<MovimientoResumen> ObtenerAsync(
        Guid movimientoId,
        CancellationToken cancelacion = default);

    /// <summary>Registra un ingreso, un gasto o un ajuste.</summary>
    /// <param name="solicitud">Datos del movimiento.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El movimiento registrado.</returns>
    /// <remarks>
    /// Las transferencias NO se registran aqui: tienen su propia operacion, porque son dos
    /// asientos y no uno.
    /// </remarks>
    Task<MovimientoResumen> RegistrarAsync(
        SolicitudRegistrarMovimiento solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Modifica un movimiento ya registrado.</summary>
    /// <param name="movimientoId">Movimiento que se modifica.</param>
    /// <param name="solicitud">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El movimiento actualizado.</returns>
    /// <remarks>
    /// Si cambia el importe, el saldo de la cuenta se ajusta por la diferencia dentro de la
    /// misma transaccion de base de datos.
    /// </remarks>
    Task<MovimientoResumen> ActualizarAsync(
        Guid movimientoId,
        SolicitudActualizarMovimiento solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Elimina un movimiento y revierte su efecto sobre el saldo.</summary>
    /// <param name="movimientoId">Movimiento que se elimina.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando queda eliminado.</returns>
    /// <remarks>
    /// El borrado es logico: la fila permanece para la auditoria, pero deja de contar en el
    /// saldo y en los informes.
    /// </remarks>
    Task EliminarAsync(Guid movimientoId, CancellationToken cancelacion = default);

    /// <summary>Traspasa dinero entre dos cuentas del espacio.</summary>
    /// <param name="solicitud">Datos del traspaso.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El traspaso registrado.</returns>
    /// <remarks>
    /// Crea DOS movimientos, la salida y la entrada, unidos por el mismo identificador de
    /// transferencia. Ninguno de los dos cuenta como ingreso ni como gasto.
    /// </remarks>
    Task<TransferenciaResumen> TransferirAsync(
        SolicitudTransferir solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Elimina un traspaso completo, sus dos asientos y su efecto en los saldos.</summary>
    /// <param name="transferenciaId">Traspaso que se elimina.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando queda eliminado.</returns>
    /// <remarks>
    /// Se eliminan SIEMPRE las dos patas a la vez. Borrar solo una dejaria dinero apareciendo
    /// o desapareciendo de la nada.
    /// </remarks>
    Task EliminarTransferenciaAsync(
        Guid transferenciaId,
        CancellationToken cancelacion = default);
}
