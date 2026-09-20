using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

using Rumbo.Dominio.Excepciones;

namespace Rumbo.Api.Middleware;

/// <summary>
/// Convierte cualquier excepcion no controlada en una respuesta <c>ProblemDetails</c>.
/// </summary>
/// <remarks>
/// <para>
/// Toda la API responde los errores con el mismo formato (RFC 9457), de modo que la
/// aplicacion movil tiene un unico camino para mostrarlos.
/// </para>
/// <para>
/// <b>Lo que no se expone.</b> En produccion nunca sale una traza de pila ni el mensaje
/// interno de una excepcion inesperada: revelaria rutas de ficheros, nombres de tablas y
/// versiones de librerias. Se devuelve un mensaje generico y un <c>traceId</c> con el que
/// localizar el detalle completo en los registros del servidor.
/// </para>
/// <para>
/// Las excepciones de dominio SI muestran su mensaje, porque estan escritas para que las lea
/// la persona ("El código de invitación ha caducado").
/// </para>
/// </remarks>
/// <param name="registro">Registro de eventos.</param>
/// <param name="entorno">Entorno de ejecucion.</param>
public class ManejadorExcepcionesGlobal(
    ILogger<ManejadorExcepcionesGlobal> registro,
    IHostEnvironment entorno) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext contextoHttp,
        Exception excepcion,
        CancellationToken cancelacion)
    {
        var (codigo, titulo, detalle) = Clasificar(excepcion);

        var idTraza = contextoHttp.TraceIdentifier;

        if (codigo >= StatusCodes.Status500InternalServerError)
        {
            registro.LogError(excepcion,
                "Error no controlado en {Metodo} {Ruta}. TraceId: {TraceId}",
                contextoHttp.Request.Method, contextoHttp.Request.Path, idTraza);
        }
        else
        {
            // Los errores de negocio son parte del funcionamiento normal: se registran como
            // informacion, no como fallo, para no llenar las alertas de ruido.
            registro.LogInformation(
                "Petición rechazada en {Metodo} {Ruta}: {Motivo}. TraceId: {TraceId}",
                contextoHttp.Request.Method, contextoHttp.Request.Path, titulo, idTraza);
        }

        var problema = new ProblemDetails
        {
            Status = codigo,
            Title = titulo,
            Detail = detalle,
            Instance = $"{contextoHttp.Request.Method} {contextoHttp.Request.Path}",
        };

        problema.Extensions["traceId"] = idTraza;

        // Solo fuera de produccion se adjunta el detalle tecnico, y aun asi sin traza de pila.
        if (!entorno.IsProduction() && codigo >= StatusCodes.Status500InternalServerError)
        {
            problema.Extensions["excepcion"] = excepcion.GetType().Name;
            problema.Extensions["mensajeTecnico"] = excepcion.Message;
        }

        contextoHttp.Response.StatusCode = codigo;
        await contextoHttp.Response.WriteAsJsonAsync(problema, cancelacion);

        return true;
    }

    /// <summary>Traduce una excepcion al codigo HTTP y al mensaje que debe verse.</summary>
    /// <param name="excepcion">Excepcion capturada.</param>
    /// <returns>Codigo HTTP, titulo y detalle.</returns>
    private static (int Codigo, string Titulo, string Detalle) Clasificar(Exception excepcion) =>
        excepcion switch
        {
            // 401: las credenciales no valen. Mismo mensaje exista o no la cuenta.
            ExcepcionCredencialesInvalidas or ExcepcionTokenInvalido =>
                (StatusCodes.Status401Unauthorized, "No autorizado", excepcion.Message),

            // 423: la cuenta esta bloqueada temporalmente.
            ExcepcionCuentaBloqueada =>
                (StatusCodes.Status423Locked, "Cuenta bloqueada", excepcion.Message),

            // 404, NO 403: se usa el mismo codigo para "no existe" y para "es de otro
            // espacio". Responder 403 confirmaria que el registro existe, y eso ya es
            // informacion que no corresponde dar.
            ExcepcionNoEncontrado or ExcepcionSinAccesoAlEspacio =>
                (StatusCodes.Status404NotFound, "No encontrado", excepcion.Message),

            // Un intento de escribir en otro espacio no es un error de usuario: es un fallo
            // grave. Se responde 404 generico y el detalle real queda solo en los registros.
            ExcepcionEspacioNoCoincide =>
                (StatusCodes.Status404NotFound, "No encontrado",
                    "No se encontró el recurso solicitado."),

            // 400: reglas de negocio con mensaje pensado para la persona.
            ExcepcionDominio =>
                (StatusCodes.Status400BadRequest, "Solicitud no válida", excepcion.Message),

            // 409: dos personas escribieron sobre el mismo registro a la vez.
            Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException =>
                (StatusCodes.Status409Conflict, "Conflicto de concurrencia",
                    "Otra persona modificó esta información al mismo tiempo. "
                    + "Vuelve a cargar los datos e inténtalo de nuevo."),

            // 499: el cliente se fue antes de terminar. No es un error del servidor.
            OperationCanceledException =>
                (StatusCodes.Status499ClientClosedRequest, "Petición cancelada",
                    "La petición se canceló antes de completarse."),

            _ => (StatusCodes.Status500InternalServerError, "Error interno",
                "Ocurrió un error inesperado. Si el problema persiste, comparte el "
                + "identificador de seguimiento con soporte."),
        };
}
