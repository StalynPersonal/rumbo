using System.Net.Http.Json;

using Rumbo.Contratos.Deudas;
using Rumbo.Contratos.Metas;
using Rumbo.Contratos.Notificaciones;
using Rumbo.Contratos.Panel;
using Rumbo.Contratos.Presupuestos;
using Rumbo.PruebasIntegracion.Autenticacion;
using Rumbo.PruebasIntegracion.Financiero;

namespace Rumbo.PruebasIntegracion.Informes;

// El panel es lo primero que se ve al abrir la aplicacion. Su valor esta en que venga
// completo en UNA sola peticion y en que nunca contradiga a las pantallas de detalle.
public class PruebasPanel(FabricaApiDePrueba fabrica) : IClassFixture<FabricaApiDePrueba>
{
    [Fact]
    public async Task ElPanelTraeTodaLaPantallaDeInicioEnUnaSolaPeticion()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "panel1@ejemplo.com", "Hogar Panel 1");

        var panel = await cliente.GetFromJsonAsync<PanelInicio>("/api/v1/panel");

        Assert.NotNull(panel);
        Assert.Equal("Hogar Panel 1", panel!.NombreEspacio);
        Assert.Equal("DOP", panel.Moneda);

        // Todas las secciones vienen presentes, aunque estén vacías: una sección ausente
        // obligaría a la app a distinguir «no hay» de «no llegó».
        Assert.NotNull(panel.Patrimonio);
        Assert.NotNull(panel.MesEnCurso);
        Assert.NotNull(panel.MesAnterior);
        Assert.NotNull(panel.MayoresGastos);
        Assert.NotNull(panel.AlertasPresupuesto);
        Assert.NotNull(panel.ProximosCompromisos);
        Assert.NotNull(panel.Metas);
        Assert.NotNull(panel.Recomendaciones);
    }

    [Fact]
    public async Task ElPatrimonioNetoRestaLasDeudas()
    {
        // Mostrar solo el saldo de las cuentas daria una sensacion de holgura que no se
        // corresponde con la realidad de un hogar endeudado.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "panel2@ejemplo.com", "Hogar Panel 2");

        await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 80_000m);
        await AyudanteFinanciero.CrearCuentaAsync(
            cliente, "Ahorro", 50_000m, tipo: "Ahorro");

        await cliente.PostAsJsonAsync(
            "/api/v1/deudas",
            new SolicitudGuardarDeuda(
                "Préstamo", "Prestamo", null, 200_000m, 120_000m, "DOP",
                null, null, null, null, new DateOnly(2025, 1, 1), null, null, null));

        var panel = await cliente.GetFromJsonAsync<PanelInicio>("/api/v1/panel");

        Assert.Equal(130_000m, panel!.Patrimonio.TotalDisponible);
        Assert.Equal(50_000m, panel.Patrimonio.TotalEnAhorro);
        Assert.Equal(120_000m, panel.Patrimonio.TotalDeudas);
        Assert.Equal(10_000m, panel.Patrimonio.PatrimonioNeto);
        Assert.Equal(2, panel.Patrimonio.CantidadCuentas);
    }

    [Fact]
    public async Task ElPanelNoContradiceALaPantallaDePresupuestos()
    {
        // Si el panel recalculara por su cuenta, tarde o temprano mostraria una cifra
        // distinta y ninguna de las dos seria creible.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "panel3@ejemplo.com", "Hogar Panel 3");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 100_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var inicio = new DateOnly(hoy.Year, hoy.Month, 1);
        var fin = inicio.AddMonths(1).AddDays(-1);

        var creado = await cliente.PostAsJsonAsync(
            "/api/v1/presupuestos",
            new SolicitudGuardarPresupuesto(
                "Mes en curso", "Mensual", inicio, fin, "DOP", null,
                [new LineaPresupuestoSolicitud(categoria.Id, 10_000m, null, null, null)]));

        creado.EnsureSuccessStatusCode();

        // Un gasto que deja la partida al 95 %: nivel Crítico.
        await AyudanteFinanciero.RegistrarAsync(
            cliente, "Gasto", cuenta.Id, categoria.Id, 9_500m, "Se fue de las manos", hoy);

        var panel = await cliente.GetFromJsonAsync<PanelInicio>("/api/v1/panel");
        var detalle = await cliente.GetFromJsonAsync<List<PresupuestoDetalle>>(
            "/api/v1/presupuestos?soloVigente=true");

        var alerta = Assert.Single(panel!.AlertasPresupuesto);
        var linea = detalle![0].Lineas[0];

        Assert.Equal(linea.PorcentajeConsumido, alerta.PorcentajeConsumido);
        Assert.Equal(linea.Nivel, alerta.Nivel);
        Assert.Equal(linea.MontoGastado, alerta.MontoGastado);
    }

    [Fact]
    public async Task LasMetasActivasAparecenConSuAvance()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "panel4@ejemplo.com", "Hogar Panel 4");

        var nomina = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 60_000m);
        var ahorro = await AyudanteFinanciero.CrearCuentaAsync(
            cliente, "Ahorro", 0m, tipo: "Ahorro");

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/metas",
            new SolicitudGuardarMeta(
                "Fondo de emergencia", null, 100_000m, "DOP",
                null, "Critica", null, ahorro.Id, null));

        var meta = (await respuesta.Content.ReadFromJsonAsync<MetaDetalle>())!;

        await cliente.PostAsJsonAsync(
            $"/api/v1/metas/{meta.Id}/aportes",
            new SolicitudAportarAMeta(
                nomina.Id, 25_000m, DateOnly.FromDateTime(DateTime.UtcNow), null, false));

        var panel = await cliente.GetFromJsonAsync<PanelInicio>("/api/v1/panel");

        var avance = Assert.Single(panel!.Metas);

        Assert.Equal("Fondo de emergencia", avance.Nombre);
        Assert.Equal(25_000m, avance.MontoActual);
        Assert.Equal(25m, avance.PorcentajeCompletado);

        // El aporte fue una transferencia, así que el mes no registra ningún gasto.
        Assert.Equal(0m, panel.MesEnCurso.TotalGastos);
    }

    [Fact]
    public async Task LosAvisosSeGeneranSinDuplicarseYSeCuentanEnElPanel()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "panel5@ejemplo.com", "Hogar Panel 5");

        var nomina = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 60_000m);
        var ahorro = await AyudanteFinanciero.CrearCuentaAsync(
            cliente, "Ahorro", 0m, tipo: "Ahorro");

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/metas",
            new SolicitudGuardarMeta(
                "Meta pequeña", null, 5_000m, "DOP", null, "Alta", null, ahorro.Id, null));

        var meta = (await respuesta.Content.ReadFromJsonAsync<MetaDetalle>())!;

        // Se completa la meta: eso genera un aviso de buena noticia.
        await cliente.PostAsJsonAsync(
            $"/api/v1/metas/{meta.Id}/aportes",
            new SolicitudAportarAMeta(
                nomina.Id, 5_000m, DateOnly.FromDateTime(DateTime.UtcNow), null, false));

        var primera = await cliente.PostAsync("/api/v1/notificaciones/generar", null);
        var lote1 = await primera.Content.ReadFromJsonAsync<ResultadoGeneracionAvisos>();

        Assert.Equal(1, lote1!.Generadas);

        // Generar otra vez NO duplica: recibir cinco veces el mismo aviso hace que se dejen
        // de leer todos.
        var segunda = await cliente.PostAsync("/api/v1/notificaciones/generar", null);
        var lote2 = await segunda.Content.ReadFromJsonAsync<ResultadoGeneracionAvisos>();

        Assert.Equal(0, lote2!.Generadas);
        Assert.Equal(1, lote2.SinLeer);

        var panel = await cliente.GetFromJsonAsync<PanelInicio>("/api/v1/panel");

        Assert.Equal(1, panel!.NotificacionesSinLeer);

        // Al marcarlos como leídos, el contador baja.
        await cliente.PutAsJsonAsync("/api/v1/notificaciones/leidas", new { });

        var tras = await cliente.GetFromJsonAsync<PanelInicio>("/api/v1/panel");

        Assert.Equal(0, tras!.NotificacionesSinLeer);
    }

    [Fact]
    public async Task ElPanelDeUnEspacioNoMuestraDatosDeOtro()
    {
        var hogarA = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "panelA@ejemplo.com", "Hogar Panel A");
        var hogarB = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "panelB@ejemplo.com", "Hogar Panel B");

        await AyudanteFinanciero.CrearCuentaAsync(hogarA, "Cuenta de A", 500_000m);

        var panelDeB = await hogarB.GetFromJsonAsync<PanelInicio>("/api/v1/panel");

        Assert.Equal("Hogar Panel B", panelDeB!.NombreEspacio);
        Assert.Equal(0m, panelDeB.Patrimonio.TotalDisponible);
        Assert.Equal(0, panelDeB.Patrimonio.CantidadCuentas);
    }
}
