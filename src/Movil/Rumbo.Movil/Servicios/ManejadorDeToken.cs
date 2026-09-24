using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Rumbo.Contratos.Autenticacion;

namespace Rumbo.Movil.Servicios;

/// <summary>
/// Pone el token en cada peticion y lo renueva cuando caduca.
/// </summary>
/// <remarks>
/// ESTE ES EL UNICO PUNTO REALMENTE NO OBVIO DE LA APLICACION. Merece leerse entero.
///
/// El token de acceso dura 15 minutos. Si cada servicio tuviera que comprobar la caducidad
/// antes de llamar, ese codigo estaria repetido en los cuarenta metodos de los servicios, y
/// el dia que se olvide en uno, esa pantalla fallara al azar y sera imposible de reproducir.
///
/// Un DelegatingHandler se mete EN MEDIO del HttpClient y la red: toda peticion pasa por
/// aqui, salga de donde salga. Se registra una vez en MauiProgram y ya no hay que acordarse.
/// </remarks>
/// <param name="almacen">Donde estan guardados los tokens.</param>
public class ManejadorDeToken(AlmacenSesion almacen) : DelegatingHandler
{
    // Evita que varias peticiones que fallan a la vez intenten renovar todas. Sin esto, al
    // abrir una pantalla que hace tres llamadas simultaneas se dispararian tres
    // renovaciones, y como el servidor ROTA el token (detecta el reuso como robo), la
    // segunda y la tercera invalidarian la sesion entera.
    private static readonly SemaphoreSlim Cerrojo = new(1, 1);

    /// <summary>Procesa la peticion.</summary>
    /// <param name="peticion">Peticion saliente.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La respuesta del servidor.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage peticion,
        CancellationToken cancelacion)
    {
        await PonerTokenAsync(peticion);

        var respuesta = await base.SendAsync(peticion, cancelacion);

        if (respuesta.StatusCode != HttpStatusCode.Unauthorized)
        {
            return respuesta;
        }

        // 401 significa que el token caduco. Se renueva UNA vez y se reintenta.
        if (!await RenovarAsync(cancelacion))
        {
            // Si la renovacion falla, la sesion termino de verdad. No se reintenta en bucle:
            // un token revocado provocaria llamadas infinitas contra el servidor.
            return respuesta;
        }

        // La peticion original ya se consumio, asi que hay que clonarla para reenviarla.
        var reintento = await ClonarAsync(peticion);
        await PonerTokenAsync(reintento);

        respuesta.Dispose();

        return await base.SendAsync(reintento, cancelacion);
    }

    /// <summary>Pone la cabecera de autorizacion si hay token.</summary>
    /// <param name="peticion">Peticion a la que se le pone.</param>
    /// <returns>Tarea que finaliza cuando esta puesta.</returns>
    private async Task PonerTokenAsync(HttpRequestMessage peticion)
    {
        var token = await almacen.ObtenerTokenAccesoAsync();

        if (!string.IsNullOrWhiteSpace(token))
        {
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    /// <summary>Pide al servidor un token nuevo.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns><c>true</c> si se renovo.</returns>
    private async Task<bool> RenovarAsync(CancellationToken cancelacion)
    {
        await Cerrojo.WaitAsync(cancelacion);

        try
        {
            var tokenRenovacion = await almacen.ObtenerTokenRenovacionAsync();

            if (string.IsNullOrWhiteSpace(tokenRenovacion))
            {
                return false;
            }

            // Cliente APARTE, sin este manejador. Si la renovacion pasara por aqui y
            // devolviera 401, intentaria renovarse a si misma indefinidamente.
            using var cliente = new HttpClient
            {
                BaseAddress = new Uri(ConfiguracionApi.DireccionBase),
            };

            var respuesta = await cliente.PostAsJsonAsync(
                "api/v1/autenticacion/renovar",
                new SolicitudRenovar(tokenRenovacion),
                cancelacion);

            if (!respuesta.IsSuccessStatusCode)
            {
                // La sesion caduco o el servidor la revoco. Se borra lo guardado para que la
                // aplicacion pida acceso de nuevo en vez de fallar una y otra vez.
                almacen.Limpiar();

                return false;
            }

            var datos = await respuesta.Content
                .ReadFromJsonAsync<RespuestaAutenticacion>(cancelacion);

            if (datos is null)
            {
                return false;
            }

            await almacen.GuardarAsync(datos.TokenAcceso, datos.TokenRenovacion);

            return true;
        }
        catch
        {
            // Sin conexion, por ejemplo. No se limpia la sesion: la persona sigue dentro,
            // simplemente no hay red ahora mismo. Borrarla la obligaria a volver a entrar
            // cada vez que se mete en un ascensor.
            return false;
        }
        finally
        {
            Cerrojo.Release();
        }
    }

    /// <summary>Copia una peticion para poder reenviarla.</summary>
    /// <param name="original">Peticion original.</param>
    /// <returns>Una copia nueva.</returns>
    /// <remarks>
    /// Hace falta porque el contenido de una peticion HTTP es un flujo que se lee UNA vez:
    /// reenviar la misma instancia lanzaria una excepcion diciendo que ya se envio.
    /// </remarks>
    private static async Task<HttpRequestMessage> ClonarAsync(HttpRequestMessage original)
    {
        var copia = new HttpRequestMessage(original.Method, original.RequestUri);

        if (original.Content is not null)
        {
            var cuerpo = await original.Content.ReadAsByteArrayAsync();
            copia.Content = new ByteArrayContent(cuerpo);

            foreach (var cabecera in original.Content.Headers)
            {
                copia.Content.Headers.TryAddWithoutValidation(cabecera.Key, cabecera.Value);
            }
        }

        foreach (var cabecera in original.Headers)
        {
            copia.Headers.TryAddWithoutValidation(cabecera.Key, cabecera.Value);
        }

        return copia;
    }
}
