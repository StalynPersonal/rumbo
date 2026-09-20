using System.Net;
using System.Net.Http.Json;

using Rumbo.Contratos.Administracion;
using Rumbo.Contratos.Comun;
using Rumbo.Contratos.Usuarios;
using Rumbo.PruebasIntegracion.Autenticacion;
using Rumbo.PruebasIntegracion.Financiero;

namespace Rumbo.PruebasIntegracion.Administracion;

// Gestion de la plataforma.
//
// Lo que mas importa verificar: el administrador gestiona altas y estados, pero NUNCA ve
// datos financieros de ningun hogar.
public class PruebasAdministracion(FabricaApiDePrueba fabrica) : IClassFixture<FabricaApiDePrueba>
{
    private async Task<HttpClient> ClienteAdministradorAsync()
    {
        var admin = await AyudanteApi.IniciarSesionAsync(
            fabrica.CreateClient(),
            FabricaApiDePrueba.CorreoAdministrador,
            FabricaApiDePrueba.ClaveAdministrador);

        return AyudanteApi.ClienteConToken(fabrica, admin.TokenAcceso);
    }

    [Fact]
    public async Task ElAdministradorVeLosEspaciosSinDatosFinancieros()
    {
        var hogar = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "admin1@ejemplo.com", "Hogar Visible");

        // Se registra dinero para comprobar que NO aparece en la vista del administrador.
        await AyudanteFinanciero.CrearCuentaAsync(hogar, "Nómina", 123_456m);

        var cliente = await ClienteAdministradorAsync();

        var respuesta = await cliente.GetAsync("/api/v1/administracion/espacios?busqueda=Hogar Visible");
        respuesta.EnsureSuccessStatusCode();

        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        // El saldo no aparece por ningún lado de la respuesta.
        Assert.DoesNotContain("123456", cuerpo, StringComparison.Ordinal);
        Assert.DoesNotContain("123456.00", cuerpo, StringComparison.Ordinal);

        var pagina = await respuesta.Content
            .ReadFromJsonAsync<ResultadoPaginado<EspacioAdminResumen>>();

        var espacio = pagina!.Elementos.First(e => e.Nombre == "Hogar Visible");

        Assert.Equal("Activo", espacio.Estado);
        Assert.Equal(1, espacio.CantidadMiembros);
        Assert.Equal("admin1@ejemplo.com", espacio.CorreoPropietario);
    }

    [Fact]
    public async Task SuspenderUnEspacioCortaElAccesoDeSusMiembros()
    {
        // Antes de esto, EstadoEspacio.Suspendido existía en el modelo pero no hacía nada:
        // el middleware solo comprobaba la membresía, no el estado del espacio.
        var hogar = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "admin2@ejemplo.com", "Hogar Suspendible");

        // Antes de suspender, entra sin problema.
        Assert.Equal(HttpStatusCode.OK, (await hogar.GetAsync("/api/v1/espacios/actual")).StatusCode);

        var cliente = await ClienteAdministradorAsync();

        var pagina = await cliente.GetFromJsonAsync<ResultadoPaginado<EspacioAdminResumen>>(
            "/api/v1/administracion/espacios?busqueda=Hogar Suspendible");

        var espacioId = pagina!.Elementos.First().Id;

        var suspension = await cliente.PutAsJsonAsync(
            $"/api/v1/administracion/espacios/{espacioId}/estado",
            new SolicitudCambiarEstadoEspacio("Suspendido", "Prueba automática"));

        suspension.EnsureSuccessStatusCode();

        // Su token sigue siendo válido, pero el espacio ya no lo está.
        var despues = await hogar.GetAsync("/api/v1/espacios/actual");

        Assert.Equal(HttpStatusCode.Forbidden, despues.StatusCode);
    }

    [Fact]
    public async Task ReactivarUnEspacioDevuelveElAcceso()
    {
        var hogar = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "admin3@ejemplo.com", "Hogar Reactivable");

        var cliente = await ClienteAdministradorAsync();

        var pagina = await cliente.GetFromJsonAsync<ResultadoPaginado<EspacioAdminResumen>>(
            "/api/v1/administracion/espacios?busqueda=Hogar Reactivable");

        var espacioId = pagina!.Elementos.First().Id;

        await cliente.PutAsJsonAsync(
            $"/api/v1/administracion/espacios/{espacioId}/estado",
            new SolicitudCambiarEstadoEspacio("Suspendido", null));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await hogar.GetAsync("/api/v1/espacios/actual")).StatusCode);

        await cliente.PutAsJsonAsync(
            $"/api/v1/administracion/espacios/{espacioId}/estado",
            new SolicitudCambiarEstadoEspacio("Activo", "Ya se resolvió"));

        // Nada se perdió: el acceso vuelve y los datos siguen ahí.
        Assert.Equal(
            HttpStatusCode.OK,
            (await hogar.GetAsync("/api/v1/espacios/actual")).StatusCode);
    }

    [Fact]
    public async Task LasMetricasSonRecuentosYNoImportes()
    {
        var cliente = await ClienteAdministradorAsync();

        var metricas = await cliente.GetFromJsonAsync<MetricasPlataforma>(
            "/api/v1/administracion/metricas");

        Assert.NotNull(metricas);
        Assert.True(metricas!.TotalUsuarios >= 1);
        Assert.True(metricas.TotalEspacios >= 0);
    }

    [Fact]
    public async Task UnAdministradorNoPuedeDeshabilitarseASiMismo()
    {
        // Si fuera el único, la plataforma se quedaría sin nadie capaz de emitir
        // invitaciones y no habría forma de recuperarla sin tocar la base de datos.
        var cliente = await ClienteAdministradorAsync();

        var usuarios = await cliente.GetFromJsonAsync<ResultadoPaginado<UsuarioAdminResumen>>(
            $"/api/v1/administracion/usuarios?busqueda={FabricaApiDePrueba.CorreoAdministrador}");

        var administrador = usuarios!.Elementos.First();

        var respuesta = await cliente.PutAsync(
            $"/api/v1/administracion/usuarios/{administrador.Id}/estado?activo=false",
            content: null);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task UnPropietarioNoPuedeEntrarEnLaAdministracion()
    {
        var hogar = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "admin4@ejemplo.com", "Hogar Sin Permisos");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await hogar.GetAsync("/api/v1/administracion/espacios")).StatusCode);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await hogar.GetAsync("/api/v1/administracion/metricas")).StatusCode);
    }
}
