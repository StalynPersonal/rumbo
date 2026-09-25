using Rumbo.Contratos.Deudas;

namespace Rumbo.Movil.Servicios;

/// <summary>
/// Deudas del hogar y los pagos que las reducen.
/// </summary>
/// <param name="api">Cliente HTTP.</param>
public class ServicioApiDeudas(ClienteApi api)
{
    /// <summary>Lista las deudas activas.</summary>
    /// <returns>Las deudas, de mayor a menor saldo.</returns>
    public Task<List<DeudaDetalle>> ListarAsync() =>
        api.ObtenerAsync<List<DeudaDetalle>>("api/v1/deudas");

    /// <summary>Crea una deuda.</summary>
    /// <param name="solicitud">Datos de la deuda.</param>
    /// <returns>La deuda creada.</returns>
    public Task<DeudaDetalle> CrearAsync(SolicitudGuardarDeuda solicitud) =>
        api.EnviarAsync<SolicitudGuardarDeuda, DeudaDetalle>("api/v1/deudas", solicitud);

    /// <summary>Registra un pago contra una deuda.</summary>
    /// <param name="deudaId">Deuda que se paga.</param>
    /// <param name="solicitud">Cuenta, desglose del pago y fecha.</param>
    /// <returns>El pago registrado.</returns>
    /// <remarks>
    /// El pago SI es un gasto, a diferencia de un aporte a una meta: el dinero sale de la
    /// cuenta y no aparece en ningun otro sitio del hogar. Que la parte de capital reduzca
    /// una deuda no cambia que ese dinero ya no esta disponible este mes.
    /// </remarks>
    public Task<PagoDeudaDto> PagarAsync(Guid deudaId, SolicitudPagarDeuda solicitud) =>
        api.EnviarAsync<SolicitudPagarDeuda, PagoDeudaDto>(
            $"api/v1/deudas/{deudaId}/pagos", solicitud);
}
