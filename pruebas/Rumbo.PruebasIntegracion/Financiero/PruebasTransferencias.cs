using System.Net;
using System.Net.Http.Json;

using Rumbo.Contratos.Comun;
using Rumbo.Contratos.Movimientos;
using Rumbo.PruebasIntegracion.Autenticacion;

namespace Rumbo.PruebasIntegracion.Financiero;

// LA REGLA MAS IMPORTANTE DEL LIBRO MAYOR: una transferencia no es un gasto.
//
// Es un requisito explicito del proyecto y el tipo de regla que se rompe sola en cuanto
// alguien escribe un informe nuevo. Estas pruebas la fijan a nivel de API.
public class PruebasTransferencias(FabricaApiDePrueba fabrica) : IClassFixture<FabricaApiDePrueba>
{
    [Fact]
    public async Task UnaTransferenciaMueveElDineroSinCrearloNiDestruirlo()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "transfer1@ejemplo.com", "Hogar Transferencias 1");

        var nomina = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 50_000m);
        var ahorro = await AyudanteFinanciero.CrearCuentaAsync(
            cliente, "Ahorro", 10_000m, tipo: "Ahorro");

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/movimientos/transferencias",
            new SolicitudTransferir(
                nomina.Id, ahorro.Id, 10_000m, null,
                new DateOnly(2026, 9, 15), "Ahorro mensual", 0m, null, null, null));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        var nominaDespues = await AyudanteFinanciero.LeerCuentaAsync(cliente, nomina.Id);
        var ahorroDespues = await AyudanteFinanciero.LeerCuentaAsync(cliente, ahorro.Id);

        Assert.Equal(40_000m, nominaDespues.SaldoActual);
        Assert.Equal(20_000m, ahorroDespues.SaldoActual);

        // El total del hogar no cambia: el dinero solo cambio de sitio.
        Assert.Equal(60_000m, nominaDespues.SaldoActual + ahorroDespues.SaldoActual);
    }

    [Fact]
    public async Task UnaTransferenciaNoCuentaComoGastoNiComoIngreso()
    {
        // Si contara, ahorrar pareceria empobrecer y el informe del mes seria falso.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "transfer2@ejemplo.com", "Hogar Transferencias 2");

        var nomina = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 50_000m);
        var ahorro = await AyudanteFinanciero.CrearCuentaAsync(
            cliente, "Ahorro", 0m, tipo: "Ahorro");

        await cliente.PostAsJsonAsync(
            "/api/v1/movimientos/transferencias",
            new SolicitudTransferir(
                nomina.Id, ahorro.Id, 15_000m, null,
                new DateOnly(2026, 9, 15), "Traspaso", 0m, null, null, null));

        var pagina = await cliente.GetFromJsonAsync<ResultadoPaginado<MovimientoResumen>>(
            "/api/v1/movimientos?Pagina=1&TamanoPagina=50");

        var asientos = pagina!.Elementos;

        // Se crearon DOS asientos, no uno.
        Assert.Equal(2, asientos.Count);
        Assert.All(asientos, m => Assert.Equal("Transferencia", m.Tipo));

        // Y NINGUNO cuenta para ingresos ni gastos.
        Assert.All(asientos, m => Assert.False(m.CuentaParaIngresosYGastos));

        // Los dos comparten el mismo identificador de transferencia.
        Assert.Single(asientos.Select(m => m.TransferenciaId).Distinct());

        // Uno resta y el otro suma.
        Assert.Contains(asientos, m => m.Signo == -1);
        Assert.Contains(asientos, m => m.Signo == 1);
    }

    [Fact]
    public async Task UnGastoSiCuentaParaLosTotales()
    {
        // Control: la regla anterior no puede deberse a que nada cuente nunca.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "transfer3@ejemplo.com", "Hogar Transferencias 3");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 50_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var gasto = await AyudanteFinanciero.RegistrarAsync(
            cliente, "Gasto", cuenta.Id, categoria.Id, 5_000m, "Supermercado");

        Assert.True(gasto.CuentaParaIngresosYGastos);
        Assert.Equal(-1, gasto.Signo);

        var cuentaDespues = await AyudanteFinanciero.LeerCuentaAsync(cliente, cuenta.Id);
        Assert.Equal(45_000m, cuentaDespues.SaldoActual);
    }

    [Fact]
    public async Task NoSePuedeTransferirALaMismaCuenta()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "transfer4@ejemplo.com", "Hogar Transferencias 4");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 10_000m);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/movimientos/transferencias",
            new SolicitudTransferir(
                cuenta.Id, cuenta.Id, 1_000m, null,
                new DateOnly(2026, 9, 15), "Absurdo", 0m, null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task NoSePuedeCrearUnMovimientoDeTipoTransferenciaSuelto()
    {
        // Si se admitiera, apareceria una pata sin su contraria y el dinero surgiria de la
        // nada en una cuenta.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "transfer5@ejemplo.com", "Hogar Transferencias 5");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 10_000m);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/movimientos",
            new SolicitudRegistrarMovimiento(
                "Transferencia", cuenta.Id, null, 1_000m, null,
                new DateOnly(2026, 9, 15), "Pata suelta", null, null, "Personal", null, null));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }
}
