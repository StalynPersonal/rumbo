namespace Rumbo.Api.Extensiones;

/// <summary>
/// Metodos de extension que registran en el contenedor de dependencias todo lo que
/// necesita la capa de API. Mantenerlos aqui evita que <c>Program.cs</c> crezca sin control.
/// </summary>
public static class ExtensionesServicios
{
    /// <summary>
    /// Registra los servicios propios de la capa de API (controladores, OpenAPI, etc.).
    /// </summary>
    public static IServiceCollection AgregarServiciosDeApi(this IServiceCollection servicios)
    {
        servicios.AddControllers();

        // Generacion del documento OpenAPI incluida en ASP.NET Core desde .NET 9.
        servicios.AddOpenApi();

        // ProblemDetails (RFC 9457) como formato unico de error de toda la API.
        // En la Fase 3 se anadira el manejador global de excepciones que lo usa.
        servicios.AddProblemDetails();

        return servicios;
    }
}
