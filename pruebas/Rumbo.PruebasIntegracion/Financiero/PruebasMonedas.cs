using System.Net;
using System.Net.Http.Json;

using Rumbo.Contratos.Monedas;
using Rumbo.Contratos.Movimientos;
using Rumbo.PruebasIntegracion.Autenticacion;

namespace Rumbo.PruebasIntegracion.Financiero;

// Multi-moneda. Sin tasas cargadas, registrar en otra divisa debe fallar con un mensaje
// claro: inventar una paridad produciria un importe plausible y falso.
public class PruebasMonedas(FabricaApiDePrueba fabrica) : IClassFixture<FabricaApiDePrueba>
{
    [Fact]
    public async Task ElCatalogoTraeLasMonedasSembradas()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "moneda1@ejemplo.com", "Hogar Monedas 1");

        var monedas = await cliente.GetFromJsonAsync<List<MonedaDto>>("/api/v1/monedas");

        Assert.Contains(monedas!, m => m.Codigo == "DOP");
        Assert.Contains(monedas!, m => m.Codigo == "USD");
    }

    [Fact]
    public async Task SinTasaCargadaNoSePuedeRegistrarEnOtraMoneda()
    {
        // Es el comportamiento deseado: mejor un error visible que un importe inventado.
        //
        // Se usa GBP y no USD a proposito. Las tasas de cambio son datos GLOBALES, no de un
        // hogar: una cargada por otra prueba de esta misma clase seria visible aqui y esta
        // comprobacion pasaria a depender del orden de ejecucion. Ninguna otra prueba toca
        // el par GBP-DOP.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "moneda2@ejemplo.com", "Hogar Monedas 2");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(
            cliente, "Cuenta en libras", 1_000m, moneda: "GBP");

        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/movimientos",
            new SolicitudRegistrarMovimiento(
                "Gasto", cuenta.Id, categoria.Id, 100m, "GBP",
                new DateOnly(2026, 9, 15), "Compra en libras",
                null, null, "Personal", null, null));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("tasa de cambio", cuerpo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConLaTasaCargadaElMovimientoSeConvierteALaMonedaBase()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "moneda3@ejemplo.com", "Hogar Monedas 3");

        // El dólar a 60 pesos.
        var tasa = await cliente.PostAsJsonAsync(
            "/api/v1/tasas-cambio",
            new SolicitudGuardarTasa("USD", "DOP", new DateOnly(2026, 9, 15), 60m));

        tasa.EnsureSuccessStatusCode();

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(
            cliente, "Cuenta en dólares", 1_000m, moneda: "USD");

        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var movimiento = await AyudanteFinanciero.RegistrarAsync(
            cliente, "Gasto", cuenta.Id, categoria.Id, 100m, "Compra en dólares");

        // 100 dólares a 60 son 6.000 pesos, congelados en el movimiento.
        Assert.Equal("USD", movimiento.Moneda);
        Assert.Equal(100m, movimiento.Monto);
        Assert.Equal(6_000m, movimiento.MontoEnMonedaBase);
        Assert.False(movimiento.TasaEsAproximada);
    }

    [Fact]
    public async Task SiFaltaLaTasaDeEseDiaSeUsaLaAnteriorYSeMarcaComoAproximada()
    {
        // Los fines de semana y festivos no tienen cotización: la del viernes sigue siendo
        // la referencia válida el domingo.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "moneda4@ejemplo.com", "Hogar Monedas 4");

        await cliente.PostAsJsonAsync(
            "/api/v1/tasas-cambio",
            new SolicitudGuardarTasa("USD", "DOP", new DateOnly(2026, 9, 11), 60m));

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(
            cliente, "Cuenta en dólares", 1_000m, moneda: "USD");

        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        // Se registra el domingo 13, sin cotización propia.
        var movimiento = await AyudanteFinanciero.RegistrarAsync(
            cliente, "Gasto", cuenta.Id, categoria.Id, 50m, "Compra del domingo",
            new DateOnly(2026, 9, 13));

        Assert.Equal(3_000m, movimiento.MontoEnMonedaBase);
        Assert.True(movimiento.TasaEsAproximada);
    }

    [Fact]
    public async Task GuardarDosVecesLaMismaFechaActualizaLaTasa()
    {
        // Dos valores para el mismo día harían que el mismo movimiento se convirtiera
        // distinto según cuál se leyera.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "moneda5@ejemplo.com", "Hogar Monedas 5");

        var fecha = new DateOnly(2026, 9, 20);

        await cliente.PostAsJsonAsync(
            "/api/v1/tasas-cambio", new SolicitudGuardarTasa("USD", "DOP", fecha, 60m));

        await cliente.PostAsJsonAsync(
            "/api/v1/tasas-cambio", new SolicitudGuardarTasa("USD", "DOP", fecha, 61.5m));

        var tasas = await cliente.GetFromJsonAsync<List<TasaCambioDto>>(
            "/api/v1/tasas-cambio?origen=USD&destino=DOP");

        var deEseDia = tasas!.Where(t => t.Fecha == fecha).ToList();

        Assert.Single(deEseDia);
        Assert.Equal(61.5m, deEseDia[0].Tasa);
    }

    [Fact]
    public async Task LaConversionInversaSeDeduceDeLaTasaDirecta()
    {
        // Cargar USD a DOP y deducir DOP a USD evita mantener el doble de filas para lo
        // mismo, con el riesgo de que se contradigan.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "moneda6@ejemplo.com", "Hogar Monedas 6");

        await cliente.PostAsJsonAsync(
            "/api/v1/tasas-cambio",
            new SolicitudGuardarTasa("USD", "DOP", new DateOnly(2026, 9, 15), 60m));

        var resultado = await cliente.GetFromJsonAsync<ResultadoConversionDto>(
            "/api/v1/tasas-cambio/convertir?monto=6000&origen=DOP&destino=USD&fecha=2026-09-15");

        Assert.Equal(100m, resultado!.MontoConvertido);
    }
}
