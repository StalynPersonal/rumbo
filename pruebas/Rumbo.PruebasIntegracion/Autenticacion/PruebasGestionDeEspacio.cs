using System.Net;
using System.Net.Http.Json;

using Rumbo.Contratos.Autenticacion;
using Rumbo.Contratos.Espacios;
using Rumbo.Contratos.Invitaciones;

namespace Rumbo.PruebasIntegracion.Autenticacion;

// Gestion del espacio y de sus miembros.
//
// Las reglas que se prueban aqui evitan tres situaciones sin salida: que un hogar se quede
// sin propietario, que alguien se degrade a si mismo y quede bloqueado, y que un
// administrador se apodere del espacio echando a quien lo creo.
public class PruebasGestionDeEspacio(FabricaApiDePrueba fabrica)
    : IClassFixture<FabricaApiDePrueba>
{
    [Fact]
    public async Task ElPropietarioVeSuEspacioYEsElUnicoMiembroAlPrincipio()
    {
        var propietario = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "gestion1@ejemplo.com", "Hogar Gestión 1");

        var cliente = AyudanteApi.ClienteConToken(fabrica, propietario.TokenAcceso);

        var espacio = await cliente.GetFromJsonAsync<EspacioDetalle>("/api/v1/espacios/actual");

        Assert.NotNull(espacio);
        Assert.Equal("Hogar Gestión 1", espacio!.Nombre);
        Assert.Equal(1, espacio.CantidadMiembros);

        var miembros = await cliente.GetFromJsonAsync<List<MiembroEspacio>>(
            "/api/v1/espacios/actual/miembros");

        Assert.NotNull(miembros);
        Assert.Single(miembros!);
        Assert.Equal("Propietario", miembros![0].Rol);
    }

    [Fact]
    public async Task ElAdministradorDePlataformaNoApareceEntreLosMiembros()
    {
        // Garantia estructural: no tiene fila en MembresiasEspacio, y el listado se
        // construye desde esa tabla.
        var propietario = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "gestion2@ejemplo.com", "Hogar Gestión 2");

        var cliente = AyudanteApi.ClienteConToken(fabrica, propietario.TokenAcceso);

        var miembros = await cliente.GetFromJsonAsync<List<MiembroEspacio>>(
            "/api/v1/espacios/actual/miembros");

        Assert.DoesNotContain(miembros!, m => m.Correo == FabricaApiDePrueba.CorreoAdministrador);
    }

    [Fact]
    public async Task ElPropietarioNoPuedeCambiarSuPropioRol()
    {
        // Sin esta regla, el unico propietario podria degradarse a Miembro y dejar el hogar
        // sin nadie capaz de devolverle el control.
        var propietario = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "gestion3@ejemplo.com", "Hogar Gestión 3");

        var cliente = AyudanteApi.ClienteConToken(fabrica, propietario.TokenAcceso);

        var respuesta = await cliente.PutAsJsonAsync(
            $"/api/v1/espacios/actual/miembros/{propietario.Usuario.Id}/rol",
            new SolicitudCambiarRol("Miembro"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task LosUmbralesDelPresupuestoDebenIrEnOrden()
    {
        var propietario = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "gestion8@ejemplo.com", "Hogar Gestión 8");

        var cliente = AyudanteApi.ClienteConToken(fabrica, propietario.TokenAcceso);

        // Aviso por encima de critico: incoherente.
        var respuesta = await cliente.PutAsJsonAsync(
            "/api/v1/espacios/actual/configuracion",
            new SolicitudActualizarConfiguracion(1, 95m, 90m, 100m, 6, true, 3));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task ElDiaDeInicioDeMesNoPuedeSuperarEl28()
    {
        // 28 es el ultimo dia que existe en todos los meses. Admitir el 31 obligaria a
        // decidir que hacer en febrero.
        var propietario = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "gestion9@ejemplo.com", "Hogar Gestión 9");

        var cliente = AyudanteApi.ClienteConToken(fabrica, propietario.TokenAcceso);

        var respuesta = await cliente.PutAsJsonAsync(
            "/api/v1/espacios/actual/configuracion",
            new SolicitudActualizarConfiguracion(31, 80m, 90m, 100m, 6, true, 3));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>Crea un espacio con su propietario y un miembro invitado por el.</summary>
    /// <param name="correoPropietario">Correo de quien sera propietario.</param>
    /// <param name="correoMiembro">Correo de la persona invitada.</param>
    /// <param name="nombreEspacio">Nombre del espacio.</param>
    /// <returns>Las credenciales de ambos.</returns>
    internal async Task<(RespuestaAutenticacion Propietario, RespuestaAutenticacion Miembro)>
        CrearEspacioConDosPersonasAsync(
            string correoPropietario,
            string correoMiembro,
            string nombreEspacio)
    {
        var propietario = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, correoPropietario, nombreEspacio);

        var clientePropietario = AyudanteApi.ClienteConToken(fabrica, propietario.TokenAcceso);

        var invitacion = (await (await clientePropietario.PostAsJsonAsync(
            "/api/v1/invitaciones", new SolicitudInvitarMiembro(correoMiembro, "Miembro")))
            .Content.ReadFromJsonAsync<InvitacionCreada>())!;

        var alta = await fabrica.CreateClient().PostAsJsonAsync(
            "/api/v1/autenticacion/registrar",
            new SolicitudRegistrar(
                invitacion.Codigo, correoMiembro, "Persona Invitada", "ClaveDePruebas2026"));

        alta.EnsureSuccessStatusCode();

        var miembro = (await alta.Content.ReadFromJsonAsync<RespuestaAutenticacion>())!;

        return (propietario, miembro);
    }

    [Fact]
    public async Task ElTipoDeEspacioElegidoAlInvitarSeRespetaAlCrearlo()
    {
        // La invitacion se emite pidiendo "Pareja". Si el alta creara siempre un espacio
        // Personal, el hogar naceria mal clasificado y nadie se daria cuenta.
        var propietario = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "tipoespacio@ejemplo.com", "Hogar de Pareja");

        var cliente = AyudanteApi.ClienteConToken(fabrica, propietario.TokenAcceso);

        var espacio = await cliente.GetFromJsonAsync<EspacioDetalle>("/api/v1/espacios/actual");

        Assert.Equal("Pareja", espacio!.Tipo);
    }

    [Fact]
    public async Task UnMiembroNoPuedeGestionarALosDemas()
    {
        var (propietario, miembro) = await CrearEspacioConDosPersonasAsync(
            "gestion4@ejemplo.com", "miembro4@ejemplo.com", "Hogar Gestión 4");

        var clienteMiembro = AyudanteApi.ClienteConToken(fabrica, miembro.TokenAcceso);

        var respuesta = await clienteMiembro.PutAsJsonAsync(
            $"/api/v1/espacios/actual/miembros/{propietario.Usuario.Id}/rol",
            new SolicitudCambiarRol("Miembro"));

        // El rol Miembro no tiene el permiso espacio.gestionar_miembros.
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task ElPropietarioPuedeAscenderAUnMiembroAAdministrador()
    {
        var (propietario, miembro) = await CrearEspacioConDosPersonasAsync(
            "gestion5@ejemplo.com", "miembro5@ejemplo.com", "Hogar Gestión 5");

        var cliente = AyudanteApi.ClienteConToken(fabrica, propietario.TokenAcceso);

        var respuesta = await cliente.PutAsJsonAsync(
            $"/api/v1/espacios/actual/miembros/{miembro.Usuario.Id}/rol",
            new SolicitudCambiarRol("Administrador"));

        respuesta.EnsureSuccessStatusCode();

        var actualizado = (await respuesta.Content.ReadFromJsonAsync<MiembroEspacio>())!;

        Assert.Equal("Administrador", actualizado.Rol);
    }

    [Fact]
    public async Task SuspenderAUnMiembroLeCortaElAccesoDeInmediato()
    {
        var (propietario, miembro) = await CrearEspacioConDosPersonasAsync(
            "gestion6@ejemplo.com", "miembro6@ejemplo.com", "Hogar Gestión 6");

        var clienteMiembro = AyudanteApi.ClienteConToken(fabrica, miembro.TokenAcceso);

        // Antes de suspenderlo, entra sin problema.
        var antes = await clienteMiembro.GetAsync("/api/v1/espacios/actual");
        Assert.Equal(HttpStatusCode.OK, antes.StatusCode);

        var clientePropietario = AyudanteApi.ClienteConToken(fabrica, propietario.TokenAcceso);

        var suspension = await clientePropietario.PutAsJsonAsync(
            $"/api/v1/espacios/actual/miembros/{miembro.Usuario.Id}/estado",
            new SolicitudCambiarEstadoMiembro("Suspendida"));

        suspension.EnsureSuccessStatusCode();

        // Su token sigue siendo valido y sigue diciendo que pertenece al espacio, pero el
        // middleware verifica la membresia contra la base de datos en cada peticion. Por eso
        // retirar el acceso no espera a que caduque el token.
        var despues = await clienteMiembro.GetAsync("/api/v1/espacios/actual");

        Assert.Equal(HttpStatusCode.Forbidden, despues.StatusCode);
    }

    [Fact]
    public async Task ElUnicoPropietarioNoPuedeSerDegradadoPorOtraPersona()
    {
        var (propietario, miembro) = await CrearEspacioConDosPersonasAsync(
            "gestion7@ejemplo.com", "miembro7@ejemplo.com", "Hogar Gestión 7");

        var clientePropietario = AyudanteApi.ClienteConToken(fabrica, propietario.TokenAcceso);

        // Se asciende al miembro a Administrador, que ya puede gestionar miembros.
        await clientePropietario.PutAsJsonAsync(
            $"/api/v1/espacios/actual/miembros/{miembro.Usuario.Id}/rol",
            new SolicitudCambiarRol("Administrador"));

        var sesionAdministrador = await AyudanteApi.IniciarSesionAsync(
            fabrica.CreateClient(), "miembro7@ejemplo.com", "ClaveDePruebas2026");

        var clienteAdministrador = AyudanteApi.ClienteConToken(
            fabrica, sesionAdministrador.TokenAcceso);

        // Un administrador que pudiera degradar al dueño se apoderaria del hogar.
        var respuesta = await clienteAdministrador.PutAsJsonAsync(
            $"/api/v1/espacios/actual/miembros/{propietario.Usuario.Id}/rol",
            new SolicitudCambiarRol("Miembro"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }
}
