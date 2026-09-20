using Rumbo.Contratos.Cuentas;

namespace Rumbo.Aplicacion.Contratos;

/// <summary>
/// Gestion de las cuentas donde se guarda o se mueve el dinero.
/// </summary>
public interface IServicioCuentas
{
    /// <summary>Lista las cuentas del espacio activo.</summary>
    /// <param name="incluirInactivas">Si se incluyen las cuentas dadas de baja.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Las cuentas con su saldo.</returns>
    Task<IReadOnlyList<CuentaResumen>> ListarAsync(
        bool incluirInactivas = false,
        CancellationToken cancelacion = default);

    /// <summary>Devuelve una cuenta concreta.</summary>
    /// <param name="cuentaId">Cuenta buscada.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La cuenta con su saldo.</returns>
    Task<CuentaResumen> ObtenerAsync(Guid cuentaId, CancellationToken cancelacion = default);

    /// <summary>Crea una cuenta.</summary>
    /// <param name="solicitud">Datos de la cuenta.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La cuenta creada.</returns>
    Task<CuentaResumen> CrearAsync(
        SolicitudCrearCuenta solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Modifica una cuenta.</summary>
    /// <param name="cuentaId">Cuenta que se modifica.</param>
    /// <param name="solicitud">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La cuenta actualizada.</returns>
    Task<CuentaResumen> ActualizarAsync(
        Guid cuentaId,
        SolicitudActualizarCuenta solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Da de baja una cuenta.</summary>
    /// <param name="cuentaId">Cuenta que se elimina.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando queda eliminada.</returns>
    /// <remarks>
    /// El borrado es logico. Una cuenta con movimientos no se puede eliminar: hacerlo dejaria
    /// esos asientos apuntando a una cuenta invisible y el libro mayor sin cuadrar. En ese
    /// caso hay que desactivarla.
    /// </remarks>
    Task EliminarAsync(Guid cuentaId, CancellationToken cancelacion = default);

    /// <summary>
    /// Recalcula el saldo desde el libro mayor y compara con el guardado.
    /// </summary>
    /// <param name="cuentaId">Cuenta que se reconcilia.</param>
    /// <param name="corregir">Si se aplica la correccion cuando hay desviacion.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Los dos saldos y su diferencia.</returns>
    /// <remarks>
    /// Es la red de seguridad del saldo en instantanea. Si alguna vez apareciera una
    /// desviacion, este endpoint la detecta y permite corregirla sin tocar la base de datos
    /// a mano.
    /// </remarks>
    Task<ResultadoReconciliacion> ReconciliarAsync(
        Guid cuentaId,
        bool corregir = false,
        CancellationToken cancelacion = default);
}
