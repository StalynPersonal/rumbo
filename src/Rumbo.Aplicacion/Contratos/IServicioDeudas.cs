using Rumbo.Contratos.Deudas;

namespace Rumbo.Aplicacion.Contratos;

/// <summary>Deudas del hogar y los pagos que las reducen.</summary>
public interface IServicioDeudas
{
    /// <summary>Lista las deudas del espacio.</summary>
    /// <param name="incluirSaldadas">Si se incluyen las saldadas y refinanciadas.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Las deudas, de mayor a menor saldo pendiente.</returns>
    Task<IReadOnlyList<DeudaDetalle>> ListarAsync(
        bool incluirSaldadas = false,
        CancellationToken cancelacion = default);

    /// <summary>Devuelve una deuda concreta.</summary>
    /// <param name="deudaId">Deuda buscada.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La deuda con su avance.</returns>
    Task<DeudaDetalle> ObtenerAsync(Guid deudaId, CancellationToken cancelacion = default);

    /// <summary>Crea una deuda.</summary>
    /// <param name="solicitud">Datos de la deuda.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La deuda creada.</returns>
    Task<DeudaDetalle> CrearAsync(
        SolicitudGuardarDeuda solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Modifica una deuda.</summary>
    /// <param name="deudaId">Deuda que se modifica.</param>
    /// <param name="solicitud">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La deuda actualizada.</returns>
    /// <remarks>
    /// El saldo pendiente no se puede editar: lo mueven los pagos. Si se pudiera cambiar a
    /// mano, el saldo y el historial de pagos dirian cosas distintas.
    /// </remarks>
    Task<DeudaDetalle> ActualizarAsync(
        Guid deudaId,
        SolicitudGuardarDeuda solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Cambia el estado de una deuda.</summary>
    /// <param name="deudaId">Deuda afectada.</param>
    /// <param name="solicitud">Estado nuevo.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La deuda actualizada.</returns>
    Task<DeudaDetalle> CambiarEstadoAsync(
        Guid deudaId,
        SolicitudCambiarEstadoDeuda solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Elimina una deuda sin pagos.</summary>
    /// <param name="deudaId">Deuda que se elimina.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando queda eliminada.</returns>
    Task EliminarAsync(Guid deudaId, CancellationToken cancelacion = default);

    /// <summary>Lista los pagos de una deuda.</summary>
    /// <param name="deudaId">Deuda consultada.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Los pagos, del mas reciente al mas antiguo.</returns>
    Task<IReadOnlyList<PagoDeudaDto>> ListarPagosAsync(
        Guid deudaId,
        CancellationToken cancelacion = default);

    /// <summary>Registra un pago contra una deuda.</summary>
    /// <param name="deudaId">Deuda que se paga.</param>
    /// <param name="solicitud">Cuenta, desglose del pago y fecha.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El pago registrado.</returns>
    /// <remarks>
    /// El pago <b>si</b> es un gasto: el dinero sale de la cuenta y no aparece en ningun
    /// otro sitio del hogar. El desglose entre capital, interes y cargos se guarda para
    /// poder analizarlo despues.
    /// </remarks>
    Task<PagoDeudaDto> PagarAsync(
        Guid deudaId,
        SolicitudPagarDeuda solicitud,
        CancellationToken cancelacion = default);
}
