using Rumbo.Contratos.Movimientos;
using Rumbo.Contratos.Recurrentes;

namespace Rumbo.Aplicacion.Contratos;

/// <summary>
/// Obligaciones e ingresos que se repiten: servicios, alquiler, salarios.
/// </summary>
/// <remarks>
/// <para>
/// No son movimientos, sino PLANTILLAS de movimientos futuros. Sirven para avisar de lo que
/// viene y para que el motor de recomendaciones sepa cuanto dinero esta ya comprometido
/// antes de sugerir ahorrar.
/// </para>
/// <para>
/// <b>Rumbo nunca genera el movimiento por su cuenta.</b> Cuando llega la fecha, avisa; el
/// movimiento se crea al confirmar el pago. Generarlo solo haria que el saldo dejara de
/// reflejar la realidad en cuanto un pago se retrasara o cambiara de importe.
/// </para>
/// </remarks>
public interface IServicioRecurrentes
{
    /// <summary>Lista los gastos recurrentes del espacio.</summary>
    /// <param name="incluirInactivos">Si se incluyen los pausados y finalizados.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Los gastos recurrentes, ordenados por proximidad de vencimiento.</returns>
    Task<IReadOnlyList<GastoRecurrenteDto>> ListarGastosAsync(
        bool incluirInactivos = false,
        CancellationToken cancelacion = default);

    /// <summary>Crea un gasto recurrente.</summary>
    /// <param name="solicitud">Datos de la obligacion.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El gasto recurrente creado.</returns>
    Task<GastoRecurrenteDto> CrearGastoAsync(
        SolicitudGuardarGastoRecurrente solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Modifica un gasto recurrente.</summary>
    /// <param name="id">Gasto recurrente que se modifica.</param>
    /// <param name="solicitud">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El gasto recurrente actualizado.</returns>
    Task<GastoRecurrenteDto> ActualizarGastoAsync(
        Guid id,
        SolicitudGuardarGastoRecurrente solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Cambia el estado de un gasto recurrente.</summary>
    /// <param name="id">Gasto recurrente afectado.</param>
    /// <param name="estado">Activa, Pausada o Finalizada.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El gasto recurrente actualizado.</returns>
    Task<GastoRecurrenteDto> CambiarEstadoGastoAsync(
        Guid id,
        string estado,
        CancellationToken cancelacion = default);

    /// <summary>Elimina un gasto recurrente.</summary>
    /// <param name="id">Gasto recurrente que se elimina.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando queda eliminado.</returns>
    Task EliminarGastoAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>
    /// Confirma que la obligacion se pago: crea el movimiento y adelanta la proxima fecha.
    /// </summary>
    /// <param name="id">Gasto recurrente que se paga.</param>
    /// <param name="solicitud">Importe y fecha reales del pago.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El movimiento creado.</returns>
    Task<MovimientoResumen> RegistrarPagoAsync(
        Guid id,
        SolicitudRegistrarPagoRecurrente solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Lista los ingresos recurrentes del espacio.</summary>
    /// <param name="incluirInactivos">Si se incluyen los pausados y finalizados.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Los ingresos recurrentes.</returns>
    Task<IReadOnlyList<IngresoRecurrenteDto>> ListarIngresosAsync(
        bool incluirInactivos = false,
        CancellationToken cancelacion = default);

    /// <summary>Crea un ingreso recurrente.</summary>
    /// <param name="solicitud">Datos del ingreso.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El ingreso recurrente creado.</returns>
    Task<IngresoRecurrenteDto> CrearIngresoAsync(
        SolicitudGuardarIngresoRecurrente solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Modifica un ingreso recurrente.</summary>
    /// <param name="id">Ingreso recurrente que se modifica.</param>
    /// <param name="solicitud">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El ingreso recurrente actualizado.</returns>
    Task<IngresoRecurrenteDto> ActualizarIngresoAsync(
        Guid id,
        SolicitudGuardarIngresoRecurrente solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Elimina un ingreso recurrente.</summary>
    /// <param name="id">Ingreso recurrente que se elimina.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando queda eliminado.</returns>
    Task EliminarIngresoAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>
    /// Confirma que el ingreso se recibio: crea el movimiento y adelanta la proxima fecha.
    /// </summary>
    /// <param name="id">Ingreso recurrente que se cobra.</param>
    /// <param name="solicitud">Importe y fecha reales del cobro.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El movimiento creado.</returns>
    Task<MovimientoResumen> RegistrarCobroAsync(
        Guid id,
        SolicitudRegistrarPagoRecurrente solicitud,
        CancellationToken cancelacion = default);
}
