using System.Net;
using System.Net.Http.Json;

using Rumbo.Contratos.Movimientos;
using Rumbo.Contratos.Recurrentes;
using Rumbo.PruebasIntegracion.Autenticacion;

namespace Rumbo.PruebasIntegracion.Financiero;

// Obligaciones e ingresos que se repiten.
//
// Lo mas importante que verifican: Rumbo NO crea el movimiento solo al llegar la fecha. Si
// lo hiciera, el saldo dejaria de reflejar la realidad en cuanto un pago se retrasara.
public class PruebasRecurrentes(FabricaApiDePrueba fabrica) : IClassFixture<FabricaApiDePrueba>
{
    private static SolicitudGuardarGastoRecurrente Internet(Guid categoriaId, Guid cuentaId) =>
        new("Internet", categoriaId, cuentaId, 2_000m, null, true, "Mensual",
            new DateOnly(2026, 9, 25), 3, "Compartido", null);

    [Fact]
    public async Task CrearUnGastoRecurrenteNoMueveNingunSaldo()
    {
        // Es una plantilla de un movimiento futuro, no un movimiento.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "recur1@ejemplo.com", "Hogar Recurrentes 1");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 50_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/gastos-recurrentes", Internet(categoria.Id, cuenta.Id));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        // El saldo no se toco: todavia no se ha pagado nada.
        var cuentaDespues = await AyudanteFinanciero.LeerCuentaAsync(cliente, cuenta.Id);
        Assert.Equal(50_000m, cuentaDespues.SaldoActual);
    }

    [Fact]
    public async Task ConfirmarElPagoCreaElMovimientoYAdelantaLaFecha()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "recur2@ejemplo.com", "Hogar Recurrentes 2");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 50_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var gasto = (await (await cliente.PostAsJsonAsync(
            "/api/v1/gastos-recurrentes", Internet(categoria.Id, cuenta.Id)))
            .Content.ReadFromJsonAsync<GastoRecurrenteDto>())!;

        var pago = await cliente.PostAsJsonAsync(
            $"/api/v1/gastos-recurrentes/{gasto.Id}/pagar",
            new SolicitudRegistrarPagoRecurrente(null, null, null));

        pago.EnsureSuccessStatusCode();

        var movimiento = (await pago.Content.ReadFromJsonAsync<MovimientoResumen>())!;

        Assert.Equal("Gasto", movimiento.Tipo);
        Assert.Equal(2_000m, movimiento.Monto);

        // Ahora si baja el saldo.
        var cuentaDespues = await AyudanteFinanciero.LeerCuentaAsync(cliente, cuenta.Id);
        Assert.Equal(48_000m, cuentaDespues.SaldoActual);

        // Y la proxima fecha avanza un mes.
        var actualizados = await cliente.GetFromJsonAsync<List<GastoRecurrenteDto>>(
            "/api/v1/gastos-recurrentes");

        var despues = actualizados!.First(g => g.Id == gasto.Id);

        Assert.Equal(new DateOnly(2026, 10, 25), despues.ProximaFechaPago);
        Assert.Equal(new DateOnly(2026, 9, 25), despues.UltimaFechaPago);
    }

    [Fact]
    public async Task ElImporteRealMandaSobreElEstimado()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "recur3@ejemplo.com", "Hogar Recurrentes 3");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 50_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var gasto = (await (await cliente.PostAsJsonAsync(
            "/api/v1/gastos-recurrentes", Internet(categoria.Id, cuenta.Id)))
            .Content.ReadFromJsonAsync<GastoRecurrenteDto>())!;

        // La factura vino por 2.350, no por los 2.000 estimados.
        var pago = await cliente.PostAsJsonAsync(
            $"/api/v1/gastos-recurrentes/{gasto.Id}/pagar",
            new SolicitudRegistrarPagoRecurrente(2_350m, null, null));

        var movimiento = (await pago.Content.ReadFromJsonAsync<MovimientoResumen>())!;

        Assert.Equal(2_350m, movimiento.Monto);
        Assert.Equal(47_650m, (await AyudanteFinanciero.LeerCuentaAsync(cliente, cuenta.Id)).SaldoActual);
    }

    [Fact]
    public async Task UnGastoPausadoNoAdmitePagos()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "recur4@ejemplo.com", "Hogar Recurrentes 4");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 50_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var gasto = (await (await cliente.PostAsJsonAsync(
            "/api/v1/gastos-recurrentes", Internet(categoria.Id, cuenta.Id)))
            .Content.ReadFromJsonAsync<GastoRecurrenteDto>())!;

        await cliente.PutAsync(
            $"/api/v1/gastos-recurrentes/{gasto.Id}/estado?estado=Pausada", content: null);

        var pago = await cliente.PostAsJsonAsync(
            $"/api/v1/gastos-recurrentes/{gasto.Id}/pagar",
            new SolicitudRegistrarPagoRecurrente(null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, pago.StatusCode);
    }

    [Fact]
    public async Task UnaCategoriaDeIngresoNoSirveParaUnGastoRecurrente()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "recur5@ejemplo.com", "Hogar Recurrentes 5");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 50_000m);
        var categoriaIngreso = await AyudanteFinanciero.ObtenerCategoriaDeIngresoAsync(cliente);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/gastos-recurrentes", Internet(categoriaIngreso.Id, cuenta.Id));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task UnHogarNoVeLosRecurrentesDeOtro()
    {
        var hogarA = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "recur6a@ejemplo.com", "Hogar Recurrentes A");

        var hogarB = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "recur6b@ejemplo.com", "Hogar Recurrentes B");

        var cuentaB = await AyudanteFinanciero.CrearCuentaAsync(hogarB, "Nómina de B", 50_000m);
        var categoriaB = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(hogarB);

        await hogarB.PostAsJsonAsync(
            "/api/v1/gastos-recurrentes", Internet(categoriaB.Id, cuentaB.Id));

        var deA = await hogarA.GetFromJsonAsync<List<GastoRecurrenteDto>>(
            "/api/v1/gastos-recurrentes");

        Assert.Empty(deA!);
    }
}
