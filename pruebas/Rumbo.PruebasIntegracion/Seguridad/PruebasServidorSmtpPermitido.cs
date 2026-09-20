using System.Net;
using System.Net.Http.Json;

using Rumbo.Contratos.Correo;
using Rumbo.PruebasIntegracion.Autenticacion;
using Rumbo.PruebasIntegracion.Financiero;

namespace Rumbo.PruebasIntegracion.Seguridad;

// El servidor SMTP de cada espacio lo elige su propietario, y la API se conecta a donde le
// digan. Sin limites, ese campo se convierte en una forma de hacer que Rumbo hable con lo que
// haya detras del cortafuegos, o de sondear que puertos estan abiertos midiendo los tiempos.
public class PruebasServidorSmtpPermitido(FabricaApiDePrueba fabrica)
    : IClassFixture<FabricaApiDePrueba>
{
    /// <summary>Construye una configuracion de correo valida salvo por lo que se pruebe.</summary>
    /// <param name="host">Servidor SMTP.</param>
    /// <param name="puerto">Puerto.</param>
    /// <returns>La solicitud.</returns>
    private static SolicitudGuardarCorreo Configuracion(string host, int puerto) =>
        new(host, puerto, true, "usuario", "clave-de-aplicacion",
            "correo@ejemplo.com", "Hogar", true);

    [Theory]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.5")]
    [InlineData("192.168.1.1")]
    [InlineData("172.16.0.1")]
    [InlineData("169.254.169.254")]
    public async Task UnServidorDeLaRedInternaEsRechazado(string host)
    {
        // 169.254.169.254 es el servicio de metadatos de las nubes: el destino clasico de
        // este tipo de abuso, porque devuelve credenciales de la maquina.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, $"smtp-{host.Replace('.', '-')}@ejemplo.com", $"Hogar SMTP {host}");

        var respuesta = await cliente.PutAsJsonAsync(
            "/api/v1/espacios/actual/correo", Configuracion(host, 587));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Theory]
    [InlineData(22)]
    [InlineData(3306)]
    [InlineData(1433)]
    [InlineData(6379)]
    public async Task UnPuertoQueNoEsDeCorreoEsRechazado(int puerto)
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, $"smtpport{puerto}@ejemplo.com", $"Hogar Puerto {puerto}");

        var respuesta = await cliente.PutAsJsonAsync(
            "/api/v1/espacios/actual/correo", Configuracion("smtp.ejemplo.com", puerto));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Theory]
    [InlineData(25)]
    [InlineData(465)]
    [InlineData(587)]
    [InlineData(2525)]
    public async Task LosPuertosDeCorreoDeVerdadSiSeAceptan(int puerto)
    {
        // La restriccion no puede impedir configurar un servidor legitimo.
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, $"smtpok{puerto}@ejemplo.com", $"Hogar SMTP OK {puerto}");

        var respuesta = await cliente.PutAsJsonAsync(
            "/api/v1/espacios/actual/correo", Configuracion("smtp.gmail.com", puerto));

        respuesta.EnsureSuccessStatusCode();
    }
}
