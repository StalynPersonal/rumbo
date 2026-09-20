using System.Globalization;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Rumbo.Api.Seguridad;

/// <summary>
/// Limites de peticiones de la API.
/// </summary>
/// <remarks>
/// <para>
/// Protegen contra la fuerza bruta sobre las claves y contra el abuso de los endpoints que
/// mandan correo. El bloqueo de cuenta de Identity ya frena los intentos contra <i>una</i>
/// cuenta; esto frena los intentos contra <i>muchas</i> desde el mismo sitio, que es el
/// ataque que el bloqueo por cuenta no llega a ver.
/// </para>
/// <para>
/// <b>Se usa un limitador global que decide por ruta</b>, en lugar de politicas con nombre
/// puestas endpoint a endpoint. La razon es concreta: una politica declarada con un atributo
/// en el controlador queda pisada por la que se aplica en bloque a todos los controladores,
/// y el limite estricto de autenticacion se pierde sin que nada avise. Aqui el reparto esta
/// en un solo sitio y se lee de un vistazo.
/// </para>
/// <para>
/// <b>Es una capa mas, no la unica.</b> Una direccion IP se cambia. Detras siguen estando el
/// bloqueo por intentos, las claves hasheadas y el registro cerrado por invitacion.
/// </para>
/// </remarks>
public static class LimitesDePeticiones
{
    /// <summary>Prefijo de los endpoints de autenticacion.</summary>
    private const string RutaAutenticacion = "/api/v1/autenticacion";

    /// <summary>Prefijo de la API.</summary>
    private const string RutaApi = "/api";

    /// <summary>Registra el limitador en el contenedor.</summary>
    /// <param name="servicios">Coleccion de servicios.</param>
    /// <param name="configuracion">Configuracion de la aplicacion.</param>
    /// <returns>La misma coleccion, para encadenar.</returns>
    public static IServiceCollection AgregarLimitesDePeticiones(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        servicios.Configure<OpcionesLimitePeticiones>(
            configuracion.GetSection(OpcionesLimitePeticiones.Seccion));

        servicios.AddRateLimiter(opciones =>
        {
            // 429 y no 503: el cliente debe saber que fue el quien se paso, no que el
            // servidor este caido.
            opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            opciones.OnRejected = async (contexto, cancelacion) =>
            {
                // Se devuelve cuanto hay que esperar. Sin esta cabecera, un cliente honesto
                // solo puede reintentar a ciegas, que es justo lo que empeora la situacion.
                if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
                {
                    contexto.HttpContext.Response.Headers.RetryAfter =
                        ((int)espera.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                contexto.HttpContext.Response.ContentType = "application/problem+json";

                await contexto.HttpContext.Response.WriteAsync(
                    """
                    {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.29",
                     "title":"Demasiadas peticiones",
                     "status":429,
                     "detail":"Has hecho demasiadas peticiones en poco tiempo. Espera un momento y vuelve a intentarlo."}
                    """,
                    cancelacion);
            };

            opciones.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                contexto =>
                {
                    var ruta = contexto.Request.Path;
                    var cupos = Cupos(contexto);

                    // Autenticacion: cupo estricto. Sin cola, porque encolar intentos de
                    // acceso solo retrasa el ataque y deja peticiones vivas ocupando
                    // memoria mientras tanto.
                    if (ruta.StartsWithSegments(RutaAutenticacion, StringComparison.OrdinalIgnoreCase))
                    {
                        return Particion(
                            "auth:" + ClaveDeCliente(contexto), cupos.PorMinutoAutenticacion);
                    }

                    // Resto de la API: cupo amplio, repartido por usuario cuando hay sesion
                    // y por direccion IP cuando no la hay. Con una particion unica por IP,
                    // dos personas tras el mismo router compartirian cupo.
                    if (ruta.StartsWithSegments(RutaApi, StringComparison.OrdinalIgnoreCase))
                    {
                        return Particion(
                            "api:" + ClaveDeCliente(contexto), cupos.PorMinutoGeneral);
                    }

                    // Todo lo demas —/salud y Swagger— sin limite. Azure consulta /salud
                    // cada pocos segundos: si el limitador lo cortara, la plataforma creeria
                    // que la API esta caida y la reiniciaria.
                    return RateLimitPartition.GetNoLimiter("libre");
                });
        });

        return servicios;
    }

    /// <summary>Crea una particion de ventana fija de un minuto.</summary>
    /// <param name="clave">Clave que identifica la particion.</param>
    /// <param name="cupo">Peticiones permitidas por minuto.</param>
    /// <returns>La particion configurada.</returns>
    private static RateLimitPartition<string> Particion(string clave, int cupo) =>
        RateLimitPartition.GetFixedWindowLimiter(
            clave,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = cupo,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            });

    /// <summary>Lee los cupos configurados.</summary>
    /// <param name="contexto">Peticion en curso.</param>
    /// <returns>Los cupos vigentes.</returns>
    private static OpcionesLimitePeticiones Cupos(HttpContext contexto) =>
        contexto.RequestServices
            .GetRequiredService<IOptions<OpcionesLimitePeticiones>>()
            .Value;

    /// <summary>Decide con que clave se reparte el cupo.</summary>
    /// <param name="contexto">Peticion en curso.</param>
    /// <returns>El identificador del usuario, o la direccion IP si no hay sesion.</returns>
    /// <remarks>
    /// El identificador de usuario sale del token ya validado, nunca de una cabecera: si se
    /// tomara de algo que el cliente controla, bastaria con cambiarlo en cada peticion para
    /// saltarse el limite.
    /// </remarks>
    private static string ClaveDeCliente(HttpContext contexto)
    {
        var usuario = contexto.User.Identity?.IsAuthenticated == true
            ? contexto.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            : null;

        return usuario ?? contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocido";
    }
}
