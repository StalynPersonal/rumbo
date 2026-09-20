using System.Net;
using System.Net.Http.Json;

using Rumbo.Contratos.Autenticacion;

namespace Rumbo.PruebasIntegracion.Autenticacion;

// Ciclo de vida de la sesion: acceso, rotacion de tokens y deteccion de robo.
public class PruebasSesion(FabricaApiDePrueba fabrica) : IClassFixture<FabricaApiDePrueba>
{
    [Fact]
    public async Task UnaContrasenaIncorrectaNoRevelaSiLaCuentaExiste()
    {
        var cliente = fabrica.CreateClient();

        // Cuenta inexistente.
        var inexistente = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacion/iniciar-sesion",
            new SolicitudIniciarSesion("nadie@ejemplo.com", "LoQueSea123456"));

        // Cuenta real con contrasena incorrecta.
        var claveMala = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacion/iniciar-sesion",
            new SolicitudIniciarSesion(FabricaApiDePrueba.CorreoAdministrador, "ClaveIncorrecta123"));

        // MISMO codigo en ambos casos. Si difirieran, probar direcciones permitiria
        // averiguar quien tiene cuenta en Rumbo.
        Assert.Equal(HttpStatusCode.Unauthorized, inexistente.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, claveMala.StatusCode);

        var cuerpoInexistente = await inexistente.Content.ReadAsStringAsync();
        var cuerpoClaveMala = await claveMala.Content.ReadAsStringAsync();

        // Y el mismo mensaje, no solo el mismo codigo.
        Assert.Contains("correo o la contraseña", cuerpoInexistente);
        Assert.Contains("correo o la contraseña", cuerpoClaveMala);
    }

    [Fact]
    public async Task ElTokenDeRenovacionRotaEnCadaUso()
    {
        var propietario = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "sesion1@ejemplo.com", "Hogar Sesión 1");

        var cliente = fabrica.CreateClient();

        var renovada = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacion/renovar",
            new SolicitudRenovar(propietario.TokenRenovacion));

        renovada.EnsureSuccessStatusCode();

        var nueva = (await renovada.Content.ReadFromJsonAsync<RespuestaAutenticacion>())!;

        Assert.NotEqual(propietario.TokenRenovacion, nueva.TokenRenovacion);
        Assert.NotEmpty(nueva.TokenAcceso);
    }

    [Fact]
    public async Task ReutilizarUnTokenYaUsadoCierraTodasLasSesiones()
    {
        // El escenario de robo: el atacante usa el token interceptado y despues la persona
        // legitima intenta renovar (o al reves). Como no hay forma de saber cual es cual,
        // se cierran las dos sesiones.
        var propietario = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "sesion2@ejemplo.com", "Hogar Sesión 2");

        var cliente = fabrica.CreateClient();

        var primeraRenovacion = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacion/renovar",
            new SolicitudRenovar(propietario.TokenRenovacion));

        primeraRenovacion.EnsureSuccessStatusCode();

        var tokenLegitimo = (await primeraRenovacion.Content
            .ReadFromJsonAsync<RespuestaAutenticacion>())!.TokenRenovacion;

        // Se reutiliza el token viejo: senal inequivoca de que hay dos copias circulando.
        var reutilizacion = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacion/renovar",
            new SolicitudRenovar(propietario.TokenRenovacion));

        Assert.Equal(HttpStatusCode.Unauthorized, reutilizacion.StatusCode);

        // Y el token que SI era legitimo tambien queda revocado: se corta la familia entera.
        var trasLaAlarma = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacion/renovar",
            new SolicitudRenovar(tokenLegitimo));

        Assert.Equal(HttpStatusCode.Unauthorized, trasLaAlarma.StatusCode);
    }

    [Fact]
    public async Task CerrarSesionInvalidaElTokenDeRenovacion()
    {
        var propietario = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "sesion3@ejemplo.com", "Hogar Sesión 3");

        var cliente = AyudanteApi.ClienteConToken(fabrica, propietario.TokenAcceso);

        var cierre = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacion/cerrar-sesion",
            new SolicitudCerrarSesion(propietario.TokenRenovacion));

        Assert.Equal(HttpStatusCode.NoContent, cierre.StatusCode);

        var renovacion = await fabrica.CreateClient().PostAsJsonAsync(
            "/api/v1/autenticacion/renovar",
            new SolicitudRenovar(propietario.TokenRenovacion));

        Assert.Equal(HttpStatusCode.Unauthorized, renovacion.StatusCode);
    }

    [Fact]
    public async Task CambiarLaContrasenaCierraTodasLasSesionesAbiertas()
    {
        // Quien cambia su contrasena suele hacerlo porque sospecha que alguien la conoce.
        // Dejar vivas las demas sesiones anularia el sentido del cambio.
        var propietario = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "sesion4@ejemplo.com", "Hogar Sesión 4", "ClaveOriginal2026");

        var cliente = AyudanteApi.ClienteConToken(fabrica, propietario.TokenAcceso);

        var cambio = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacion/cambiar-clave",
            new SolicitudCambiarClave("ClaveOriginal2026", "ClaveNuevaSegura2026"));

        Assert.Equal(HttpStatusCode.NoContent, cambio.StatusCode);

        var renovacion = await fabrica.CreateClient().PostAsJsonAsync(
            "/api/v1/autenticacion/renovar",
            new SolicitudRenovar(propietario.TokenRenovacion));

        Assert.Equal(HttpStatusCode.Unauthorized, renovacion.StatusCode);
    }

    [Fact]
    public async Task PedirRestablecerLaClaveRespondeIgualExistaONoLaCuenta()
    {
        var cliente = fabrica.CreateClient();

        var existente = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacion/olvide-clave",
            new SolicitudOlvideClave(FabricaApiDePrueba.CorreoAdministrador));

        var inexistente = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacion/olvide-clave",
            new SolicitudOlvideClave("nadie.de.nadie@ejemplo.com"));

        Assert.Equal(HttpStatusCode.NoContent, existente.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, inexistente.StatusCode);
    }
}
