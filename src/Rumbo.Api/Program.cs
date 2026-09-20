using System.Reflection;

using Rumbo.Api.Extensiones;

// ---------------------------------------------------------------------------
//  Punto de entrada de la API de Rumbo.
//
//  El fichero se llama Program.cs porque asi lo espera el SDK de .NET; es de
//  los pocos nombres que no traducimos al espanol (ver docs/GLOSARIO.md).
//
//  Aqui solo se ARMA la aplicacion. La logica vive en las capas Aplicacion e
//  Infraestructura; este fichero se limita a conectar las piezas.
// ---------------------------------------------------------------------------

var constructor = WebApplication.CreateBuilder(args);

// --- Servicios (inyeccion de dependencias) ---------------------------------

constructor.Services.AgregarServiciosDeApi();

var aplicacion = constructor.Build();

// --- Tuberia de peticiones (middleware) ------------------------------------
// El ORDEN importa: cada middleware envuelve a los siguientes.

if (aplicacion.Environment.IsDevelopment() || aplicacion.Environment.IsStaging())
{
    // Documento OpenAPI en /openapi/v1.json (lo genera el propio ASP.NET Core).
    aplicacion.MapOpenApi();

    // Interfaz visual de Swagger en /swagger, apuntando a ese documento.
    aplicacion.UseSwaggerUI(opciones =>
    {
        opciones.SwaggerEndpoint("/openapi/v1.json", "Rumbo API v1");
        opciones.DocumentTitle = "Rumbo API";
    });
}

aplicacion.UseHttpsRedirection();

aplicacion.MapControllers();

// --- Endpoint de salud ------------------------------------------------------
// Sirve para comprobar que la API esta viva sin necesidad de autenticarse.
// Azure App Service tambien lo usara como health check en la Fase 10.
aplicacion.MapGet("/salud", () => Results.Ok(new
{
    estado = "Activa",
    aplicacion = "Rumbo API",
    version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "desconocida",
    ambiente = aplicacion.Environment.EnvironmentName,
    fechaHora = DateTimeOffset.UtcNow,
}))
.WithName("ConsultarSalud")
.WithTags("Salud");

aplicacion.Run();

/// <summary>
/// Declaracion explicita de la clase <c>Program</c> para que el proyecto de pruebas de
/// integracion pueda usarla con <c>WebApplicationFactory&lt;Program&gt;</c> (Fase 3).
/// </summary>
public partial class Program;
