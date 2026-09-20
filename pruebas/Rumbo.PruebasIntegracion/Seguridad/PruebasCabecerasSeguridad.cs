using Rumbo.PruebasIntegracion.Autenticacion;
using Rumbo.PruebasIntegracion.Financiero;

namespace Rumbo.PruebasIntegracion.Seguridad;

// Las cabeceras de seguridad son de las cosas que se ponen una vez y nadie vuelve a mirar.
// Estas pruebas se dan cuenta si alguna desaparece.
public class PruebasCabecerasSeguridad(FabricaApiDePrueba fabrica)
    : IClassFixture<FabricaApiDePrueba>
{
    [Theory]
    [InlineData("X-Content-Type-Options", "nosniff")]
    [InlineData("X-Frame-Options", "DENY")]
    [InlineData("Referrer-Policy", "no-referrer")]
    public async Task TodaRespuestaLlevaLasCabecerasDeSeguridad(
        string cabecera,
        string valorEsperado)
    {
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/salud");

        Assert.True(
            respuesta.Headers.TryGetValues(cabecera, out var valores),
            $"Falta la cabecera {cabecera}.");

        Assert.Equal(valorEsperado, valores!.First());
    }

    [Fact]
    public async Task LasRespuestasDeErrorTambienLlevanLasCabeceras()
    {
        // Van antes del manejador de excepciones en la tuberia justamente por esto: una
        // respuesta de error es una respuesta como cualquier otra.
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/api/v1/cuentas");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.True(respuesta.Headers.Contains("X-Content-Type-Options"));
    }

    [Fact]
    public async Task LosDatosFinancierosNoSeGuardanEnNingunaCache()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "cabeceras1@ejemplo.com", "Hogar Cabeceras 1");

        var respuesta = await cliente.GetAsync("/api/v1/cuentas");

        respuesta.EnsureSuccessStatusCode();

        Assert.True(respuesta.Headers.CacheControl!.NoStore);
        Assert.True(respuesta.Headers.CacheControl.NoCache);
    }

    [Fact]
    public async Task NoSeAnunciaConQueEstaHechoElServidor()
    {
        // No es seguridad de verdad, pero tampoco hay razon para regalar el dato.
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/salud");

        Assert.False(respuesta.Headers.Contains("Server"));
        Assert.False(respuesta.Headers.Contains("X-Powered-By"));
    }

    [Fact]
    public async Task LaPoliticaDeContenidoProhibeCargarCualquierCosa()
    {
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/salud");

        var politica = respuesta.Headers.GetValues("Content-Security-Policy").First();

        Assert.Contains("default-src 'none'", politica, StringComparison.Ordinal);
        Assert.Contains("frame-ancestors 'none'", politica, StringComparison.Ordinal);
    }
}
