using System.Net;
using System.Net.Http.Json;

using Rumbo.Contratos.Comun;
using Rumbo.Contratos.Deudas;
using Rumbo.Contratos.Movimientos;
using Rumbo.PruebasIntegracion.Autenticacion;

namespace Rumbo.PruebasIntegracion.Financiero;

// Una deuda no es una cuenta con saldo negativo: es una obligacion con su calendario. Pero
// cada pago si mueve dinero real, y eso tiene que cuadrar siempre.
public class PruebasDeudas(FabricaApiDePrueba fabrica) : IClassFixture<FabricaApiDePrueba>
{
    /// <summary>Crea una deuda y devuelve su detalle.</summary>
    /// <param name="cliente">Cliente ya autenticado.</param>
    /// <param name="nombre">Nombre de la deuda.</param>
    /// <param name="original">Importe original.</param>
    /// <param name="saldo">Saldo pendiente.</param>
    /// <param name="cuota">Cuota mensual.</param>
    /// <returns>La deuda creada.</returns>
    private static async Task<DeudaDetalle> CrearDeudaAsync(
        HttpClient cliente,
        string nombre,
        decimal original,
        decimal saldo,
        decimal? cuota = null)
    {
        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/deudas",
            new SolicitudGuardarDeuda(
                nombre, "Prestamo", "Banco Popular", original, saldo, "DOP",
                18.5m, null, cuota, 15, new DateOnly(2025, 1, 15), null, null, null));

        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<DeudaDetalle>())!;
    }

    [Fact]
    public async Task UnPagoReduceLaDeudaYElSaldoDeLaCuentaALaVez()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "deuda1@ejemplo.com", "Hogar Deudas 1");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 100_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var deuda = await CrearDeudaAsync(cliente, "Préstamo del carro", 500_000m, 300_000m);

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/deudas/{deuda.Id}/pagos",
            new SolicitudPagarDeuda(
                cuenta.Id, categoria.Id, 12_000m, 3_000m, 500m,
                new DateOnly(2026, 9, 15), "Cuota de septiembre", null));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        var pago = (await respuesta.Content.ReadFromJsonAsync<PagoDeudaDto>())!;

        // El total es capital + interés + cargos: no se envía, se calcula.
        Assert.Equal(15_500m, pago.MontoTotal);
        Assert.Equal(288_000m, pago.SaldoPosterior);

        var cuentaDespues = await AyudanteFinanciero.LeerCuentaAsync(cliente, cuenta.Id);
        var deudaDespues = await cliente.GetFromJsonAsync<DeudaDetalle>(
            $"/api/v1/deudas/{deuda.Id}");

        // De la cuenta salió el total; de la deuda solo bajó el capital.
        Assert.Equal(84_500m, cuentaDespues.SaldoActual);
        Assert.Equal(288_000m, deudaDespues!.SaldoActual);
        Assert.Equal(3_000m, deudaDespues.InteresTotalPagado);
    }

    [Fact]
    public async Task UnPagoDeDeudaSiCuentaComoGasto()
    {
        // A diferencia de una transferencia, el dinero sale del hogar y no aparece en
        // ningun otro sitio. El presupuesto familiar tiene que contarlo.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "deuda2@ejemplo.com", "Hogar Deudas 2");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 100_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var deuda = await CrearDeudaAsync(cliente, "Tarjeta", 80_000m, 50_000m);

        await cliente.PostAsJsonAsync(
            $"/api/v1/deudas/{deuda.Id}/pagos",
            new SolicitudPagarDeuda(
                cuenta.Id, categoria.Id, 5_000m, 1_200m, 0m,
                new DateOnly(2026, 9, 15), null, null));

        var pagina = await cliente.GetFromJsonAsync<ResultadoPaginado<MovimientoResumen>>(
            "/api/v1/movimientos?Pagina=1&TamanoPagina=50");

        var movimiento = Assert.Single(pagina!.Elementos);

        Assert.Equal("Gasto", movimiento.Tipo);
        Assert.Equal(6_200m, movimiento.Monto);
    }

    [Fact]
    public async Task UnCapitalMayorQueLoQueSeDebeEsRechazado()
    {
        // Dejaria la deuda en negativo, es decir, el banco debiendo dinero al hogar.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "deuda3@ejemplo.com", "Hogar Deudas 3");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 100_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var deuda = await CrearDeudaAsync(cliente, "Préstamo pequeño", 20_000m, 5_000m);

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/deudas/{deuda.Id}/pagos",
            new SolicitudPagarDeuda(
                cuenta.Id, categoria.Id, 9_000m, 0m, 0m,
                new DateOnly(2026, 9, 15), null, null));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        // Y no se movió nada.
        var sinTocar = await AyudanteFinanciero.LeerCuentaAsync(cliente, cuenta.Id);

        Assert.Equal(100_000m, sinTocar.SaldoActual);
    }

    [Fact]
    public async Task AlLlegarACeroLaDeudaSeCierraSola()
    {
        // Dejarla activa con saldo cero obligaria a acordarse de cerrarla a mano.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "deuda4@ejemplo.com", "Hogar Deudas 4");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 100_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var deuda = await CrearDeudaAsync(cliente, "Último préstamo", 20_000m, 4_000m);

        await cliente.PostAsJsonAsync(
            $"/api/v1/deudas/{deuda.Id}/pagos",
            new SolicitudPagarDeuda(
                cuenta.Id, categoria.Id, 4_000m, 100m, 0m,
                new DateOnly(2026, 9, 15), "Pago final", null));

        var tras = await cliente.GetFromJsonAsync<DeudaDetalle>($"/api/v1/deudas/{deuda.Id}");

        Assert.Equal(0m, tras!.SaldoActual);
        Assert.Equal("Saldada", tras.Estado);
        Assert.Equal(100m, tras.PorcentajePagado);

        // Y ya no admite más pagos.
        var otro = await cliente.PostAsJsonAsync(
            $"/api/v1/deudas/{deuda.Id}/pagos",
            new SolicitudPagarDeuda(
                cuenta.Id, categoria.Id, 1_000m, 0m, 0m,
                new DateOnly(2026, 9, 20), null, null));

        Assert.Equal(HttpStatusCode.BadRequest, otro.StatusCode);
    }

    [Fact]
    public async Task ElSaldoDeUnaDeudaNoSePuedeEditarAMano()
    {
        // Si se pudiera, el saldo y el historial de pagos dirian cosas distintas y no
        // habria forma de saber cual es la buena.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "deuda5@ejemplo.com", "Hogar Deudas 5");

        var deuda = await CrearDeudaAsync(cliente, "Hipoteca", 2_000_000m, 1_500_000m);

        var respuesta = await cliente.PutAsJsonAsync(
            $"/api/v1/deudas/{deuda.Id}",
            new SolicitudGuardarDeuda(
                "Hipoteca", "Hipoteca", "Banco Popular", 2_000_000m, 10m, "DOP",
                9.5m, null, 25_000m, 5, new DateOnly(2025, 1, 15), null, null, "Renegociada"));

        respuesta.EnsureSuccessStatusCode();

        var tras = await respuesta.Content.ReadFromJsonAsync<DeudaDetalle>();

        // El resto sí cambió, pero el saldo se ignoró.
        Assert.Equal(1_500_000m, tras!.SaldoActual);
        Assert.Equal("Renegociada", tras.Notas);
        Assert.Equal(9.5m, tras.TasaInteres);
    }

    [Fact]
    public async Task LosMesesRestantesSeEstimanConLaCuota()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "deuda6@ejemplo.com", "Hogar Deudas 6");

        // 100.000 pendientes a 10.000 al mes son 10 meses.
        var deuda = await CrearDeudaAsync(
            cliente, "Préstamo personal", 150_000m, 100_000m, cuota: 10_000m);

        Assert.Equal(10, deuda.MesesEstimadosRestantes);

        // Sin cuota no se estima nada: inventar un plazo seria peor que no dar ninguno.
        var sinCuota = await CrearDeudaAsync(cliente, "Deuda sin cuota", 50_000m, 30_000m);

        Assert.Null(sinCuota.MesesEstimadosRestantes);
    }

    [Fact]
    public async Task UnaDeudaConPagosNoSePuedeEliminar()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "deuda7@ejemplo.com", "Hogar Deudas 7");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 100_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var deuda = await CrearDeudaAsync(cliente, "Con pagos", 50_000m, 40_000m);

        await cliente.PostAsJsonAsync(
            $"/api/v1/deudas/{deuda.Id}/pagos",
            new SolicitudPagarDeuda(
                cuenta.Id, categoria.Id, 2_000m, 0m, 0m,
                new DateOnly(2026, 9, 15), null, null));

        var borrado = await cliente.DeleteAsync($"/api/v1/deudas/{deuda.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, borrado.StatusCode);
    }

    [Fact]
    public async Task LasDeudasDeUnEspacioNoSonVisiblesDesdeOtro()
    {
        var hogarA = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "deudaA@ejemplo.com", "Hogar Deudas A");
        var hogarB = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "deudaB@ejemplo.com", "Hogar Deudas B");

        var deA = await CrearDeudaAsync(hogarA, "Deuda privada de A", 100_000m, 80_000m);

        var respuesta = await hogarB.GetAsync($"/api/v1/deudas/{deA.Id}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);

        var listaDeB = await hogarB.GetFromJsonAsync<List<DeudaDetalle>>("/api/v1/deudas");

        Assert.Empty(listaDeB!);
    }
}
