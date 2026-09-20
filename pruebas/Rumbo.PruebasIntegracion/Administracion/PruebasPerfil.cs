using System.Net;
using System.Net.Http.Json;

using Rumbo.Contratos.Usuarios;
using Rumbo.PruebasIntegracion.Autenticacion;
using Rumbo.PruebasIntegracion.Financiero;

namespace Rumbo.PruebasIntegracion.Administracion;

// Perfil propio. Solo el propio: no hay ruta que reciba un identificador de usuario.
public class PruebasPerfil(FabricaApiDePrueba fabrica) : IClassFixture<FabricaApiDePrueba>
{
    [Fact]
    public async Task CadaPersonaVeSuPropioPerfil()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "perfil1@ejemplo.com", "Hogar Perfil 1");

        var perfil = await cliente.GetFromJsonAsync<PerfilUsuario>("/api/v1/usuarios/yo");

        Assert.NotNull(perfil);
        Assert.Equal("perfil1@ejemplo.com", perfil!.Correo);
        Assert.False(perfil.EsAdministradorPlataforma);
        Assert.Equal(1, perfil.CantidadEspacios);
    }

    [Fact]
    public async Task SePuedeCambiarElNombreYLaCultura()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "perfil2@ejemplo.com", "Hogar Perfil 2");

        var respuesta = await cliente.PutAsJsonAsync(
            "/api/v1/usuarios/yo",
            new SolicitudActualizarPerfil("María Fernández", "es-DO"));

        respuesta.EnsureSuccessStatusCode();

        var perfil = (await respuesta.Content.ReadFromJsonAsync<PerfilUsuario>())!;

        Assert.Equal("María Fernández", perfil.NombreCompleto);
        Assert.Equal("es-DO", perfil.CulturaPreferida);
    }

    [Fact]
    public async Task UnaCulturaInventadaSeRechaza()
    {
        // Una cultura inexistente haría fallar el formateo de importes y fechas en la
        // aplicación móvil, y el error aparecería lejos de su causa.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "perfil3@ejemplo.com", "Hogar Perfil 3");

        var respuesta = await cliente.PutAsJsonAsync(
            "/api/v1/usuarios/yo",
            new SolicitudActualizarPerfil("Alguien", "xx-ZZ-inventada"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task ElNombreNoPuedeQuedarVacio()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "perfil4@ejemplo.com", "Hogar Perfil 4");

        var respuesta = await cliente.PutAsJsonAsync(
            "/api/v1/usuarios/yo", new SolicitudActualizarPerfil("   ", "es-DO"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task SinTokenNoHayPerfil()
    {
        var respuesta = await fabrica.CreateClient().GetAsync("/api/v1/usuarios/yo");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }
}
