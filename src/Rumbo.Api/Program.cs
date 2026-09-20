using System.Reflection;

using Rumbo.Api.Extensiones;
using Rumbo.Api.Middleware;
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

// Las cabeceras de seguridad van muy pronto para que las lleve TODA respuesta, incluidas las
// de error que genera el manejador anterior.
aplicacion.UseMiddleware<MiddlewareCabecerasSeguridad>();

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

if (!aplicacion.Environment.IsDevelopment())
{
    // En desarrollo no: el certificado local no vale para nada fuera de la maquina, y HSTS
    // se queda pegado en el navegador durante un ano aunque despues se quite.
    aplicacion.UseHsts();
}

aplicacion.UseHttpsRedirection();

// El limitador va DESPUES de la autenticacion para poder repartir el cupo por usuario y no
// solo por IP, y ANTES de los controladores para rechazar el exceso sin tocar la base de
// datos: una peticion que se va a rechazar no deberia costar una consulta.

// Orden obligatorio: primero se comprueba QUIEN es (autenticacion), despues sobre QUE
// espacio opera (resolucion), y solo entonces si PUEDE hacerlo (autorizacion). El middleware
// de espacio tiene que ir en medio, porque la autorizacion por permisos necesita el rol que
// el verifica contra la base de datos.
aplicacion.UseAuthentication();
aplicacion.UseMiddleware<MiddlewareResolucionEspacio>();
aplicacion.UseAuthorization();

aplicacion.UseRateLimiter();

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
