using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Hosting;

using Rumbo.Contratos.Autenticacion;
using Rumbo.PruebasIntegracion.Autenticacion;

namespace Rumbo.PruebasIntegracion.Seguridad;

/// <summary>
/// Fabrica con cupos bajos, para comprobar que el limitador corta de verdad.
/// </summary>
/// <remarks>
/// La fabrica general sube los cupos a un numero enorme para que las pruebas no se estorben
/// entre si. Aqui se hace lo contrario: dos peticiones por minuto, y se comprueba que la
/// tercera se rechaza. Sin esta prueba, el limitador podria estar mal conectado y nadie se
/// enteraria hasta que alguien lo atacara en produccion.
/// </remarks>
public class FabricaConCupoBajo : FabricaApiDePrueba
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder constructor)
    {
        base.ConfigureWebHost(constructor);

        // Se sobrescriben los cupos altos que pone la fabrica base.
        constructor.UseSetting("LimitePeticiones:PorMinutoAutenticacion", "2");
        constructor.UseSetting("LimitePeticiones:PorMinutoGeneral", "3");
    }
}

// El limitador protege contra fuerza bruta sobre las claves y contra usar «olvide-clave»
// para bombardear a alguien con correos.
public class PruebasLimiteDePeticiones(FabricaConCupoBajo fabrica)
    : IClassFixture<FabricaConCupoBajo>
{
    [Fact]
    public async Task TrasVariosIntentosFallidosDeSesionSeDevuelve429()
    {
        var cliente = fabrica.CreateClient();

        var solicitud = new SolicitudIniciarSesion("nadie@ejemplo.com", "ClaveIncorrecta1!");

        // Los dos primeros llegan al servicio y fallan con 401: credenciales inválidas.
        for (var intento = 0; intento < 2; intento++)
        {
            var respuesta = await cliente.PostAsJsonAsync(
                "/api/v1/autenticacion/iniciar-sesion", solicitud);

            Assert.NotEqual(HttpStatusCode.TooManyRequests, respuesta.StatusCode);
        }

        // El tercero ya no llega: lo corta el limitador.
        var cortada = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacion/iniciar-sesion", solicitud);

        Assert.Equal(HttpStatusCode.TooManyRequests, cortada.StatusCode);
    }

    [Fact]
    public async Task LaRespuesta429DiceCuantoHayQueEsperar()
    {
        // Sin esa cabecera, un cliente honesto solo puede reintentar a ciegas, que es justo
        // lo que empeora la situacion.
        var cliente = fabrica.CreateClient();

        HttpResponseMessage? cortada = null;

        for (var intento = 0; intento < 6 && cortada is null; intento++)
        {
            var respuesta = await cliente.PostAsJsonAsync(
                "/api/v1/autenticacion/olvide-clave",
                new SolicitudOlvideClave("alguien@ejemplo.com"));

            if (respuesta.StatusCode == HttpStatusCode.TooManyRequests)
            {
                cortada = respuesta;
            }
        }

        Assert.NotNull(cortada);
        Assert.NotNull(cortada!.Headers.RetryAfter);

        var cuerpo = await cortada.Content.ReadAsStringAsync();

        // Y el error sale en el mismo formato que todos los demas de la API.
        Assert.Equal("application/problem+json", cortada.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Demasiadas peticiones", cuerpo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ElEndpointDeSaludNoTieneLimite()
    {
        // Azure lo consulta cada pocos segundos como comprobacion de vida. Si el limitador
        // lo cortara, la plataforma creeria que la API esta caida y la reiniciaria.
        var cliente = fabrica.CreateClient();

        for (var intento = 0; intento < 10; intento++)
        {
            var respuesta = await cliente.GetAsync("/salud");

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        }
    }
}
