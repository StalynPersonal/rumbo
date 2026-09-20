using System.Net;
using System.Net.Http.Json;

using Rumbo.Contratos.Cuentas;
using Rumbo.Contratos.Movimientos;
using Rumbo.PruebasIntegracion.Autenticacion;

namespace Rumbo.PruebasIntegracion.Financiero;

// El saldo guardado es una instantanea; la verdad es la suma del libro mayor. Estas pruebas
// verifican que las dos cifras nunca se separan.
public class PruebasSaldos(FabricaApiDePrueba fabrica) : IClassFixture<FabricaApiDePrueba>
{
    [Fact]
    public async Task ElSaldoRefejaLosMovimientosRegistrados()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "saldo1@ejemplo.com", "Hogar Saldos 1");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 50_000m);
        var gasto = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);
        var ingreso = await AyudanteFinanciero.ObtenerCategoriaDeIngresoAsync(cliente);

        await AyudanteFinanciero.RegistrarAsync(
            cliente, "Ingreso", cuenta.Id, ingreso.Id, 20_000m, "Bono");

        await AyudanteFinanciero.RegistrarAsync(
            cliente, "Gasto", cuenta.Id, gasto.Id, 5_000m, "Supermercado");

        await AyudanteFinanciero.RegistrarAsync(
            cliente, "Gasto", cuenta.Id, gasto.Id, 2_500m, "Restaurante");

        var final = await AyudanteFinanciero.LeerCuentaAsync(cliente, cuenta.Id);

        // 50.000 + 20.000 - 5.000 - 2.500
        Assert.Equal(62_500m, final.SaldoActual);
    }

    [Fact]
    public async Task LaReconciliacionNoEncuentraDesviacion()
    {
        // Es la comprobacion de que la instantanea y el libro mayor dicen lo mismo. Si esta
        // prueba fallara, significaria que en algun camino se actualiza uno y no el otro.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "saldo2@ejemplo.com", "Hogar Saldos 2");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 30_000m);
        var gasto = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        await AyudanteFinanciero.RegistrarAsync(
            cliente, "Gasto", cuenta.Id, gasto.Id, 1_200m, "Combustible");

        await AyudanteFinanciero.RegistrarAsync(
            cliente, "Gasto", cuenta.Id, gasto.Id, 800m, "Parqueo");

        var respuesta = await cliente.PostAsync(
            $"/api/v1/cuentas/{cuenta.Id}/reconciliar", content: null);

        respuesta.EnsureSuccessStatusCode();

        var reconciliacion = (await respuesta.Content
            .ReadFromJsonAsync<ResultadoReconciliacion>())!;

        Assert.Equal(0m, reconciliacion.Desviacion);
        Assert.Equal(28_000m, reconciliacion.SaldoCalculado);
        Assert.Equal(2, reconciliacion.CantidadMovimientos);
    }

    [Fact]
    public async Task EliminarUnMovimientoRevierteElSaldo()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "saldo3@ejemplo.com", "Hogar Saldos 3");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 10_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var movimiento = await AyudanteFinanciero.RegistrarAsync(
            cliente, "Gasto", cuenta.Id, categoria.Id, 3_000m, "Compra");

        Assert.Equal(7_000m, (await AyudanteFinanciero.LeerCuentaAsync(cliente, cuenta.Id)).SaldoActual);

        var borrado = await cliente.DeleteAsync($"/api/v1/movimientos/{movimiento.Id}");
        Assert.Equal(HttpStatusCode.NoContent, borrado.StatusCode);

        Assert.Equal(10_000m, (await AyudanteFinanciero.LeerCuentaAsync(cliente, cuenta.Id)).SaldoActual);
    }

    [Fact]
    public async Task CambiarElImporteAjustaElSaldoPorLaDiferencia()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "saldo4@ejemplo.com", "Hogar Saldos 4");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 10_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var movimiento = await AyudanteFinanciero.RegistrarAsync(
            cliente, "Gasto", cuenta.Id, categoria.Id, 1_000m, "Compra mal anotada");

        // Se corrige: eran 1.500, no 1.000.
        var respuesta = await cliente.PutAsJsonAsync(
            $"/api/v1/movimientos/{movimiento.Id}",
            new SolicitudActualizarMovimiento(
                categoria.Id, 1_500m, new DateOnly(2026, 9, 15), "Compra corregida",
                null, null, "Personal", null, null));

        respuesta.EnsureSuccessStatusCode();

        // Solo se aplica la diferencia de 500, no se suma otra vez el importe entero.
        var final = await AyudanteFinanciero.LeerCuentaAsync(cliente, cuenta.Id);
        Assert.Equal(8_500m, final.SaldoActual);
    }

    [Fact]
    public async Task UnImporteNegativoEsRechazado()
    {
        // La direccion la marca el tipo, no el signo del importe. Si se admitiera un gasto
        // negativo, el saldo subiria al registrar un gasto.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "saldo5@ejemplo.com", "Hogar Saldos 5");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 10_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/movimientos",
            new SolicitudRegistrarMovimiento(
                "Gasto", cuenta.Id, categoria.Id, -500m, null,
                new DateOnly(2026, 9, 15), "Gasto negativo", null, null, "Personal", null, null));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task NoSePuedeEliminarUnaCuentaConMovimientos()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "saldo6@ejemplo.com", "Hogar Saldos 6");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 10_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        await AyudanteFinanciero.RegistrarAsync(
            cliente, "Gasto", cuenta.Id, categoria.Id, 500m, "Compra");

        var respuesta = await cliente.DeleteAsync($"/api/v1/cuentas/{cuenta.Id}");

        // Borrarla dejaria sus movimientos apuntando a una cuenta invisible.
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }
}
