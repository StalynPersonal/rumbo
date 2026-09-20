using Microsoft.AspNetCore.Authorization;

using Rumbo.Api.Autorizacion;
using Rumbo.Api.Middleware;
using Rumbo.Infraestructura;

namespace Rumbo.Api.Extensiones;

/// <summary>
/// Metodos de extension que registran en el contenedor de dependencias todo lo que necesita
/// la aplicacion.
/// </summary>
/// <remarks>
/// Mantenerlos aqui evita que <c>Program.cs</c> crezca sin control y deja en un unico punto
/// la composicion de dependencias, que es lo que justifica que la capa de API referencie a
/// la de infraestructura.
/// </remarks>
public static class ExtensionesServicios
{
    /// <summary>
    /// Registra los servicios de la API y de la infraestructura.
    /// </summary>
    /// <param name="servicios">Coleccion de servicios de la aplicacion.</param>
    /// <param name="configuracion">Configuracion de la aplicacion.</param>
    /// <returns>La misma coleccion, para poder encadenar llamadas.</returns>
    public static IServiceCollection AgregarServiciosDeApi(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        // Base de datos, identidad e interceptores.
        servicios.AgregarInfraestructura(configuracion);

        servicios.AddControllers();

        // --- Autorizacion por permiso ------------------------------------------
        // El proveedor construye al vuelo una politica por cada permiso que se use, de modo
        // que anadir un permiso nuevo no obliga a registrar nada aqui.
        servicios.AddSingleton<IAuthorizationPolicyProvider, ProveedorPoliticasPermiso>();
        servicios.AddScoped<IAuthorizationHandler, ManejadorPermisos>();

        // --- Errores en formato ProblemDetails ----------------------------------
        servicios.AddExceptionHandler<ManejadorExcepcionesGlobal>();

        // Generacion del documento OpenAPI, incluida en ASP.NET Core desde .NET 9.
        servicios.AddOpenApi();

        // ProblemDetails (RFC 9457) como formato unico de error de toda la API.
        servicios.AddProblemDetails();

        return servicios;
    }
}
