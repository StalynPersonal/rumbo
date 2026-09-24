using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Hosting;

using Rumbo.Contratos.Aplicacion;
using Rumbo.PruebasIntegracion.Autenticacion;

namespace Rumbo.PruebasIntegracion.Salud;

/// <summary>Fabrica con unas versiones concretas publicadas.</summary>
public class FabricaConVersiones : FabricaApiDePrueba
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder constructor)
    {
        base.ConfigureWebHost(constructor);

        constructor.UseSetting("VersionApp:VersionMinima", "1.2.0");
        constructor.UseSetting("VersionApp:VersionRecomendada", "1.5.0");
        constructor.UseSetting("VersionApp:UrlDescarga", "https://ejemplo.com/rumbo.apk");
    }
}

// El APK se instala a mano, sin tienda, asi que nadie avisa de que hay una version nueva.
// Este endpoint es ese aviso.
public class PruebasVersionDeLaApp(FabricaConVersiones fabrica)
    : IClassFixture<FabricaConVersiones>
{
    /// <summary>Pregunta por una version concreta.</summary>
    /// <param name="cliente">Cliente HTTP.</param>
    /// <param name="version">Version instalada.</param>
    /// <returns>La respuesta del servidor.</returns>
    private static async Task<VersionAplicacion> ConsultarAsync(
        HttpClient cliente,
        string? version)
    {
        var ruta = version is null
            ? "/api/v1/app/version"
            : $"/api/v1/app/version?version={version}";

        return (await cliente.GetFromJsonAsync<VersionAplicacion>(ruta))!;
    }

    [Fact]
    public async Task NoExigeIniciarSesion()
    {
        // La aplicacion necesita preguntarlo ANTES de que nadie entre: si la version ya no
        // sirve, lo util es decirlo en la pantalla de acceso y no despues de que la persona
        // se pelee con un error raro.
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/api/v1/app/version?version=1.5.0");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task UnaVersionAlDiaNoPideNada()
    {
        var resultado = await ConsultarAsync(fabrica.CreateClient(), "1.5.0");

        Assert.False(resultado.HayActualizacion);
        Assert.False(resultado.ActualizacionObligatoria);
        Assert.Null(resultado.Mensaje);
    }

    [Fact]
    public async Task UnaVersionAtrasadaPeroValidaSoloAvisa()
    {
        // Entre la minima y la recomendada: funciona, pero conviene actualizar.
        var resultado = await ConsultarAsync(fabrica.CreateClient(), "1.3.0");

        Assert.True(resultado.HayActualizacion);
        Assert.False(resultado.ActualizacionObligatoria);
        Assert.NotNull(resultado.Mensaje);
    }

    [Fact]
    public async Task UnaVersionPorDebajoDeLaMinimaSeMarcaComoObligatoria()
    {
        var resultado = await ConsultarAsync(fabrica.CreateClient(), "1.0.0");

        Assert.True(resultado.ActualizacionObligatoria);
        Assert.Equal("https://ejemplo.com/rumbo.apk", resultado.UrlDescarga);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-es-una-version")]
    public async Task SinVersionEntendibleNoSeBloqueaANadie(string? version)
    {
        // Una aplicacion tan antigua que ni manda su version se avisa, pero no se bloquea a
        // ciegas: dejar a alguien sin ver sus finanzas es peor que la version vieja.
        var resultado = await ConsultarAsync(fabrica.CreateClient(), version);

        Assert.False(resultado.ActualizacionObligatoria);
        Assert.Equal("1.2.0", resultado.VersionMinima);
        Assert.Equal("1.5.0", resultado.VersionRecomendada);
    }
}
