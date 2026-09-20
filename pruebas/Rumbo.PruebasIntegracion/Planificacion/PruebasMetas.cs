using System.Net;
using System.Net.Http.Json;

using Rumbo.Contratos.Comun;
using Rumbo.Contratos.Metas;
using Rumbo.Contratos.Movimientos;
using Rumbo.PruebasIntegracion.Autenticacion;
using Rumbo.PruebasIntegracion.Financiero;

namespace Rumbo.PruebasIntegracion.Planificacion;

// Una meta de ahorro no guarda dinero: el dinero vive en una cuenta. La meta dice cuanto se
// quiere reunir y para cuando, y el sistema calcula cuanto haria falta aportar al mes.
//
// La regla que estas pruebas fijan: un aporte es una TRANSFERENCIA, nunca un gasto.
public class PruebasMetas(FabricaApiDePrueba fabrica) : IClassFixture<FabricaApiDePrueba>
{
    /// <summary>Crea una meta y devuelve su detalle.</summary>
    /// <param name="cliente">Cliente ya autenticado.</param>
    /// <param name="solicitud">Datos de la meta.</param>
    /// <returns>La meta creada.</returns>
    private static async Task<MetaDetalle> CrearMetaAsync(
        HttpClient cliente,
        SolicitudGuardarMeta solicitud)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/v1/metas", solicitud);

        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<MetaDetalle>())!;
    }

    /// <summary>
    /// Devuelve una fecha objetivo que siempre queda a 15 meses completos de hoy.
    /// </summary>
    /// <returns>La fecha objetivo.</returns>
    /// <remarks>
    /// No se puede escribir una fecha fija: la prueba pasaria hoy y fallaria dentro de un
    /// mes. Se calcula desde hoy. El ajuste del dia cubre el caso en que sumar 15 meses cae
    /// en un mes mas corto (31 de enero + 15 meses = 30 de abril), donde el recuento daria
    /// 14 meses completos en lugar de 15.
    /// </remarks>
    private static DateOnly FechaAQuinceMeses()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var objetivo = hoy.AddMonths(15);

        return objetivo.Day == hoy.Day ? objetivo : objetivo.AddDays(1);
    }

    [Fact]
    public async Task ElAporteMensualNecesarioSaleDeLoQueFaltaEntreLosMesesQueQuedan()
    {
        // El ejemplo de referencia del proyecto: 180.000 de objetivo, 30.000 ya reunidos,
        // 15 meses por delante. (180.000 - 30.000) / 15 = 10.000 al mes.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "metas1@ejemplo.com", "Hogar Metas 1");

        var nomina = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 100_000m);
        var ahorro = await AyudanteFinanciero.CrearCuentaAsync(
            cliente, "Ahorro Japón", 0m, tipo: "Ahorro");

        var meta = await CrearMetaAsync(cliente, new SolicitudGuardarMeta(
            "Viaje a Japón", "Dos semanas", 180_000m, "DOP",
            FechaAQuinceMeses(), "Alta", 10_000m, ahorro.Id, null));

        await cliente.PostAsJsonAsync(
            $"/api/v1/metas/{meta.Id}/aportes",
            new SolicitudAportarAMeta(
                nomina.Id, 30_000m, new DateOnly(2026, 9, 15), null, false));

        var tras = await cliente.GetFromJsonAsync<MetaDetalle>($"/api/v1/metas/{meta.Id}");

        Assert.Equal(30_000m, tras!.MontoActual);
        Assert.Equal(150_000m, tras.MontoFaltante);
        Assert.Equal(15, tras.MesesRestantes);
        Assert.Equal(10_000m, tras.AporteMensualNecesario);

        // Con el compromiso de 10.000 al mes, la meta va justo a tiempo.
        Assert.False(tras.VaAtrasada);
    }

    [Fact]
    public async Task AportarAUnaMetaNoSeContabilizaComoGasto()
    {
        // Es la regla innegociable: ahorrar no empobrece el mes, solo cambia el dinero
        // de sitio. Si el aporte contara como gasto, el informe mensual seria falso.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "metas2@ejemplo.com", "Hogar Metas 2");

        var nomina = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 50_000m);
        var ahorro = await AyudanteFinanciero.CrearCuentaAsync(
            cliente, "Fondo de emergencia", 0m, tipo: "Ahorro");

        var meta = await CrearMetaAsync(cliente, new SolicitudGuardarMeta(
            "Fondo de emergencia", null, 120_000m, "DOP",
            null, "Critica", null, ahorro.Id, null));

        await cliente.PostAsJsonAsync(
            $"/api/v1/metas/{meta.Id}/aportes",
            new SolicitudAportarAMeta(
                nomina.Id, 8_000m, new DateOnly(2026, 9, 20), "Aporte de septiembre", false));

        var pagina = await cliente.GetFromJsonAsync<ResultadoPaginado<MovimientoResumen>>(
            "/api/v1/movimientos?Pagina=1&TamanoPagina=50");

        // Dos asientos, los dos de tipo Transferencia. Ninguno es un gasto.
        Assert.Equal(2, pagina!.Elementos.Count);
        Assert.All(pagina.Elementos, m => Assert.Equal("Transferencia", m.Tipo));

        var nominaDespues = await AyudanteFinanciero.LeerCuentaAsync(cliente, nomina.Id);
        var ahorroDespues = await AyudanteFinanciero.LeerCuentaAsync(cliente, ahorro.Id);

        Assert.Equal(42_000m, nominaDespues.SaldoActual);
        Assert.Equal(8_000m, ahorroDespues.SaldoActual);

        // El patrimonio del hogar no cambió.
        Assert.Equal(50_000m, nominaDespues.SaldoActual + ahorroDespues.SaldoActual);
    }

    [Fact]
    public async Task UnaMetaSinCuentaVinculadaNoAdmiteAportes()
    {
        // Sin cuenta, el acumulado subiria sin que ningun saldo bajara: el hogar creeria
        // tener ese dinero dos veces.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "metas3@ejemplo.com", "Hogar Metas 3");

        var nomina = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 20_000m);

        var meta = await CrearMetaAsync(cliente, new SolicitudGuardarMeta(
            "Cambiar la nevera", null, 40_000m, "DOP", null, "Media", null, null, null));

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/metas/{meta.Id}/aportes",
            new SolicitudAportarAMeta(
                nomina.Id, 5_000m, new DateOnly(2026, 9, 20), null, false));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        var sinTocar = await AyudanteFinanciero.LeerCuentaAsync(cliente, nomina.Id);

        Assert.Equal(20_000m, sinTocar.SaldoActual);
    }

    [Fact]
    public async Task UnaMetaConAportesNoSePuedeEliminar()
    {
        // Esos movimientos existen y apuntan a ella. Para retirarla se cancela.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "metas4@ejemplo.com", "Hogar Metas 4");

        var nomina = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 30_000m);
        var ahorro = await AyudanteFinanciero.CrearCuentaAsync(
            cliente, "Ahorro", 0m, tipo: "Ahorro");

        var meta = await CrearMetaAsync(cliente, new SolicitudGuardarMeta(
            "Laptop nueva", null, 60_000m, "DOP", null, "Media", null, ahorro.Id, null));

        await cliente.PostAsJsonAsync(
            $"/api/v1/metas/{meta.Id}/aportes",
            new SolicitudAportarAMeta(
                nomina.Id, 5_000m, new DateOnly(2026, 9, 20), null, false));

        var borrado = await cliente.DeleteAsync($"/api/v1/metas/{meta.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, borrado.StatusCode);

        // Cancelarla sí funciona, y conserva el historial.
        var cancelada = await cliente.PutAsJsonAsync(
            $"/api/v1/metas/{meta.Id}/estado",
            new SolicitudCambiarEstadoMeta("Cancelada"));

        cancelada.EnsureSuccessStatusCode();

        var detalle = await cancelada.Content.ReadFromJsonAsync<MetaDetalle>();

        Assert.Equal("Cancelada", detalle!.Estado);
        Assert.Equal(5_000m, detalle.MontoActual);
    }

    [Fact]
    public async Task LasMetasDeUnEspacioNoSonVisiblesDesdeOtro()
    {
        var hogarA = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "metasA@ejemplo.com", "Hogar Metas A");
        var hogarB = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "metasB@ejemplo.com", "Hogar Metas B");

        var metaDeA = await CrearMetaAsync(hogarA, new SolicitudGuardarMeta(
            "Secreto de A", null, 10_000m, "DOP", null, "Baja", null, null, null));

        // 404, no 403: revelar que el identificador existe ya seria filtrar informacion.
        var respuesta = await hogarB.GetAsync($"/api/v1/metas/{metaDeA.Id}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);

        var listaDeB = await hogarB.GetFromJsonAsync<List<MetaDetalle>>("/api/v1/metas");

        Assert.Empty(listaDeB!);
    }
}
