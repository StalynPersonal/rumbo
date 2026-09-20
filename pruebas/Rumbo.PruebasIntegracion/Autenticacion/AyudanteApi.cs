using System.Net.Http.Headers;
using System.Net.Http.Json;

using Rumbo.Contratos.Autenticacion;
using Rumbo.Contratos.Invitaciones;

namespace Rumbo.PruebasIntegracion.Autenticacion;

/// <summary>
/// Atajos para montar escenarios completos sobre la API en las pruebas.
/// </summary>
/// <remarks>
/// Los escenarios de Rumbo requieren varios pasos encadenados (invitar, registrar, invitar de
/// nuevo). Tenerlos aqui evita repetir treinta lineas en cada prueba y que una prueba falle
/// por un error del montaje en vez de por lo que pretende verificar.
/// </remarks>
public static class AyudanteApi
{
    /// <summary>Inicia sesion y devuelve la respuesta completa.</summary>
    /// <param name="cliente">Cliente HTTP.</param>
    /// <param name="correo">Correo de la cuenta.</param>
    /// <param name="clave">Contrasena.</param>
    /// <returns>Credenciales y contexto de la sesion.</returns>
    public static async Task<RespuestaAutenticacion> IniciarSesionAsync(
        HttpClient cliente, string correo, string clave)
    {
        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacion/iniciar-sesion", new SolicitudIniciarSesion(correo, clave));

        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<RespuestaAutenticacion>())!;
    }

    /// <summary>Crea un espacio completo con su propietario, partiendo del administrador.</summary>
    /// <param name="fabrica">Fabrica de la API.</param>
    /// <param name="correoPropietario">Correo de quien sera propietario.</param>
    /// <param name="nombreEspacio">Nombre del espacio.</param>
    /// <param name="clave">Contrasena del propietario.</param>
    /// <returns>Las credenciales del propietario recien creado.</returns>
    public static async Task<RespuestaAutenticacion> CrearEspacioConPropietarioAsync(
        FabricaApiDePrueba fabrica,
        string correoPropietario,
        string nombreEspacio,
        string clave = "ClaveDePruebas2026")
    {
        var clienteAdmin = fabrica.CreateClient();

        var admin = await IniciarSesionAsync(
            clienteAdmin,
            FabricaApiDePrueba.CorreoAdministrador,
            FabricaApiDePrueba.ClaveAdministrador);

        clienteAdmin.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.TokenAcceso);

        var respuestaInvitacion = await clienteAdmin.PostAsJsonAsync(
            "/api/v1/administracion/invitaciones",
            new SolicitudInvitarPropietario(correoPropietario, nombreEspacio, "Pareja"));

        respuestaInvitacion.EnsureSuccessStatusCode();

        var invitacion = (await respuestaInvitacion.Content.ReadFromJsonAsync<InvitacionCreada>())!;

        var clientePropietario = fabrica.CreateClient();

        var respuestaAlta = await clientePropietario.PostAsJsonAsync(
            "/api/v1/autenticacion/registrar",
            new SolicitudRegistrar(
                invitacion.Codigo, correoPropietario, $"Propietario de {nombreEspacio}", clave));

        respuestaAlta.EnsureSuccessStatusCode();

        return (await respuestaAlta.Content.ReadFromJsonAsync<RespuestaAutenticacion>())!;
    }

    /// <summary>Crea un cliente HTTP ya autenticado con el token indicado.</summary>
    /// <param name="fabrica">Fabrica de la API.</param>
    /// <param name="tokenAcceso">Token de acceso.</param>
    /// <returns>Cliente con la cabecera de autorizacion puesta.</returns>
    public static HttpClient ClienteConToken(FabricaApiDePrueba fabrica, string tokenAcceso)
    {
        var cliente = fabrica.CreateClient();

        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenAcceso);

        return cliente;
    }
}
