using System.Net;
using System.Net.Http.Json;

using Rumbo.Contratos.Autenticacion;
using Rumbo.Contratos.Invitaciones;

namespace Rumbo.PruebasIntegracion.Autenticacion;

// Verifica el modelo de alta cerrada: nadie entra en Rumbo sin una invitacion valida.
public class PruebasFlujoDeAlta(FabricaApiDePrueba fabrica) : IClassFixture<FabricaApiDePrueba>
{
    [Fact]
    public async Task ElAdministradorDePlataformaNoPerteneceANingunEspacio()
    {
        // Es la garantia estructural del modelo: el administrador no tiene fila en
        // MembresiasEspacio, asi que no aparece en el listado de miembros de ningun hogar
        // ni puede operar sobre sus finanzas.
        var cliente = fabrica.CreateClient();

        var sesion = await AyudanteApi.IniciarSesionAsync(
            cliente, FabricaApiDePrueba.CorreoAdministrador, FabricaApiDePrueba.ClaveAdministrador);

        Assert.True(sesion.Usuario.EsAdministradorPlataforma);
        Assert.Null(sesion.EspacioActivo);
        Assert.Empty(sesion.EspaciosDisponibles);
    }

    [Fact]
    public async Task NoSePuedeRegistrarSinCodigoDeInvitacion()
    {
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacion/registrar",
            new SolicitudRegistrar("codigo-inventado", "nadie@ejemplo.com", "Nadie", "ClaveLarga2026"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task AceptarUnaInvitacionDePropietarioCreaElEspacioConSusCategorias()
    {
        var propietario = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "propietario.alta@ejemplo.com", "Hogar de prueba");

        Assert.NotNull(propietario.EspacioActivo);
        Assert.Equal("Hogar de prueba", propietario.EspacioActivo!.Nombre);
        Assert.Equal("Propietario", propietario.EspacioActivo.Rol);
        Assert.Equal("DOP", propietario.EspacioActivo.MonedaBase);
    }

    [Fact]
    public async Task UnCodigoDeInvitacionSoloSirveUnaVez()
    {
        var clienteAdmin = AyudanteApi.ClienteConToken(
            fabrica,
            (await AyudanteApi.IniciarSesionAsync(
                fabrica.CreateClient(),
                FabricaApiDePrueba.CorreoAdministrador,
                FabricaApiDePrueba.ClaveAdministrador)).TokenAcceso);

        var invitacion = (await (await clienteAdmin.PostAsJsonAsync(
            "/api/v1/administracion/invitaciones",
            new SolicitudInvitarPropietario("unsolouso@ejemplo.com", "Hogar único", "Personal")))
            .Content.ReadFromJsonAsync<InvitacionCreada>())!;

        var primerUso = await fabrica.CreateClient().PostAsJsonAsync(
            "/api/v1/autenticacion/registrar",
            new SolicitudRegistrar(
                invitacion.Codigo, "unsolouso@ejemplo.com", "Primera Persona", "ClaveLarga2026"));

        Assert.Equal(HttpStatusCode.OK, primerUso.StatusCode);

        var segundoUso = await fabrica.CreateClient().PostAsJsonAsync(
            "/api/v1/autenticacion/registrar",
            new SolicitudRegistrar(
                invitacion.Codigo, "otro@ejemplo.com", "Segunda Persona", "ClaveLarga2026"));

        Assert.Equal(HttpStatusCode.BadRequest, segundoUso.StatusCode);
    }

    [Fact]
    public async Task UnCodigoEmitidoParaOtroCorreoNoSirve()
    {
        // El codigo va ligado a un destinatario. Si se reenvia a otra persona, no debe
        // servirle: de lo contrario, filtrar un correo bastaria para colarse.
        var clienteAdmin = AyudanteApi.ClienteConToken(
            fabrica,
            (await AyudanteApi.IniciarSesionAsync(
                fabrica.CreateClient(),
                FabricaApiDePrueba.CorreoAdministrador,
                FabricaApiDePrueba.ClaveAdministrador)).TokenAcceso);

        var invitacion = (await (await clienteAdmin.PostAsJsonAsync(
            "/api/v1/administracion/invitaciones",
            new SolicitudInvitarPropietario("destinatario@ejemplo.com", "Hogar", "Personal")))
            .Content.ReadFromJsonAsync<InvitacionCreada>())!;

        var respuesta = await fabrica.CreateClient().PostAsJsonAsync(
            "/api/v1/autenticacion/registrar",
            new SolicitudRegistrar(
                invitacion.Codigo, "interceptor@ejemplo.com", "Interceptor", "ClaveLarga2026"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }
}
