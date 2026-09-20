namespace Rumbo.Api.Middleware;

/// <summary>
/// Anade a cada respuesta las cabeceras de seguridad del navegador.
/// </summary>
/// <remarks>
/// <para>
/// Rumbo es una API que consume una aplicacion Android, asi que buena parte de estas
/// cabeceras no protegen al cliente principal. Se ponen igual por tres razones: Swagger sí
/// se abre en un navegador, cualquier respuesta JSON puede acabar abierta en uno, y el dia
/// que exista una web no habra que acordarse de esto.
/// </para>
/// <para>
/// Tambien se <b>quitan</b> las cabeceras que revelan con que esta hecho el servidor. No es
/// seguridad de verdad —nadie se detiene por no saber la version—, pero tampoco hay razon
/// para regalar el dato.
/// </para>
/// </remarks>
/// <param name="siguiente">Siguiente eslabon de la tuberia.</param>
public class MiddlewareCabecerasSeguridad(RequestDelegate siguiente)
{
    /// <summary>Procesa la peticion.</summary>
    /// <param name="contexto">Peticion en curso.</param>
    /// <returns>Tarea que finaliza cuando termina el resto de la tuberia.</returns>
    public async Task InvokeAsync(HttpContext contexto)
    {
        // Las cabeceras se fijan ANTES de que nadie escriba en el cuerpo: una vez enviada la
        // respuesta ya no se pueden cambiar, y la excepcion que eso provoca llegaria tarde.
        contexto.Response.OnStarting(() =>
        {
            var cabeceras = contexto.Response.Headers;

            // Impide que el navegador adivine el tipo de contenido. Sin esto, un JSON con
            // texto elegido por un atacante podria interpretarse como HTML y ejecutarse.
            cabeceras["X-Content-Type-Options"] = "nosniff";

            // La API no se muestra dentro de un marco. Evita el secuestro de clics.
            cabeceras["X-Frame-Options"] = "DENY";

            // No se filtra la URL completa al salir hacia otro sitio: una ruta como
            // /api/v1/metas/{id} ya dice mas de la cuenta.
            cabeceras["Referrer-Policy"] = "no-referrer";

            // Una API no necesita camara, microfono ni ubicacion. Declararlo cierra la
            // puerta por si alguna vez se sirve contenido desde aqui.
            cabeceras["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            // Politica de contenido restrictiva. La API solo devuelve JSON, asi que no
            // necesita cargar absolutamente nada.
            //
            // Se excluye Swagger, que es una pagina de verdad y necesita su propio script y
            // su hoja de estilos. Relajar la politica para toda la API con tal de que
            // Swagger funcione seria pagar en produccion una comodidad de desarrollo; y
            // Swagger, ademas, solo se publica fuera de produccion.
            if (!EsSwagger(contexto.Request.Path))
            {
                cabeceras["Content-Security-Policy"] =
                    "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
            }

            // Datos financieros: ni cache compartida ni copias en disco del navegador.
            cabeceras.CacheControl = "no-store, no-cache, must-revalidate";
            cabeceras.Pragma = "no-cache";

            // Sobran y solo dicen con que esta hecho el servidor.
            cabeceras.Remove("Server");
            cabeceras.Remove("X-Powered-By");

            return Task.CompletedTask;
        });

        await siguiente(contexto);
    }

    /// <summary>Indica si la ruta pertenece a la interfaz de Swagger.</summary>
    /// <param name="ruta">Ruta de la peticion.</param>
    /// <returns><c>true</c> si es Swagger o su documento OpenAPI.</returns>
    private static bool EsSwagger(PathString ruta) =>
        ruta.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase)
        || ruta.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase);
}
