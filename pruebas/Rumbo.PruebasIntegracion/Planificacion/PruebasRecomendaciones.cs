using System.Net;
using System.Net.Http.Json;

using Rumbo.Contratos.Comun;
using Rumbo.Contratos.Metas;
using Rumbo.Contratos.Movimientos;
using Rumbo.Contratos.Presupuestos;
using Rumbo.Contratos.Recomendaciones;
using Rumbo.PruebasIntegracion.Autenticacion;
using Rumbo.PruebasIntegracion.Financiero;

namespace Rumbo.PruebasIntegracion.Planificacion;

// El motor de recomendaciones es determinista: reglas y aritmetica, sin inteligencia
// artificial. Y, sobre todo, NUNCA mueve dinero. Aceptar una sugerencia solo registra la
// decision; el movimiento lo crea la persona aparte.
public class PruebasRecomendaciones(FabricaApiDePrueba fabrica)
    : IClassFixture<FabricaApiDePrueba>
{
    [Fact]
    public async Task UnaMetaConPlazoGeneraUnaSugerenciaDeAporteConSusInsumos()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "recom1@ejemplo.com", "Hogar Recomendaciones 1");

        var ahorro = await AyudanteFinanciero.CrearCuentaAsync(
            cliente, "Ahorro", 0m, tipo: "Ahorro");

        var meta = await cliente.PostAsJsonAsync("/api/v1/metas", new SolicitudGuardarMeta(
            "Viaje a Japón", null, 180_000m, "DOP",
            new DateOnly(2027, 12, 15), "Alta", 10_000m, ahorro.Id, null));

        meta.EnsureSuccessStatusCode();

        var respuesta = await cliente.PostAsync("/api/v1/recomendaciones/recalcular", null);

        respuesta.EnsureSuccessStatusCode();

        var sugerencias = await respuesta.Content
            .ReadFromJsonAsync<List<RecomendacionDto>>();

        var aporte = Assert.Single(sugerencias!, s => s.MetaId is not null);

        Assert.Equal("Pendiente", aporte.Estado);
        Assert.NotNull(aporte.MontoSugerido);

        // Cada sugerencia guarda con que datos se calculo. Es lo que permite responder
        // «¿de dónde sale este número?» en lugar de pedir fe.
        Assert.False(string.IsNullOrWhiteSpace(aporte.Insumos));
    }

    [Fact]
    public async Task AceptarUnaSugerenciaNoMueveDinero()
    {
        // El principio innegociable del proyecto. Si el sistema transfiriera por su cuenta,
        // aunque fuera con buena intencion, dejaria de ser fiable.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "recom2@ejemplo.com", "Hogar Recomendaciones 2");

        var nomina = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 60_000m);
        var ahorro = await AyudanteFinanciero.CrearCuentaAsync(
            cliente, "Ahorro", 0m, tipo: "Ahorro");

        await cliente.PostAsJsonAsync("/api/v1/metas", new SolicitudGuardarMeta(
            "Fondo de emergencia", null, 150_000m, "DOP",
            new DateOnly(2027, 9, 30), "Critica", 12_000m, ahorro.Id, null));

        var recalculo = await cliente.PostAsync("/api/v1/recomendaciones/recalcular", null);
        var sugerencias = await recalculo.Content
            .ReadFromJsonAsync<List<RecomendacionDto>>();

        var primera = sugerencias!.First();

        var aceptada = await cliente.PutAsJsonAsync(
            $"/api/v1/recomendaciones/{primera.Id}/respuesta",
            new SolicitudResponderRecomendacion("Aceptada"));

        aceptada.EnsureSuccessStatusCode();

        var detalle = await aceptada.Content.ReadFromJsonAsync<RecomendacionDto>();

        Assert.Equal("Aceptada", detalle!.Estado);

        // Y ahora lo importante: ningun saldo se movio y no hay ni un solo movimiento.
        var nominaDespues = await AyudanteFinanciero.LeerCuentaAsync(cliente, nomina.Id);
        var ahorroDespues = await AyudanteFinanciero.LeerCuentaAsync(cliente, ahorro.Id);

        Assert.Equal(60_000m, nominaDespues.SaldoActual);
        Assert.Equal(0m, ahorroDespues.SaldoActual);

        var pagina = await cliente.GetFromJsonAsync<ResultadoPaginado<MovimientoResumen>>(
            "/api/v1/movimientos?Pagina=1&TamanoPagina=50");

        Assert.Empty(pagina!.Elementos);
    }

    [Fact]
    public async Task UnaSugerenciaYaRespondidaNoSePuedeVolverAResponder()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "recom3@ejemplo.com", "Hogar Recomendaciones 3");

        var ahorro = await AyudanteFinanciero.CrearCuentaAsync(
            cliente, "Ahorro", 0m, tipo: "Ahorro");

        await cliente.PostAsJsonAsync("/api/v1/metas", new SolicitudGuardarMeta(
            "Cambiar el carro", null, 900_000m, "DOP",
            new DateOnly(2028, 6, 30), "Media", 20_000m, ahorro.Id, null));

        var recalculo = await cliente.PostAsync("/api/v1/recomendaciones/recalcular", null);
        var sugerencias = await recalculo.Content
            .ReadFromJsonAsync<List<RecomendacionDto>>();

        var id = sugerencias!.First().Id;

        var primera = await cliente.PutAsJsonAsync(
            $"/api/v1/recomendaciones/{id}/respuesta",
            new SolicitudResponderRecomendacion("Descartada"));

        primera.EnsureSuccessStatusCode();

        var segunda = await cliente.PutAsJsonAsync(
            $"/api/v1/recomendaciones/{id}/respuesta",
            new SolicitudResponderRecomendacion("Aceptada"));

        Assert.Equal(HttpStatusCode.BadRequest, segunda.StatusCode);
    }

    [Fact]
    public async Task RecalcularSustituyeLasPendientesPeroConservaLasRespondidas()
    {
        // Si se acumularan, a la semana habria veinte sugerencias contradictorias. Y volver
        // a proponer algo ya rechazado seria molesto.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "recom4@ejemplo.com", "Hogar Recomendaciones 4");

        var ahorro = await AyudanteFinanciero.CrearCuentaAsync(
            cliente, "Ahorro", 0m, tipo: "Ahorro");

        await cliente.PostAsJsonAsync("/api/v1/metas", new SolicitudGuardarMeta(
            "Mudanza", null, 200_000m, "DOP",
            new DateOnly(2027, 8, 31), "Alta", 15_000m, ahorro.Id, null));

        var primera = await cliente.PostAsync("/api/v1/recomendaciones/recalcular", null);
        var lote1 = await primera.Content.ReadFromJsonAsync<List<RecomendacionDto>>();

        await cliente.PutAsJsonAsync(
            $"/api/v1/recomendaciones/{lote1![0].Id}/respuesta",
            new SolicitudResponderRecomendacion("Descartada"));

        await cliente.PostAsync("/api/v1/recomendaciones/recalcular", null);

        var todas = await cliente.GetFromJsonAsync<List<RecomendacionDto>>(
            "/api/v1/recomendaciones?incluirRespondidas=true");

        // La descartada sigue ahí: es el historial de decisiones del hogar.
        Assert.Contains(todas!, r => r.Id == lote1[0].Id && r.Estado == "Descartada");
    }

    [Fact]
    public async Task UnPresupuestoExcedidoGeneraUnaAlerta()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "recom5@ejemplo.com", "Hogar Recomendaciones 5");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 80_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        await cliente.PostAsJsonAsync("/api/v1/presupuestos", new SolicitudGuardarPresupuesto(
            "Septiembre 2026", "Mensual",
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            "DOP", null,
            [new LineaPresupuestoSolicitud(categoria.Id, 5_000m, null, null, null)]));

        await AyudanteFinanciero.RegistrarAsync(
            cliente, "Gasto", cuenta.Id, categoria.Id, 7_500m, "Se fue de las manos",
            new DateOnly(2026, 9, 18));

        var recalculo = await cliente.PostAsync("/api/v1/recomendaciones/recalcular", null);
        var sugerencias = await recalculo.Content
            .ReadFromJsonAsync<List<RecomendacionDto>>();

        Assert.Contains(sugerencias!, s => s.Tipo.Contains("Presupuesto"));
    }

    [Fact]
    public async Task LasSugerenciasDeUnEspacioNoSonVisiblesDesdeOtro()
    {
        var hogarA = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "recomA@ejemplo.com", "Hogar Recomendaciones A");
        var hogarB = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "recomB@ejemplo.com", "Hogar Recomendaciones B");

        var ahorro = await AyudanteFinanciero.CrearCuentaAsync(
            hogarA, "Ahorro", 0m, tipo: "Ahorro");

        await hogarA.PostAsJsonAsync("/api/v1/metas", new SolicitudGuardarMeta(
            "Meta de A", null, 100_000m, "DOP",
            new DateOnly(2027, 9, 30), "Alta", 8_000m, ahorro.Id, null));

        await hogarA.PostAsync("/api/v1/recomendaciones/recalcular", null);

        var deB = await hogarB.GetFromJsonAsync<List<RecomendacionDto>>(
            "/api/v1/recomendaciones");

        Assert.Empty(deB!);
    }
}
