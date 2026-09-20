using System.Net;
using System.Net.Http.Json;

using Rumbo.Contratos.Presupuestos;
using Rumbo.PruebasIntegracion.Autenticacion;
using Rumbo.PruebasIntegracion.Financiero;

namespace Rumbo.PruebasIntegracion.Planificacion;

// Un presupuesto no impide gastar: compara lo planificado con lo realmente gastado y avisa.
// El gasto real siempre sale del libro mayor, nunca de un contador aparte.
public class PruebasPresupuestos(FabricaApiDePrueba fabrica)
    : IClassFixture<FabricaApiDePrueba>
{
    /// <summary>Crea un presupuesto y devuelve su detalle.</summary>
    /// <param name="cliente">Cliente ya autenticado.</param>
    /// <param name="solicitud">Datos del presupuesto.</param>
    /// <returns>El presupuesto creado.</returns>
    private static async Task<PresupuestoDetalle> CrearAsync(
        HttpClient cliente,
        SolicitudGuardarPresupuesto solicitud)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/v1/presupuestos", solicitud);

        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<PresupuestoDetalle>())!;
    }

    [Fact]
    public async Task ElConsumoDeUnaPartidaSaleDeLosMovimientosReales()
    {
        // El ejemplo de referencia: 8.000 presupuestados, 6.800 gastados, quedan 1.200,
        // un 85 % consumido.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "presu1@ejemplo.com", "Hogar Presupuestos 1");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 50_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var presupuesto = await CrearAsync(cliente, new SolicitudGuardarPresupuesto(
            "Septiembre 2026", "Mensual",
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            "DOP", null,
            [new LineaPresupuestoSolicitud(categoria.Id, 8_000m, null, null, null)]));

        Assert.Equal(8_000m, presupuesto.TotalAsignado);
        Assert.Equal(0m, presupuesto.TotalGastado);

        await AyudanteFinanciero.RegistrarAsync(
            cliente, "Gasto", cuenta.Id, categoria.Id, 6_800m, "Compras del mes",
            new DateOnly(2026, 9, 15));

        var tras = await cliente.GetFromJsonAsync<PresupuestoDetalle>(
            $"/api/v1/presupuestos/{presupuesto.Id}");

        var linea = Assert.Single(tras!.Lineas);

        Assert.Equal(6_800m, linea.MontoGastado);
        Assert.Equal(1_200m, linea.MontoDisponible);
        Assert.Equal(85m, linea.PorcentajeConsumido);
        Assert.Equal("Aviso", linea.Nivel);
    }

    [Fact]
    public async Task UnGastoFueraDelPeriodoNoConsumeElPresupuesto()
    {
        // Si contara, cerrar un mes no serviria de nada: el presupuesto de septiembre
        // seguiria moviendose en octubre.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "presu2@ejemplo.com", "Hogar Presupuestos 2");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 50_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var presupuesto = await CrearAsync(cliente, new SolicitudGuardarPresupuesto(
            "Septiembre 2026", "Mensual",
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            "DOP", null,
            [new LineaPresupuestoSolicitud(categoria.Id, 5_000m, null, null, null)]));

        await AyudanteFinanciero.RegistrarAsync(
            cliente, "Gasto", cuenta.Id, categoria.Id, 3_000m, "Gasto de octubre",
            new DateOnly(2026, 10, 5));

        var tras = await cliente.GetFromJsonAsync<PresupuestoDetalle>(
            $"/api/v1/presupuestos/{presupuesto.Id}");

        Assert.Equal(0m, Assert.Single(tras!.Lineas).MontoGastado);
    }

    [Fact]
    public async Task UnaTransferenciaNoConsumeElPresupuesto()
    {
        // La regla mas importante del libro mayor, comprobada tambien aqui: mover dinero
        // entre cuentas propias no es gastar.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "presu3@ejemplo.com", "Hogar Presupuestos 3");

        var nomina = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 50_000m);
        var ahorro = await AyudanteFinanciero.CrearCuentaAsync(
            cliente, "Ahorro", 0m, tipo: "Ahorro");
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var presupuesto = await CrearAsync(cliente, new SolicitudGuardarPresupuesto(
            "Septiembre 2026", "Mensual",
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            "DOP", null,
            [new LineaPresupuestoSolicitud(categoria.Id, 10_000m, null, null, null)]));

        await cliente.PostAsJsonAsync(
            "/api/v1/movimientos/transferencias",
            new Rumbo.Contratos.Movimientos.SolicitudTransferir(
                nomina.Id, ahorro.Id, 9_000m, null,
                new DateOnly(2026, 9, 10), "Traspaso", 0m, null, null, null));

        var tras = await cliente.GetFromJsonAsync<PresupuestoDetalle>(
            $"/api/v1/presupuestos/{presupuesto.Id}");

        Assert.Equal(0m, tras!.TotalGastado);
    }

    [Fact]
    public async Task UnPresupuestoSobreUnaCategoriaDeIngresoEsRechazado()
    {
        // Un presupuesto limita gastos. Presupuestar un ingreso no significa nada.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "presu4@ejemplo.com", "Hogar Presupuestos 4");

        var ingreso = await AyudanteFinanciero.ObtenerCategoriaDeIngresoAsync(cliente);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/presupuestos",
            new SolicitudGuardarPresupuesto(
                "Mal planteado", "Mensual",
                new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
                "DOP", null,
                [new LineaPresupuestoSolicitud(ingreso.Id, 1_000m, null, null, null)]));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task UnaCategoriaRepetidaEnDosPartidasEsRechazada()
    {
        // Con la categoria repetida, el consumo se compararia contra un limite ambiguo.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "presu5@ejemplo.com", "Hogar Presupuestos 5");

        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/presupuestos",
            new SolicitudGuardarPresupuesto(
                "Duplicado", "Mensual",
                new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
                "DOP", null,
                [
                    new LineaPresupuestoSolicitud(categoria.Id, 1_000m, null, null, null),
                    new LineaPresupuestoSolicitud(categoria.Id, 2_000m, null, null, null),
                ]));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task EliminarUnPresupuestoNoBorraNingunMovimiento()
    {
        // Un presupuesto es una intencion de gasto, no dinero. El historial permanece.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "presu6@ejemplo.com", "Hogar Presupuestos 6");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 50_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var presupuesto = await CrearAsync(cliente, new SolicitudGuardarPresupuesto(
            "Septiembre 2026", "Mensual",
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            "DOP", null,
            [new LineaPresupuestoSolicitud(categoria.Id, 5_000m, null, null, null)]));

        await AyudanteFinanciero.RegistrarAsync(
            cliente, "Gasto", cuenta.Id, categoria.Id, 2_000m, "Supermercado",
            new DateOnly(2026, 9, 12));

        var borrado = await cliente.DeleteAsync($"/api/v1/presupuestos/{presupuesto.Id}");

        Assert.Equal(HttpStatusCode.NoContent, borrado.StatusCode);

        // El saldo sigue reflejando el gasto: nada se deshizo.
        var tras = await AyudanteFinanciero.LeerCuentaAsync(cliente, cuenta.Id);

        Assert.Equal(48_000m, tras.SaldoActual);
    }

    [Fact]
    public async Task LosPresupuestosDeUnEspacioNoSonVisiblesDesdeOtro()
    {
        var hogarA = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "presuA@ejemplo.com", "Hogar Presupuestos A");
        var hogarB = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "presuB@ejemplo.com", "Hogar Presupuestos B");

        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(hogarA);

        var deA = await CrearAsync(hogarA, new SolicitudGuardarPresupuesto(
            "Privado de A", "Mensual",
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            "DOP", null,
            [new LineaPresupuestoSolicitud(categoria.Id, 1_000m, null, null, null)]));

        var respuesta = await hogarB.GetAsync($"/api/v1/presupuestos/{deA.Id}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }
}
