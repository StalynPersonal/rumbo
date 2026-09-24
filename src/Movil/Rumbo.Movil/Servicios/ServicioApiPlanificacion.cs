using Rumbo.Contratos.Metas;
using Rumbo.Contratos.Presupuestos;
using Rumbo.Contratos.Reportes;
using Rumbo.Contratos.Viajes;

namespace Rumbo.Movil.Servicios;

/// <summary>
/// Presupuestos, metas, viajes e informes.
/// </summary>
/// <remarks>
/// Todo lo que en Rumbo es <b>planificar</b> en vez de registrar. Fijate en que ninguno de
/// estos metodos calcula nada: las proyecciones, los porcentajes y la viabilidad vienen ya
/// resueltos del servidor. Si el movil los recalculara, tarde o temprano mostraria una cifra
/// distinta a la del servidor y ninguna de las dos seria creible.
/// </remarks>
/// <param name="api">Cliente HTTP.</param>
public class ServicioApiPlanificacion(ClienteApi api)
{
    /// <summary>Presupuestos con el consumo de cada partida.</summary>
    /// <param name="soloVigente">Si solo se quiere el del periodo actual.</param>
    /// <returns>Los presupuestos.</returns>
    public Task<List<PresupuestoDetalle>> ListarPresupuestosAsync(bool soloVigente = true) =>
        api.ObtenerAsync<List<PresupuestoDetalle>>(
            $"api/v1/presupuestos?soloVigente={soloVigente.ToString().ToLowerInvariant()}");

    /// <summary>Metas de ahorro con su proyeccion.</summary>
    /// <returns>Las metas activas.</returns>
    public Task<List<MetaDetalle>> ListarMetasAsync() =>
        api.ObtenerAsync<List<MetaDetalle>>("api/v1/metas");

    /// <summary>Aporta dinero a una meta.</summary>
    /// <param name="metaId">Meta que recibe el dinero.</param>
    /// <param name="solicitud">Cuenta de origen, importe y fecha.</param>
    /// <returns>La meta con su progreso actualizado.</returns>
    /// <remarks>
    /// Es una TRANSFERENCIA hacia la cuenta de ahorro de la meta, no un gasto. Ahorrar no
    /// empobrece el mes: solo cambia el dinero de sitio.
    /// </remarks>
    public Task<MetaDetalle> AportarAsync(Guid metaId, SolicitudAportarAMeta solicitud) =>
        api.EnviarAsync<SolicitudAportarAMeta, MetaDetalle>(
            $"api/v1/metas/{metaId}/aportes", solicitud);

    /// <summary>Viajes planificados.</summary>
    /// <returns>Los viajes con su presupuesto y su fondo.</returns>
    public Task<List<ViajeDetalle>> ListarViajesAsync() =>
        api.ObtenerAsync<List<ViajeDetalle>>("api/v1/viajes");

    /// <summary>Responde si el hogar puede permitirse un viaje.</summary>
    /// <param name="viajeId">Viaje analizado.</param>
    /// <returns>El veredicto con sus tres escenarios.</returns>
    /// <remarks>
    /// Es una proyeccion para decidir: no mueve dinero, no crea aportes y no reserva nada.
    /// </remarks>
    public Task<ViabilidadViajeDto> ConsultarViabilidadAsync(Guid viajeId) =>
        api.ObtenerAsync<ViabilidadViajeDto>($"api/v1/viajes/{viajeId}/viabilidad");

    /// <summary>Evolucion mes a mes.</summary>
    /// <returns>La serie mensual y sus promedios.</returns>
    public Task<ReporteMensual> ObtenerReporteMensualAsync() =>
        api.ObtenerAsync<ReporteMensual>("api/v1/reportes/mensual");

    /// <summary>Gasto por categoria del periodo.</summary>
    /// <returns>El desglose, de mayor a menor.</returns>
    public Task<ReporteCategorias> ObtenerReporteCategoriasAsync() =>
        api.ObtenerAsync<ReporteCategorias>("api/v1/reportes/categorias");
}
