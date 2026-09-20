using System.Net;
using System.Net.Http.Json;

using Rumbo.Contratos.Invitaciones;

namespace Rumbo.PruebasIntegracion.Autenticacion;

// Aislamiento entre espacios comprobado a traves de la API REAL, no del DbContext.
//
// Las pruebas de Persistencia/PruebasAislamientoEspacio verifican los filtros de EF Core.
// Estas verifican la CADENA COMPLETA: token, middleware de resolucion, autorizacion por
// permisos, filtros y controladores. Es donde de verdad se demuestra que un hogar no ve al
// otro, porque prueba el sistema tal como se despliega.
public class PruebasAislamientoEnLaApi(FabricaApiDePrueba fabrica)
    : IClassFixture<FabricaApiDePrueba>
{
    [Fact]
    public async Task UnEspacioNoVeLasInvitacionesDeOtro()
    {
        var hogarA = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "duenoa@ejemplo.com", "Hogar A");

        var hogarB = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "duenob@ejemplo.com", "Hogar B");

        // El hogar B invita a alguien.
        var clienteB = AyudanteApi.ClienteConToken(fabrica, hogarB.TokenAcceso);

        var creada = await clienteB.PostAsJsonAsync(
            "/api/v1/invitaciones", new SolicitudInvitarMiembro("invitadob@ejemplo.com", "Miembro"));

        Assert.Equal(HttpStatusCode.Created, creada.StatusCode);

        // El hogar A no debe ver ni rastro de esa invitacion.
        var clienteA = AyudanteApi.ClienteConToken(fabrica, hogarA.TokenAcceso);

        var listaA = await clienteA.GetFromJsonAsync<List<InvitacionResumen>>("/api/v1/invitaciones");

        Assert.NotNull(listaA);
        Assert.DoesNotContain(listaA!, i => i.Correo == "invitadob@ejemplo.com");
    }

    [Fact]
    public async Task NoSePuedeAnularUnaInvitacionDeOtroEspacio()
    {
        var hogarA = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "duenoa2@ejemplo.com", "Hogar A2");

        var hogarB = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "duenob2@ejemplo.com", "Hogar B2");

        var clienteB = AyudanteApi.ClienteConToken(fabrica, hogarB.TokenAcceso);

        var invitacionB = (await (await clienteB.PostAsJsonAsync(
            "/api/v1/invitaciones", new SolicitudInvitarMiembro("victima@ejemplo.com", "Miembro")))
            .Content.ReadFromJsonAsync<InvitacionCreada>())!;

        // El hogar A conoce el identificador y trata de anularla. Debe recibir 404, NO 403:
        // un 403 confirmaria que la invitacion existe, y eso ya es informacion que no
        // corresponde dar.
        var clienteA = AyudanteApi.ClienteConToken(fabrica, hogarA.TokenAcceso);

        var respuesta = await clienteA.DeleteAsync($"/api/v1/invitaciones/{invitacionB.Id}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task ElAdministradorDePlataformaNoPuedeOperarDentroDeUnEspacio()
    {
        // La garantia de privacidad del modelo: quien administra la plataforma reparte
        // invitaciones, pero no entra en las finanzas de nadie.
        await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "duenoc@ejemplo.com", "Hogar C");

        var admin = await AyudanteApi.IniciarSesionAsync(
            fabrica.CreateClient(),
            FabricaApiDePrueba.CorreoAdministrador,
            FabricaApiDePrueba.ClaveAdministrador);

        var clienteAdmin = AyudanteApi.ClienteConToken(fabrica, admin.TokenAcceso);

        var respuesta = await clienteAdmin.GetAsync("/api/v1/invitaciones");

        // Sin membresía en ningún espacio no tiene ningún permiso sobre datos de hogares.
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task UnPropietarioNoPuedeEntrarEnLaAdministracionDePlataforma()
    {
        var propietario = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "duenod@ejemplo.com", "Hogar D");

        var cliente = AyudanteApi.ClienteConToken(fabrica, propietario.TokenAcceso);

        var respuesta = await cliente.GetAsync("/api/v1/administracion/invitaciones");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task SinTokenNoSeAccedeANingunEndpointDeDatos()
    {
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/api/v1/invitaciones");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task UnTokenManipuladoEsRechazado()
    {
        // Cambiar un caracter del token invalida la firma. Es lo que impide que alguien
        // edite el claim del espacio para colarse en otro hogar.
        var propietario = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "duenoe@ejemplo.com", "Hogar E");

        var tokenManipulado = propietario.TokenAcceso[..^4] + "AAAA";

        var cliente = AyudanteApi.ClienteConToken(fabrica, tokenManipulado);

        var respuesta = await cliente.GetAsync("/api/v1/invitaciones");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }
}
