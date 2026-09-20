using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;

namespace Rumbo.PruebasIntegracion.Salud;

// Primera prueba de integracion del proyecto. Levanta la API completa en memoria
// (WebApplicationFactory arranca el mismo Program.cs que en produccion) y le hace peticiones
// HTTP reales. No usa base de datos todavia: eso llega en la Fase 2.
//
// Sirve de plantilla para las pruebas de aislamiento entre espacios de la Fase 3, que son
// el requisito de seguridad mas importante del sistema.
public class PruebasEndpointSalud : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _fabrica;

    public PruebasEndpointSalud(WebApplicationFactory<Program> fabrica) => _fabrica = fabrica;

    [Fact]
    public async Task ElEndpointDeSaludRespondeCorrectamente()
    {
        var cliente = _fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/salud");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Activa", cuerpo.GetProperty("estado").GetString());
        Assert.Equal("Rumbo API", cuerpo.GetProperty("aplicacion").GetString());
    }

    [Fact]
    public async Task ElEndpointDeSaludNoExigeAutenticacion()
    {
        // Azure App Service consultara /salud sin credenciales (Fase 10), asi que debe
        // seguir siendo publico aunque en la Fase 3 se anada autenticacion al resto de la API.
        var cliente = _fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/salud");

        Assert.NotEqual(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }
}
