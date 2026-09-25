using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Rumbo.Movil.Servicios;

/// <summary>
/// Error que devuelve la API, ya traducido a algo que se le puede enseñar a una persona.
/// </summary>
/// <param name="mensaje">Explicacion en espanol.</param>
/// <param name="codigo">Codigo HTTP que devolvio el servidor.</param>
public class ExcepcionApi(string mensaje, HttpStatusCode codigo) : Exception(mensaje)
{
    /// <summary>Codigo HTTP de la respuesta.</summary>
    public HttpStatusCode Codigo { get; } = codigo;
}

/// <summary>
/// El unico sitio de la aplicacion que habla con el servidor.
/// </summary>
/// <remarks>
/// Los servicios de cada modulo (cuentas, movimientos, metas...) usan esta clase. Asi, el
/// manejo de errores y la deserializacion estan escritos una vez.
/// </remarks>
/// <param name="cliente">Cliente HTTP ya configurado con la direccion y el manejador.</param>
public class ClienteApi(HttpClient cliente)
{
    /// <summary>Opciones de JSON, iguales a las que usa la API.</summary>
    /// <remarks>
    /// El servidor manda los nombres en minuscula inicial (montoObjetivo) y las clases de
    /// Contratos los tienen en mayuscula (MontoObjetivo). Sin esta linea, todas las
    /// propiedades llegarian vacias y el sintoma seria una pantalla en blanco sin error.
    /// </remarks>
    private static readonly JsonSerializerOptions Opciones = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Pide datos al servidor.</summary>
    /// <typeparam name="T">Tipo que se espera recibir.</typeparam>
    /// <param name="ruta">Ruta relativa, por ejemplo <c>api/v1/cuentas</c>.</param>
    /// <returns>Lo que devolvio el servidor.</returns>
    public async Task<T> ObtenerAsync<T>(string ruta)
    {
        var respuesta = await cliente.GetAsync(ruta);

        return await LeerAsync<T>(respuesta);
    }

    /// <summary>Envia datos al servidor.</summary>
    /// <typeparam name="TPeticion">Tipo de lo que se envia.</typeparam>
    /// <typeparam name="TRespuesta">Tipo que se espera recibir.</typeparam>
    /// <param name="ruta">Ruta relativa.</param>
    /// <param name="cuerpo">Datos que se envian.</param>
    /// <returns>Lo que devolvio el servidor.</returns>
    public async Task<TRespuesta> EnviarAsync<TPeticion, TRespuesta>(
        string ruta,
        TPeticion cuerpo)
    {
        var respuesta = await cliente.PostAsJsonAsync(ruta, cuerpo);

        return await LeerAsync<TRespuesta>(respuesta);
    }

    /// <summary>Modifica algo en el servidor.</summary>
    /// <typeparam name="TPeticion">Tipo de lo que se envia.</typeparam>
    /// <typeparam name="TRespuesta">Tipo que se espera recibir.</typeparam>
    /// <param name="ruta">Ruta relativa.</param>
    /// <param name="cuerpo">Datos que se envian.</param>
    /// <returns>Lo que devolvio el servidor.</returns>
    public async Task<TRespuesta> ActualizarAsync<TPeticion, TRespuesta>(
        string ruta,
        TPeticion cuerpo)
    {
        var respuesta = await cliente.PutAsJsonAsync(ruta, cuerpo);

        return await LeerAsync<TRespuesta>(respuesta);
    }

    /// <summary>Envia datos sin esperar respuesta con contenido.</summary>
    /// <typeparam name="TPeticion">Tipo de lo que se envia.</typeparam>
    /// <param name="ruta">Ruta relativa.</param>
    /// <param name="cuerpo">Datos que se envian.</param>
    /// <returns>Tarea que finaliza cuando el servidor responde.</returns>
    public async Task EnviarSinRespuestaAsync<TPeticion>(string ruta, TPeticion cuerpo)
    {
        var respuesta = await cliente.PostAsJsonAsync(ruta, cuerpo);

        await VerificarAsync(respuesta);
    }

    /// <summary>Borra algo en el servidor.</summary>
    /// <param name="ruta">Ruta relativa.</param>
    /// <returns>Tarea que finaliza cuando el servidor responde.</returns>
    /// <remarks>
    /// En Rumbo casi todo el borrado es LOGICO: la fila permanece en la base de datos para
    /// la auditoria y deja de contar en saldos e informes. Un movimiento borrado no
    /// desaparece del historial del hogar, simplemente deja de sumar.
    /// </remarks>
    public async Task BorrarAsync(string ruta)
    {
        var respuesta = await cliente.DeleteAsync(ruta);

        await VerificarAsync(respuesta);
    }

    /// <summary>Convierte la respuesta en el objeto esperado, o lanza un error legible.</summary>
    /// <typeparam name="T">Tipo esperado.</typeparam>
    /// <param name="respuesta">Respuesta del servidor.</param>
    /// <returns>El objeto.</returns>
    private static async Task<T> LeerAsync<T>(HttpResponseMessage respuesta)
    {
        await VerificarAsync(respuesta);

        var datos = await respuesta.Content.ReadFromJsonAsync<T>(Opciones);

        return datos ?? throw new ExcepcionApi(
            "El servidor respondió sin datos.", respuesta.StatusCode);
    }

    /// <summary>Lanza una excepcion legible si la respuesta no fue correcta.</summary>
    /// <param name="respuesta">Respuesta del servidor.</param>
    /// <returns>Tarea que finaliza tras la comprobacion.</returns>
    /// <remarks>
    /// La API devuelve los errores en formato ProblemDetails con un campo "detail" escrito
    /// en espanol y pensado para leerse. Se aprovecha: enseñar "BadRequest" a una persona no
    /// le dice nada, y "El aporte debe ser mayor que cero" si.
    /// </remarks>
    private static async Task VerificarAsync(HttpResponseMessage respuesta)
    {
        if (respuesta.IsSuccessStatusCode)
        {
            return;
        }

        var mensaje = await ExtraerMensajeAsync(respuesta);

        throw new ExcepcionApi(mensaje, respuesta.StatusCode);
    }

    /// <summary>Saca el mensaje de error de la respuesta.</summary>
    /// <param name="respuesta">Respuesta del servidor.</param>
    /// <returns>El texto que se le mostrara a la persona.</returns>
    private static async Task<string> ExtraerMensajeAsync(HttpResponseMessage respuesta)
    {
        try
        {
            var problema = await respuesta.Content
                .ReadFromJsonAsync<JsonElement>(Opciones);

            if (problema.TryGetProperty("detail", out var detalle)
                && detalle.GetString() is { Length: > 0 } texto)
            {
                return texto;
            }
        }
        catch
        {
            // El cuerpo no era JSON. Se cae al mensaje generico de abajo.
        }

        return respuesta.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Tu sesión terminó. Vuelve a entrar.",
            HttpStatusCode.Forbidden => "No tienes permiso para hacer esto.",
            HttpStatusCode.NotFound => "No se encontró lo que buscabas.",
            HttpStatusCode.TooManyRequests =>
                "Demasiados intentos seguidos. Espera un momento.",
            _ => "No se pudo conectar con Rumbo. Revisa tu conexión e inténtalo de nuevo.",
        };
    }
}
