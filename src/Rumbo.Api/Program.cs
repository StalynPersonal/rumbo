using System.Reflection;

using Rumbo.Api.Extensiones;
using Rumbo.Infraestructura.MultiEspacio;
using Rumbo.Infraestructura.Persistencia.Semilla;

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

constructor.Services.AgregarServiciosDeApi(constructor.Configuration);

var aplicacion = constructor.Build();

// Roles de plataforma y primer administrador. Sin el, nadie podria emitir la primera
// invitacion y el sistema quedaria inaccesible, porque Rumbo no tiene registro publico.
await SembradorInicial.SembrarAsync(aplicacion.Services);

// --- Tuberia de peticiones (middleware) ------------------------------------
// El ORDEN importa: cada middleware envuelve a los siguientes.

// El manejador de excepciones va PRIMERO: envuelve a todos los que vienen despues, de modo
// que cualquier fallo, venga de donde venga, sale como ProblemDetails y nunca como una
// traza de pila.
aplicacion.UseExceptionHandler();

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

// Orden obligatorio: primero se comprueba QUIEN es (autenticacion), despues sobre QUE
// espacio opera (resolucion), y solo entonces si PUEDE hacerlo (autorizacion). El middleware
// de espacio tiene que ir en medio, porque la autorizacion por permisos necesita el rol que
// el verifica contra la base de datos.
aplicacion.UseAuthentication();
aplicacion.UseMiddleware<MiddlewareResolucionEspacio>();
aplicacion.UseAuthorization();

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
