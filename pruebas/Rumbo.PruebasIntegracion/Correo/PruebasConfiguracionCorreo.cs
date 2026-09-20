using System.Net;
using System.Net.Http.Json;

using Rumbo.Contratos.Correo;
using Rumbo.PruebasIntegracion.Autenticacion;

namespace Rumbo.PruebasIntegracion.Correo;

// Configuracion de los servidores SMTP: uno de plataforma y uno por espacio.
//
// Lo que mas importa verificar no es que se guarde bien, sino que la contrasena NO salga
// nunca por la API. Es la credencial del correo personal de alguien.
public class PruebasConfiguracionCorreo(FabricaApiDePrueba fabrica)
    : IClassFixture<FabricaApiDePrueba>
{
    private const string ClaveSmtpDePrueba = "contrasena-de-aplicacion-secreta-123";

    private async Task<HttpClient> ClienteAdministradorAsync()
    {
        var admin = await AyudanteApi.IniciarSesionAsync(
            fabrica.CreateClient(),
            FabricaApiDePrueba.CorreoAdministrador,
            FabricaApiDePrueba.ClaveAdministrador);

        return AyudanteApi.ClienteConToken(fabrica, admin.TokenAcceso);
    }

    [Fact]
    public async Task LaContrasenaSmtpNuncaSeDevuelvePorLaApi()
    {
        // LA PRUEBA MAS IMPORTANTE DE ESTE MODULO. Si la API devolviera la contrasena,
        // cualquiera con acceso al panel se llevaria la cuenta de correo, no solo los datos
        // financieros.
        var cliente = await ClienteAdministradorAsync();

        var guardado = await cliente.PutAsJsonAsync(
            "/api/v1/administracion/correo",
            new SolicitudGuardarCorreo(
                "smtp.ejemplo.com", 587, false, "usuario@ejemplo.com",
                ClaveSmtpDePrueba, "usuario@ejemplo.com", "Rumbo", true));

        guardado.EnsureSuccessStatusCode();

        // Se revisa el JSON en crudo y no el objeto deserializado: si la contrasena se
        // colara en un campo que el DTO no declara, el objeto no la mostraria pero el JSON
        // si la llevaria.
        var cuerpoGuardado = await guardado.Content.ReadAsStringAsync();
        Assert.DoesNotContain(ClaveSmtpDePrueba, cuerpoGuardado, StringComparison.Ordinal);

        var leido = await cliente.GetAsync("/api/v1/administracion/correo");
        var cuerpoLeido = await leido.Content.ReadAsStringAsync();

        Assert.DoesNotContain(ClaveSmtpDePrueba, cuerpoLeido, StringComparison.Ordinal);

        // Pero si debe informar de que hay una guardada.
        var dto = (await leido.Content.ReadFromJsonAsync<ConfiguracionCorreoDto>())!;
        Assert.True(dto.ClaveConfigurada);
    }

    [Fact]
    public async Task GuardarSinContrasenaConservaLaQueYaHabia()
    {
        // Para cambiar el puerto no hay que volver a escribir la contrasena, y la API no la
        // devuelve para poder reenviarla: si no se conservara, editar cualquier campo la
        // borraria.
        var cliente = await ClienteAdministradorAsync();

        await cliente.PutAsJsonAsync(
            "/api/v1/administracion/correo",
            new SolicitudGuardarCorreo(
                "smtp.ejemplo.com", 587, false, "usuario@ejemplo.com",
                ClaveSmtpDePrueba, "usuario@ejemplo.com", "Rumbo", true));

        var segundo = await cliente.PutAsJsonAsync(
            "/api/v1/administracion/correo",
            new SolicitudGuardarCorreo(
                "smtp.ejemplo.com", 465, true, "usuario@ejemplo.com",
                null, "usuario@ejemplo.com", "Rumbo", true));

        var dto = (await segundo.Content.ReadFromJsonAsync<ConfiguracionCorreoDto>())!;

        Assert.Equal(465, dto.Puerto);
        Assert.True(dto.UsarSslDirecto);
        Assert.True(dto.ClaveConfigurada);
    }

    [Fact]
    public async Task NoSePuedeActivarSinServidorNiRemitente()
    {
        var cliente = await ClienteAdministradorAsync();

        var respuesta = await cliente.PutAsJsonAsync(
            "/api/v1/administracion/correo",
            new SolicitudGuardarCorreo(null, 587, false, null, null, null, "Rumbo", true));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task SePuedeGuardarIncompletaSiQuedaDesactivada()
    {
        // Permite ir completando la configuracion a ratos sin que la aplicacion intente
        // enviar con datos a medias.
        var cliente = await ClienteAdministradorAsync();

        var respuesta = await cliente.PutAsJsonAsync(
            "/api/v1/administracion/correo",
            new SolicitudGuardarCorreo("smtp.ejemplo.com", 587, false, null, null, null, null, false));

        respuesta.EnsureSuccessStatusCode();

        var dto = (await respuesta.Content.ReadFromJsonAsync<ConfiguracionCorreoDto>())!;

        Assert.False(dto.Activa);
        Assert.Equal("smtp.ejemplo.com", dto.Host);
    }

    [Fact]
    public async Task ElPropietarioConfiguraElCorreoDeSuEspacio()
    {
        var propietario = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "correo.dueno@ejemplo.com", "Hogar con correo");

        var cliente = AyudanteApi.ClienteConToken(fabrica, propietario.TokenAcceso);

        var guardado = await cliente.PutAsJsonAsync(
            "/api/v1/espacios/actual/correo",
            new SolicitudGuardarCorreo(
                "smtp.hogar.com", 587, false, "juan@hogar.com",
                ClaveSmtpDePrueba, "juan@hogar.com", "Juan", true));

        guardado.EnsureSuccessStatusCode();

        var cuerpo = await guardado.Content.ReadAsStringAsync();
        Assert.DoesNotContain(ClaveSmtpDePrueba, cuerpo, StringComparison.Ordinal);

        var dto = (await guardado.Content.ReadFromJsonAsync<ConfiguracionCorreoDto>())!;
        Assert.Equal("smtp.hogar.com", dto.Host);
        Assert.True(dto.ClaveConfigurada);
    }

    [Fact]
    public async Task UnEspacioNoVeElCorreoDeOtro()
    {
        var hogarA = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "correo.a@ejemplo.com", "Hogar Correo A");

        var hogarB = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "correo.b@ejemplo.com", "Hogar Correo B");

        var clienteB = AyudanteApi.ClienteConToken(fabrica, hogarB.TokenAcceso);

        await clienteB.PutAsJsonAsync(
            "/api/v1/espacios/actual/correo",
            new SolicitudGuardarCorreo(
                "smtp.solo-de-b.com", 587, false, "b@hogar.com",
                ClaveSmtpDePrueba, "b@hogar.com", "B", true));

        var clienteA = AyudanteApi.ClienteConToken(fabrica, hogarA.TokenAcceso);

        var dtoA = await clienteA.GetFromJsonAsync<ConfiguracionCorreoDto>(
            "/api/v1/espacios/actual/correo");

        // El hogar A ve su propia configuracion vacia, no la del hogar B.
        Assert.NotNull(dtoA);
        Assert.Null(dtoA!.Host);
        Assert.False(dtoA.ClaveConfigurada);
    }

    [Fact]
    public async Task ElAdministradorDePlataformaNoPuedeTocarElCorreoDeUnEspacio()
    {
        await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "correo.c@ejemplo.com", "Hogar Correo C");

        var cliente = await ClienteAdministradorAsync();

        var respuesta = await cliente.GetAsync("/api/v1/espacios/actual/correo");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task UnPropietarioNoPuedeTocarElCorreoDeLaPlataforma()
    {
        var propietario = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "correo.d@ejemplo.com", "Hogar Correo D");

        var cliente = AyudanteApi.ClienteConToken(fabrica, propietario.TokenAcceso);

        var respuesta = await cliente.GetAsync("/api/v1/administracion/correo");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task ProbarUnServidorInexistenteDevuelveUnErrorComprensible()
    {
        var cliente = await ClienteAdministradorAsync();

        await cliente.PutAsJsonAsync(
            "/api/v1/administracion/correo",
            new SolicitudGuardarCorreo(
                "servidor.que.no.existe.rumbo", 587, false, "x@y.com",
                "clave", "x@y.com", "Rumbo", true));

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/administracion/correo/probar", new SolicitudProbarCorreo(null));

        // La prueba falla, pero el endpoint responde 200 con el motivo: un servidor mal
        // configurado no es un error de la API, es informacion que hay que mostrar.
        respuesta.EnsureSuccessStatusCode();

        var resultado = (await respuesta.Content.ReadFromJsonAsync<ResultadoPruebaCorreo>())!;

        Assert.False(resultado.Correcta);
        Assert.False(resultado.CorreoEnviado);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Mensaje));
    }
}
