using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Rumbo.PruebasIntegracion.Autenticacion;

namespace Rumbo.PruebasIntegracion.Salud;

// /salud y /salud/preparada responden preguntas distintas, y por eso son dos endpoints.
//
// /salud dice si el proceso vive. Azure lo usa como comprobacion de vida: si fallara porque
// la base de datos esta caida, la plataforma reiniciaria la aplicacion, que no arregla nada
// y encima tira las sesiones.
//
// /salud/preparada ademas consulta la base. Sirve para diagnosticar y para decidir si una
// instancia recien desplegada puede recibir trafico.
public class PruebasEndpointPreparada(FabricaApiDePrueba fabrica)
    : IClassFixture<FabricaApiDePrueba>
{
    [Fact]
    public async Task ConLaBaseDeDatosAccesibleDevuelvePreparada()
    {
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/salud/preparada");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Preparada", cuerpo.GetProperty("estado").GetString());
        Assert.Equal("Accesible", cuerpo.GetProperty("baseDeDatos").GetString());
    }

    [Fact]
    public async Task NoExigeAutenticacion()
    {
        // El despliegue la consulta antes de que exista ninguna sesion, y la plataforma no
        // tiene credenciales. Exigir token la haria inutil.
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/salud/preparada");

        Assert.NotEqual(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task NoRevelaDatosDelServidor()
    {
        // La respuesta solo dice si se llega o no a la base. Nunca el nombre del servidor,
        // el usuario ni el mensaje de la excepcion: una cadena de conexion mal formada
        // puede llevar dentro cualquiera de las tres cosas.
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/salud/preparada");
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.DoesNotContain("Server=", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("localhost", cuerpo, StringComparison.OrdinalIgnoreCase);
    }
}
