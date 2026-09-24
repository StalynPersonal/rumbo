using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Rumbo.Api.Seguridad;
using Rumbo.Contratos.Aplicacion;

namespace Rumbo.Api.Controladores.V1;

/// <summary>
/// Información sobre la propia aplicación móvil.
/// </summary>
/// <remarks>
/// El APK se instala a mano, sin tienda, así que nadie avisa de que hay una versión nueva.
/// Este controlador es ese aviso.
/// </remarks>
/// <param name="opciones">Versiones publicadas.</param>
[ApiController]
[Route("api/v1/app")]
[Produces("application/json")]
public class AplicacionController(IOptions<OpcionesVersionApp> opciones) : ControllerBase
{
    /// <summary>Dice si la versión que usa la persona sigue siendo válida.</summary>
    /// <param name="version">Versión instalada, por ejemplo <c>1.0.3</c>.</param>
    /// <returns>Qué versión se espera y si conviene actualizar.</returns>
    /// <remarks>
    /// <para>
    /// Es <b>anónimo</b> a propósito. La aplicación necesita poder preguntarlo antes de que
    /// nadie inicie sesión: si la versión instalada ya no sirve, lo útil es decirlo en la
    /// pantalla de acceso, no después de que la persona se pelee con un error raro.
    /// </para>
    /// <para>
    /// El servidor <b>no</b> corta el acceso a las versiones antiguas: la v1 de la API sigue
    /// funcionando para todas. Dejar a alguien sin ver sus finanzas por no haber actualizado
    /// sería peor que la versión vieja.
    /// </para>
    /// </remarks>
    [HttpGet("version")]
    [AllowAnonymous]
    [ProducesResponseType<VersionAplicacion>(StatusCodes.Status200OK)]
    public ActionResult<VersionAplicacion> ConsultarVersion([FromQuery] string? version)
    {
        var configuracion = opciones.Value;

        var instalada = Interpretar(version);
        var minima = Interpretar(configuracion.VersionMinima);
        var recomendada = Interpretar(configuracion.VersionRecomendada);

        // Si no se envía versión, o no se entiende, no se marca nada como obligatorio: una
        // aplicación tan antigua que ni manda su versión se avisa, pero no se bloquea a
        // ciegas.
        var obligatoria = instalada is not null && minima is not null && instalada < minima;
        var hayNueva = instalada is not null && recomendada is not null && instalada < recomendada;

        return Ok(new VersionAplicacion(
            configuracion.VersionMinima,
            configuracion.VersionRecomendada,
            configuracion.UrlDescarga,
            obligatoria
                ? "Esta versión de Rumbo ya no está soportada. Instala la última para seguir."
                : hayNueva
                    ? "Hay una versión nueva de Rumbo disponible."
                    : null,
            obligatoria,
            hayNueva));
    }

    /// <summary>Convierte un texto de versión en algo comparable.</summary>
    /// <param name="texto">Versión en formato <c>1.2.3</c>.</param>
    /// <returns>La versión, o <c>null</c> si el texto no se entiende.</returns>
    private static Version? Interpretar(string? texto) =>
        Version.TryParse(texto, out var version) ? version : null;
}
