using System.Net;
using System.Net.Http.Json;

using Rumbo.Contratos.Auditoria;
using Rumbo.Contratos.Comun;
using Rumbo.Contratos.Invitaciones;
using Rumbo.PruebasIntegracion.Autenticacion;
using Rumbo.PruebasIntegracion.Financiero;

namespace Rumbo.PruebasIntegracion.Administracion;

// El historial de auditoria. Se escribe solo, desde un interceptor, para que no dependa de
// que alguien se acuerde de registrar cada accion.
public class PruebasAuditoria(FabricaApiDePrueba fabrica) : IClassFixture<FabricaApiDePrueba>
{
    [Fact]
    public async Task RegistrarUnMovimientoDejaRastroEnElHistorial()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "auditoria1@ejemplo.com", "Hogar Auditoría 1");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 10_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        var movimiento = await AyudanteFinanciero.RegistrarAsync(
            cliente, "Gasto", cuenta.Id, categoria.Id, 500m, "Compra auditada");

        var historial = await cliente.GetFromJsonAsync<ResultadoPaginado<EntradaAuditoria>>(
            $"/api/v1/auditoria?TipoEntidad=Movimiento&EntidadId={movimiento.Id}");

        Assert.NotNull(historial);
        Assert.NotEmpty(historial!.Elementos);
        Assert.Contains(historial.Elementos, e => e.Accion == "Creacion");
    }

    [Fact]
    public async Task ElHistorialNoExponeLosImportes()
    {
        // El campo de cambios guarda los valores anteriores y nuevos. En un movimiento son
        // importes: exponerlos convertiria el historial en una segunda via para leer las
        // finanzas, saltandose los permisos del modulo de movimientos.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "auditoria2@ejemplo.com", "Hogar Auditoría 2");

        var cuenta = await AyudanteFinanciero.CrearCuentaAsync(cliente, "Nómina", 10_000m);
        var categoria = await AyudanteFinanciero.ObtenerCategoriaDeGastoAsync(cliente);

        await AyudanteFinanciero.RegistrarAsync(
            cliente, "Gasto", cuenta.Id, categoria.Id, 7_777m, "Importe reconocible");

        var historial = await cliente.GetFromJsonAsync<ResultadoPaginado<EntradaAuditoria>>(
            "/api/v1/auditoria");

        Assert.NotEmpty(historial!.Elementos);

        // Se revisan los campos de texto, no el cuerpo entero: un identificador GUID es
        // hexadecimal y puede contener «7777» por casualidad, lo que haría fallar la prueba
        // un día de cada tantos sin que nada estuviera mal.
        var textos = historial.Elementos
            .SelectMany(e => new[] { e.Accion, e.TipoEntidad, e.Descripcion, e.CorreoUsuario })
            .Where(t => t is not null);

        Assert.All(textos, t => Assert.DoesNotContain("7777", t!, StringComparison.Ordinal));
    }

    [Fact]
    public async Task UnHogarNoVeElHistorialDeOtro()
    {
        // RegistrosAuditoria es una tabla global sin filtro: el aislamiento depende de un
        // Where explicito en el servicio. Si faltara, un hogar veria la actividad del otro.
        var hogarA = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "auditoria3a@ejemplo.com", "Hogar Auditoría A");

        var hogarB = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "auditoria3b@ejemplo.com", "Hogar Auditoría B");

        var cuentaB = await AyudanteFinanciero.CrearCuentaAsync(
            hogarB, "Cuenta privada de B", 5_000m);

        var historialDeA = await hogarA.GetFromJsonAsync<ResultadoPaginado<EntradaAuditoria>>(
            "/api/v1/auditoria?TipoEntidad=Cuenta");

        Assert.DoesNotContain(historialDeA!.Elementos, e => e.EntidadId == cuentaB.Id);
    }

    [Fact]
    public async Task UnMiembroSinPermisoNoPuedeAuditar()
    {
        // El historial revela los hábitos de cada persona: no todos los miembros tienen por
        // qué poder auditarse entre sí.
        var propietario = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, "auditoria4@ejemplo.com", "Hogar Auditoría 4");

        var clientePropietario = AyudanteApi.ClienteConToken(fabrica, propietario.TokenAcceso);

        var invitacion = (await (await clientePropietario.PostAsJsonAsync(
            "/api/v1/invitaciones", new SolicitudInvitarMiembro("miembro.aud@ejemplo.com", "Miembro")))
            .Content.ReadFromJsonAsync<InvitacionCreada>())!;

        var alta = await fabrica.CreateClient().PostAsJsonAsync(
            "/api/v1/autenticacion/registrar",
            new Rumbo.Contratos.Autenticacion.SolicitudRegistrar(
                invitacion.Codigo, "miembro.aud@ejemplo.com", "Miembro", "ClaveDePruebas2026"));

        var miembro = (await alta.Content
            .ReadFromJsonAsync<Rumbo.Contratos.Autenticacion.RespuestaAutenticacion>())!;

        var clienteMiembro = AyudanteApi.ClienteConToken(fabrica, miembro.TokenAcceso);

        var respuesta = await clienteMiembro.GetAsync("/api/v1/auditoria");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }
}
