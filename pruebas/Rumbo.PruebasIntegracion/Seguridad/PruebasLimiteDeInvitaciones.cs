using System.Net;
using System.Net.Http.Json;

using Rumbo.Contratos.Invitaciones;
using Rumbo.PruebasIntegracion.Autenticacion;
using Rumbo.PruebasIntegracion.Financiero;

namespace Rumbo.PruebasIntegracion.Seguridad;

// Una cuenta comprometida no debe poder convertirse en un enviador de correo masivo desde
// nuestro servidor SMTP: eso arruinaria la reputacion del dominio remitente y dejaria de
// llegar el correo legitimo.
public class PruebasLimiteDeInvitaciones(FabricaApiDePrueba fabrica)
    : IClassFixture<FabricaApiDePrueba>
{
    [Fact]
    public async Task UnEspacioNoPuedeEnviarInvitacionesSinTope()
    {
        var cliente = await AyudanteFinanciero.CrearHogarAsync(
            fabrica, "invlimite@ejemplo.com", "Hogar Límite Invitaciones");

        HttpResponseMessage? rechazada = null;
        var aceptadas = 0;

        // Se invita a personas distintas cada vez, así que el rechazo no puede venir de la
        // regla de «ya hay una invitación pendiente para ese correo».
        for (var numero = 0; numero < 12 && rechazada is null; numero++)
        {
            var respuesta = await cliente.PostAsJsonAsync(
                "/api/v1/invitaciones",
                new SolicitudInvitarMiembro($"invitado{numero}@ejemplo.com", "Miembro"));

            if (respuesta.StatusCode == HttpStatusCode.BadRequest)
            {
                rechazada = respuesta;
            }
            else
            {
                respuesta.EnsureSuccessStatusCode();
                aceptadas++;
            }
        }

        Assert.NotNull(rechazada);

        // El tope por hora son diez.
        Assert.Equal(10, aceptadas);

        var cuerpo = await rechazada!.Content.ReadAsStringAsync();

        Assert.Contains("última hora", cuerpo, StringComparison.Ordinal);
    }
}
