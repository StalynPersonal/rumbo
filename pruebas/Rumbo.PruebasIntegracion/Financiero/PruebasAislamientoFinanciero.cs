using System.Net;
using System.Net.Http.Json;

using Rumbo.Contratos.Comun;
using Rumbo.Contratos.Movimientos;
using Rumbo.PruebasIntegracion.Autenticacion;

namespace Rumbo.PruebasIntegracion.Financiero;

// Aislamiento entre hogares sobre los datos que de verdad importan: las cuentas y el dinero.
//
// Las pruebas de aislamiento anteriores usaban invitaciones y espacios. Estas atacan los
// endpoints financieros, que son los que manejan la informacion sensible.
public class PruebasAislamientoFinanciero(FabricaApiDePrueba fabrica)
    : IClassFixture<FabricaApiDePrueba>
{
    [Fact]
    public async Task UnHogarNoVeLasCuentasDeOtro()
    {
        var hogarA = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "aisla1a@ejemplo.com", "Hogar Aislado A");

        var hogarB = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "aisla1b@ejemplo.com", "Hogar Aislado B");

        await AyudanteFinanciero.CrearCuentaAsync(hogarB, "Cuenta secreta de B", 999_999m);

        var cuentasDeA = await hogarA.GetFromJsonAsync<List<Rumbo.Contratos.Cuentas.CuentaResumen>>(
            "/api/v1/cuentas");

        Assert.Empty(cuentasDeA!);
    }

    [Fact]
    public async Task PedirLaCuentaDeOtroHogarPorSuIdDevuelve404()
    {
        // El ataque mas simple: conseguir un identificador y pedirlo directamente. Debe
        // responder 404 y no 403: un 403 confirmaria que la cuenta existe.
        var hogarA = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "aisla2a@ejemplo.com", "Hogar Aislado A2");

        var hogarB = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "aisla2b@ejemplo.com", "Hogar Aislado B2");

        var cuentaDeB = await AyudanteFinanciero.CrearCuentaAsync(hogarB, "Nómina de B", 80_000m);

        var respuesta = await hogarA.GetAsync($"/api/v1/cuentas/{cuentaDeB.Id}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task NoSePuedeRegistrarUnMovimientoEnLaCuentaDeOtroHogar()
    {
        var hogarA = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "aisla3a@ejemplo.com", "Hogar Aislado A3");

        var hogarB = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "aisla3b@ejemplo.com", "Hogar Aislado B3");

        var cuentaDeB = await AyudanteFinanciero.CrearCuentaAsync(hogarB, "Nómina de B", 80_000m);
        var categoriaDeA = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(hogarA);

        var respuesta = await hogarA.PostAsJsonAsync(
            "/api/v1/movimientos",
            new SolicitudRegistrarMovimiento(
                "Gasto", cuentaDeB.Id, categoriaDeA.Id, 1_000m, null,
                new DateOnly(2026, 9, 15), "Intrusión", null, null, "Personal", null, null));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);

        // Y el saldo de B no se toco.
        var cuentaDespues = await AyudanteFinanciero.LeerCuentaAsync(hogarB, cuentaDeB.Id);
        Assert.Equal(80_000m, cuentaDespues.SaldoActual);
    }

    [Fact]
    public async Task NoSePuedeTransferirHaciaLaCuentaDeOtroHogar()
    {
        // Seria la forma mas directa de sacar dinero de un hogar: transferir a una cuenta
        // ajena.
        var hogarA = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "aisla4a@ejemplo.com", "Hogar Aislado A4");

        var hogarB = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "aisla4b@ejemplo.com", "Hogar Aislado B4");

        var cuentaDeA = await AyudanteFinanciero.CrearCuentaAsync(hogarA, "Nómina de A", 50_000m);
        var cuentaDeB = await AyudanteFinanciero.CrearCuentaAsync(hogarB, "Nómina de B", 10_000m);

        var respuesta = await hogarA.PostAsJsonAsync(
            "/api/v1/movimientos/transferencias",
            new SolicitudTransferir(
                cuentaDeA.Id, cuentaDeB.Id, 50_000m, null,
                new DateOnly(2026, 9, 15), "Fuga", 0m, null, null, null));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);

        // Ningun saldo cambio.
        Assert.Equal(50_000m, (await AyudanteFinanciero.LeerCuentaAsync(hogarA, cuentaDeA.Id)).SaldoActual);
        Assert.Equal(10_000m, (await AyudanteFinanciero.LeerCuentaAsync(hogarB, cuentaDeB.Id)).SaldoActual);
    }

    [Fact]
    public async Task ElLibroMayorDeCadaHogarEsIndependiente()
    {
        var hogarA = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "aisla5a@ejemplo.com", "Hogar Aislado A5");

        var hogarB = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "aisla5b@ejemplo.com", "Hogar Aislado B5");

        var cuentaB = await AyudanteFinanciero.CrearCuentaAsync(hogarB, "Nómina de B", 40_000m);
        var categoriaB = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(hogarB);

        await AyudanteFinanciero.RegistrarAsync(
            hogarB, "Gasto", cuentaB.Id, categoriaB.Id, 7_000m, "Gasto privado de B");

        var libroDeA = await hogarA.GetFromJsonAsync<ResultadoPaginado<MovimientoResumen>>(
            "/api/v1/movimientos");

        // Ni las filas ni el total revelan nada: un contador mal filtrado ya diria cuanto
        // se mueve en el otro hogar.
        Assert.Empty(libroDeA!.Elementos);
        Assert.Equal(0, libroDeA.TotalElementos);
    }

    [Fact]
    public async Task UnHogarNoVeLasCategoriasPropiasDeOtro()
    {
        var hogarA = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "aisla6a@ejemplo.com", "Hogar Aislado A6");

        var hogarB = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "aisla6b@ejemplo.com", "Hogar Aislado B6");

        await hogarB.PostAsJsonAsync(
            "/api/v1/categorias",
            new Rumbo.Contratos.Categorias.SolicitudCrearCategoria(
                "Categoría privada de B", "Gasto", null, null, null));

        var categoriasDeA = await hogarA
            .GetFromJsonAsync<List<Rumbo.Contratos.Categorias.CategoriaArbol>>("/api/v1/categorias");

        Assert.DoesNotContain(categoriasDeA!, c => c.Nombre == "Categoría privada de B");
    }
}
