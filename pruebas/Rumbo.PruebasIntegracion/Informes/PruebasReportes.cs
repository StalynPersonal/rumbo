using System.Net;
using System.Net.Http.Json;

using Rumbo.Contratos.Movimientos;
using Rumbo.Contratos.Reportes;
using Rumbo.PruebasIntegracion.Autenticacion;
using Rumbo.PruebasIntegracion.Financiero;

namespace Rumbo.PruebasIntegracion.Informes;

// Todos los informes salen del libro mayor y ninguno cuenta las transferencias. Esa regla se
// rompe sola en cuanto alguien escribe un informe nuevo, asi que se fija aqui.
public class PruebasReportes(FabricaApiDePrueba fabrica) : IClassFixture<FabricaApiDePrueba>
{
    [Fact]
    public async Task ElResumenSumaIngresosYGastosYCalculaLaTasaDeAhorro()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "reporte1@ejemplo.com", "Hogar Reportes 1");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 0m);
        var gasto = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);
        var ingreso = await AyudanteFinanciero.ObtenerCategoriaDeIngresoAsync(cliente);

        await AyudanteFinanciero.RegistrarAsync(
            cliente, "Ingreso", cuenta.Id, ingreso.Id, 80_000m, "Sueldo",
            new DateOnly(2026, 9, 1));

        await AyudanteFinanciero.RegistrarAsync(
            cliente, "Gasto", cuenta.Id, gasto.Id, 60_000m, "Gastos del mes",
            new DateOnly(2026, 9, 10));

        var resumen = await cliente.GetFromJsonAsync<ResumenPeriodo>(
            "/api/v1/reportes/resumen?Desde=2026-09-01&Hasta=2026-09-30");

        Assert.Equal(80_000m, resumen!.TotalIngresos);
        Assert.Equal(60_000m, resumen.TotalGastos);
        Assert.Equal(20_000m, resumen.Balance);

        // De cada 100 que entraron, quedaron 25 sin gastar.
        Assert.Equal(25m, resumen.TasaDeAhorro);
        Assert.Equal(2, resumen.CantidadMovimientos);
    }

    [Fact]
    public async Task NingunInformeCuentaLasTransferencias()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "reporte2@ejemplo.com", "Hogar Reportes 2");

        var nomina = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 100_000m);
        var ahorro = await AyudanteFinanciero.CrearCuentaAsync(
            cliente, "Ahorro", 0m, tipo: "Ahorro");

        await cliente.PostAsJsonAsync(
            "/api/v1/movimientos/transferencias",
            new SolicitudTransferir(
                nomina.Id, ahorro.Id, 40_000m, null,
                new DateOnly(2026, 9, 10), "Traspaso", 0m, null, null, null));

        var resumen = await cliente.GetFromJsonAsync<ResumenPeriodo>(
            "/api/v1/reportes/resumen?Desde=2026-09-01&Hasta=2026-09-30");

        Assert.Equal(0m, resumen!.TotalIngresos);
        Assert.Equal(0m, resumen.TotalGastos);

        var categorias = await cliente.GetFromJsonAsync<ReporteCategorias>(
            "/api/v1/reportes/categorias?Desde=2026-09-01&Hasta=2026-09-30");

        Assert.Empty(categorias!.Categorias);

        var mensual = await cliente.GetFromJsonAsync<ReporteMensual>(
            "/api/v1/reportes/mensual?Desde=2026-09-01&Hasta=2026-09-30");

        Assert.All(mensual!.Meses, m => Assert.Equal(0m, m.Gastos));
    }

    [Fact]
    public async Task ElInformePorCategoriaOrdenaDeMayorAMenorYCalculaPorcentajes()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "reporte3@ejemplo.com", "Hogar Reportes 3");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 100_000m);

        var arbol = await cliente.GetFromJsonAsync<List<Rumbo.Contratos.Categorias.CategoriaArbol>>(
            "/api/v1/categorias");

        var padres = arbol!.Where(c => c.Tipo == "Gasto" && c.Subcategorias.Count > 0).ToList();
        var primera = padres[0].Subcategorias[0];
        var segunda = padres[1].Subcategorias[0];

        await AyudanteFinanciero.RegistrarAsync(
            cliente, "Gasto", cuenta.Id, primera.Id, 30_000m, "Grande",
            new DateOnly(2026, 9, 5));

        await AyudanteFinanciero.RegistrarAsync(
            cliente, "Gasto", cuenta.Id, segunda.Id, 10_000m, "Pequeño",
            new DateOnly(2026, 9, 6));

        var informe = await cliente.GetFromJsonAsync<ReporteCategorias>(
            "/api/v1/reportes/categorias?Desde=2026-09-01&Hasta=2026-09-30");

        Assert.Equal(2, informe!.Categorias.Count);
        Assert.Equal(30_000m, informe.Categorias[0].Total);
        Assert.Equal(75m, informe.Categorias[0].Porcentaje);
        Assert.Equal(25m, informe.Categorias[1].Porcentaje);

        // Los porcentajes suman 100: es un desglose, no una muestra.
        Assert.Equal(100m, informe.Categorias.Sum(c => c.Porcentaje));
    }

    [Fact]
    public async Task LaSerieMensualIncluyeLosMesesSinMovimientos()
    {
        // Un mes vacio es informacion. Saltarselo deformaria la grafica.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "reporte4@ejemplo.com", "Hogar Reportes 4");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 100_000m);
        var gasto = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        await AyudanteFinanciero.RegistrarAsync(
            cliente, "Gasto", cuenta.Id, gasto.Id, 5_000m, "Solo en julio",
            new DateOnly(2026, 7, 10));

        var informe = await cliente.GetFromJsonAsync<ReporteMensual>(
            "/api/v1/reportes/mensual?Desde=2026-07-01&Hasta=2026-09-30");

        Assert.Equal(3, informe!.Meses.Count);
        Assert.Equal(5_000m, informe.Meses[0].Gastos);
        Assert.Equal(0m, informe.Meses[1].Gastos);
        Assert.Equal(0m, informe.Meses[2].Gastos);

        // El promedio reparte entre los tres meses, no entre el único con datos.
        Assert.Equal(1_666.67m, informe.PromedioGastos);
    }

    [Fact]
    public async Task ElRepartoMuestraQuienPusoDeMasEnLosGastosCompartidos()
    {
        // Rumbo registra y reporta el desbalance, pero no liquida entre personas.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "reporte5@ejemplo.com", "Hogar Reportes 5");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 100_000m);
        var gasto = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        await cliente.PostAsJsonAsync(
            "/api/v1/movimientos",
            new SolicitudRegistrarMovimiento(
                "Gasto", cuenta.Id, gasto.Id, 12_000m, null,
                new DateOnly(2026, 9, 10), "Supermercado del mes",
                null, null, "Compartido", null, null));

        var informe = await cliente.GetFromJsonAsync<ReporteReparto>(
            "/api/v1/reportes/reparto?Desde=2026-09-01&Hasta=2026-09-30");

        Assert.Equal(12_000m, informe!.TotalCompartido);

        // El hogar tiene un solo miembro, así que le toca todo y no hay desbalance.
        var unico = Assert.Single(informe.Miembros);

        Assert.Equal(12_000m, unico.TotalPagado);
        Assert.Equal(12_000m, unico.ParteQueLeToca);
        Assert.Equal(0m, unico.Diferencia);
    }

    [Fact]
    public async Task UnaFechaInicialPosteriorALaFinalEsRechazada()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "reporte6@ejemplo.com", "Hogar Reportes 6");

        var respuesta = await cliente.GetAsync(
            "/api/v1/reportes/resumen?Desde=2026-09-30&Hasta=2026-09-01");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task LosInformesDeUnEspacioNoIncluyenMovimientosDeOtro()
    {
        var hogarA = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "reporteA@ejemplo.com", "Hogar Reportes A");
        var hogarB = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "reporteB@ejemplo.com", "Hogar Reportes B");

        var cuentaA = await AyudanteFinanciero.CrearCuentaAsync(hogarA, "Nómina A", 100_000m);
        var gastoA = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(hogarA);

        await AyudanteFinanciero.RegistrarAsync(
            hogarA, "Gasto", cuentaA.Id, gastoA.Id, 45_000m, "Gasto privado de A",
            new DateOnly(2026, 9, 10));

        var informeDeB = await hogarB.GetFromJsonAsync<ResumenPeriodo>(
            "/api/v1/reportes/resumen?Desde=2026-09-01&Hasta=2026-09-30");

        Assert.Equal(0m, informeDeB!.TotalGastos);
        Assert.Equal(0, informeDeB.CantidadMovimientos);
    }
}
