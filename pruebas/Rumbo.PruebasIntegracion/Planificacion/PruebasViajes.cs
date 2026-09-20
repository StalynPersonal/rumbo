using System.Net;
using System.Net.Http.Json;

using Rumbo.Contratos.Metas;
using Rumbo.Contratos.Movimientos;
using Rumbo.Contratos.Viajes;
using Rumbo.PruebasIntegracion.Autenticacion;
using Rumbo.PruebasIntegracion.Financiero;

namespace Rumbo.PruebasIntegracion.Planificacion;

// Un viaje tiene dos caras: el presupuesto (lo que se piensa gastar) y el fondo (lo que se
// lleva ahorrado). Confundirlas es el error clasico, y estas pruebas lo impiden.
public class PruebasViajes(FabricaApiDePrueba fabrica) : IClassFixture<FabricaApiDePrueba>
{
    /// <summary>Crea un viaje y devuelve su detalle.</summary>
    /// <param name="cliente">Cliente ya autenticado.</param>
    /// <param name="solicitud">Datos del viaje.</param>
    /// <returns>El viaje creado.</returns>
    private static async Task<ViajeDetalle> CrearViajeAsync(
        HttpClient cliente,
        SolicitudGuardarViaje solicitud)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/v1/viajes", solicitud);

        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<ViajeDetalle>())!;
    }

    /// <summary>Desglose de ejemplo: vuelos, hospedaje y comida.</summary>
    /// <returns>Las partidas.</returns>
    private static IReadOnlyList<LineaViajeSolicitud> DesgloseDeEjemplo() =>
    [
        new LineaViajeSolicitud("Vuelos", 60_000m, "Ida y vuelta con equipaje"),
        new LineaViajeSolicitud("Hospedaje", 40_000m, null),
        new LineaViajeSolicitud("Alimentacion", 20_000m, null),
    ];

    [Fact]
    public async Task ElPresupuestoTotalEsLaSumaDeLasPartidas()
    {
        // No se envía por separado: si se guardaran las dos cosas podrían no cuadrar, y
        // entonces habría que decidir cuál manda.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "viaje1@ejemplo.com", "Hogar Viajes 1");

        var viaje = await CrearViajeAsync(cliente, new SolicitudGuardarViaje(
            "Viaje a Colombia", "Cartagena", null,
            new DateOnly(2027, 6, 10), new DateOnly(2027, 6, 20),
            "DOP", 2, null, DesgloseDeEjemplo()));

        Assert.Equal(120_000m, viaje.PresupuestoTotal);
        Assert.Equal(120_000m, viaje.TotalPlanificadoEnLineas);

        // Y el coste por persona sale solo.
        Assert.Equal(60_000m, viaje.CostoPorViajero);
        Assert.Equal(11, viaje.DuracionEnDias);
    }

    [Fact]
    public async Task ElGastoRealSeAgrupaPorPartidaDelViaje()
    {
        // Es donde se ve que el hospedaje se disparó y los vuelos salieron baratos.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "viaje2@ejemplo.com", "Hogar Viajes 2");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 200_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var viaje = await CrearViajeAsync(cliente, new SolicitudGuardarViaje(
            "Viaje a Colombia", "Cartagena", null,
            new DateOnly(2027, 6, 10), new DateOnly(2027, 6, 20),
            "DOP", 2, null, DesgloseDeEjemplo()));

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/movimientos",
            new SolicitudRegistrarMovimiento(
                "Gasto", cuenta.Id, categoria.Id, 55_000m, null,
                new DateOnly(2026, 9, 18), "Boletos aéreos",
                null, null, "Compartido", null, viaje.Id, "Vuelos"));

        respuesta.EnsureSuccessStatusCode();

        var tras = await cliente.GetFromJsonAsync<ViajeDetalle>($"/api/v1/viajes/{viaje.Id}");

        Assert.Equal(55_000m, tras!.TotalGastado);

        var vuelos = tras.Lineas.First(l => l.Categoria == "Vuelos");

        Assert.Equal(55_000m, vuelos.MontoGastado);
        Assert.Equal(5_000m, vuelos.MontoDisponible);

        // Las demás partidas siguen intactas.
        Assert.Equal(0m, tras.Lineas.First(l => l.Categoria == "Hospedaje").MontoGastado);
    }

    [Fact]
    public async Task UnaPartidaDeViajeSinViajeEsRechazada()
    {
        // Sería un dato huérfano que después aparece en un informe de viajes sin pertenecer
        // a ninguno.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "viaje3@ejemplo.com", "Hogar Viajes 3");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 50_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/movimientos",
            new SolicitudRegistrarMovimiento(
                "Gasto", cuenta.Id, categoria.Id, 1_000m, null,
                new DateOnly(2026, 9, 18), "Un gasto cualquiera",
                null, null, "Personal", null, null, "Vuelos"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task ElFondoDelViajeSaleDeLaMetaYNoSeDuplica()
    {
        // El fondo no se guarda en el viaje: se lee de la meta. Duplicarlo abriría la puerta
        // a que las dos cifras dijeran cosas distintas sobre el mismo dinero.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "viaje4@ejemplo.com", "Hogar Viajes 4");

        var nomina = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 150_000m);
        var ahorro = await AyudanteFinanciero.CrearCuentaAsync(
            cliente, "Fondo del viaje", 0m, tipo: "Ahorro");

        var meta = await cliente.PostAsJsonAsync("/api/v1/metas", new SolicitudGuardarMeta(
            "Fondo Colombia", null, 120_000m, "DOP",
            new DateOnly(2027, 6, 1), "Alta", 10_000m, ahorro.Id, null));

        var metaCreada = (await meta.Content.ReadFromJsonAsync<MetaDetalle>())!;

        var viaje = await CrearViajeAsync(cliente, new SolicitudGuardarViaje(
            "Viaje a Colombia", "Cartagena", null,
            new DateOnly(2027, 6, 10), new DateOnly(2027, 6, 20),
            "DOP", 2, metaCreada.Id, DesgloseDeEjemplo()));

        Assert.Equal(0m, viaje.FondoActual);

        await cliente.PostAsJsonAsync(
            $"/api/v1/metas/{metaCreada.Id}/aportes",
            new SolicitudAportarAMeta(
                nomina.Id, 30_000m, new DateOnly(2026, 9, 20), null, false));

        var tras = await cliente.GetFromJsonAsync<ViajeDetalle>($"/api/v1/viajes/{viaje.Id}");

        Assert.Equal(30_000m, tras!.FondoActual);
        Assert.Equal(25m, tras.PorcentajeFinanciado);

        // Y el aporte al fondo NO es un gasto del viaje: es ahorro.
        Assert.Equal(0m, tras.TotalGastado);
    }

    [Fact]
    public async Task LaViabilidadDevuelveTresEscenariosYNoMueveDinero()
    {
        // Es la pregunta que da sentido a la aplicación. Y es una proyección: no reserva
        // nada, no crea aportes, no toca un solo saldo.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "viaje5@ejemplo.com", "Hogar Viajes 5");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 100_000m);

        var viaje = await CrearViajeAsync(cliente, new SolicitudGuardarViaje(
            "Viaje a Colombia", "Cartagena", null,
            new DateOnly(2027, 12, 10), new DateOnly(2027, 12, 20),
            "DOP", 2, null, DesgloseDeEjemplo()));

        var viabilidad = await cliente.GetFromJsonAsync<ViabilidadViajeDto>(
            $"/api/v1/viajes/{viaje.Id}/viabilidad");

        Assert.Equal(120_000m, viabilidad!.CostoTotal);
        Assert.Equal(3, viabilidad.Escenarios.Count);
        Assert.Equal("Conservador", viabilidad.Escenarios[0].Nombre);
        Assert.Equal("Optimista", viabilidad.Escenarios[2].Nombre);
        Assert.False(string.IsNullOrWhiteSpace(viabilidad.Explicacion));

        // Sin historial, la respuesta se marca como estimación en lugar de callarse.
        Assert.True(viabilidad.ConfianzaBaja);

        // Y no se movió nada.
        var tras = await AyudanteFinanciero.LeerCuentaAsync(cliente, cuenta.Id);

        Assert.Equal(100_000m, tras.SaldoActual);
    }

    [Fact]
    public async Task UnaPartidaRepetidaEsRechazada()
    {
        // El gasto real se compararía contra un importe ambiguo y el desglose dejaría de
        // sumar el total.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "viaje6@ejemplo.com", "Hogar Viajes 6");

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/viajes",
            new SolicitudGuardarViaje(
                "Mal planteado", null, null,
                new DateOnly(2027, 6, 10), new DateOnly(2027, 6, 20),
                "DOP", 1, null,
                [
                    new LineaViajeSolicitud("Vuelos", 10_000m, null),
                    new LineaViajeSolicitud("Vuelos", 20_000m, null),
                ]));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task UnViajeQueTerminaAntesDeEmpezarEsRechazado()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "viaje7@ejemplo.com", "Hogar Viajes 7");

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/viajes",
            new SolicitudGuardarViaje(
                "Imposible", null, null,
                new DateOnly(2027, 6, 20), new DateOnly(2027, 6, 10),
                "DOP", 1, null,
                [new LineaViajeSolicitud("Vuelos", 10_000m, null)]));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task UnViajeConGastosNoSePuedeEliminar()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "viaje8@ejemplo.com", "Hogar Viajes 8");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 100_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var viaje = await CrearViajeAsync(cliente, new SolicitudGuardarViaje(
            "Viaje con gastos", null, null,
            new DateOnly(2027, 6, 10), new DateOnly(2027, 6, 20),
            "DOP", 1, null, DesgloseDeEjemplo()));

        await cliente.PostAsJsonAsync(
            "/api/v1/movimientos",
            new SolicitudRegistrarMovimiento(
                "Gasto", cuenta.Id, categoria.Id, 3_000m, null,
                new DateOnly(2026, 9, 18), "Seguro de viaje",
                null, null, "Personal", null, viaje.Id, "Seguro"));

        var borrado = await cliente.DeleteAsync($"/api/v1/viajes/{viaje.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, borrado.StatusCode);

        // Cancelarlo sí funciona y conserva el historial.
        var cancelado = await cliente.PutAsJsonAsync(
            $"/api/v1/viajes/{viaje.Id}/estado",
            new SolicitudCambiarEstadoViaje("Cancelado"));

        cancelado.EnsureSuccessStatusCode();

        var detalle = await cancelado.Content.ReadFromJsonAsync<ViajeDetalle>();

        Assert.Equal("Cancelado", detalle!.Estado);
        Assert.Equal(3_000m, detalle.TotalGastado);
    }

    [Fact]
    public async Task LosViajesDeUnEspacioNoSonVisiblesDesdeOtro()
    {
        var hogarA = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "viajeA@ejemplo.com", "Hogar Viajes A");
        var hogarB = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "viajeB@ejemplo.com", "Hogar Viajes B");

        var deA = await CrearViajeAsync(hogarA, new SolicitudGuardarViaje(
            "Viaje privado de A", null, null,
            new DateOnly(2027, 6, 10), new DateOnly(2027, 6, 20),
            "DOP", 1, null, DesgloseDeEjemplo()));

        // 404, no 403: confirmar que el identificador existe ya sería filtrar información.
        var respuesta = await hogarB.GetAsync($"/api/v1/viajes/{deA.Id}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);

        var viabilidad = await hogarB.GetAsync($"/api/v1/viajes/{deA.Id}/viabilidad");

        Assert.Equal(HttpStatusCode.NotFound, viabilidad.StatusCode);

        var listaDeB = await hogarB.GetFromJsonAsync<List<ViajeDetalle>>("/api/v1/viajes");

        Assert.Empty(listaDeB!);
    }
}
