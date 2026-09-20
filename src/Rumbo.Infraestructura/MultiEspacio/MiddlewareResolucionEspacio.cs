using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Rumbo.Dominio.Enums;
using Rumbo.Infraestructura.Identidad;
using Rumbo.Infraestructura.Persistencia;

namespace Rumbo.Infraestructura.MultiEspacio;

/// <summary>
/// Determina, en cada peticion, quien la hace y sobre que espacio opera.
/// </summary>
/// <remarks>
/// <para>
/// Es la PRIMERA capa del aislamiento multi-tenant, y la que alimenta a todas las demas: los
/// filtros globales de consulta y los interceptores de escritura leen el espacio de aqui.
/// </para>
/// <para>
/// <b>El espacio sale del token firmado, nunca de la peticion.</b> Si se aceptara de una
/// cabecera o del cuerpo, cualquiera podria leer las finanzas de otro hogar cambiando un
/// identificador.
/// </para>
/// <para>
/// <b>Y ademas se verifica contra la base de datos.</b> El token es de fiar (esta firmado),
/// pero refleja la situacion del momento en que se emitio. Si entre tanto se expulsa a
/// alguien del hogar, su token seguiria diciendo que pertenece a el durante quince minutos
/// mas. Comprobar la membresia en cada peticion hace que revocar un acceso surta efecto de
/// inmediato.
/// </para>
/// <para>
/// Esa comprobacion se cachea 30 segundos para no consultar la base en cada peticion. Es un
/// equilibrio deliberado: reduce mucho la carga y mantiene la revocacion practicamente
/// inmediata.
/// </para>
/// </remarks>
/// <param name="siguiente">Siguiente middleware de la tuberia.</param>
/// <param name="registro">Registro de eventos.</param>
public class MiddlewareResolucionEspacio(
    RequestDelegate siguiente,
    ILogger<MiddlewareResolucionEspacio> registro)
{
    /// <summary>Cuanto se conserva en cache el resultado de verificar una membresia.</summary>
    private static readonly TimeSpan DuracionCache = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Procesa la peticion, resolviendo el usuario y el espacio antes de continuar.
    /// </summary>
    /// <param name="contextoHttp">Contexto de la peticion.</param>
    /// <param name="contextoEspacio">Contexto de espacio de la peticion.</param>
    /// <param name="usuarioActual">Identidad del solicitante.</param>
    /// <param name="baseDatos">Contexto de base de datos.</param>
    /// <param name="cache">Cache en memoria para las membresias.</param>
    /// <returns>Tarea que finaliza cuando se procesa la peticion.</returns>
    public async Task InvokeAsync(
        HttpContext contextoHttp,
        ContextoEspacio contextoEspacio,
        UsuarioActual usuarioActual,
        ContextoRumbo baseDatos,
        IMemoryCache cache)
    {
        var principal = contextoHttp.User;

        if (principal.Identity?.IsAuthenticated != true)
        {
            // Peticion anonima: inicio de sesion, registro o el endpoint de salud. Se deja
            // pasar sin espacio; los filtros globales se encargan de que no vea nada.
            await siguiente(contextoHttp);
            return;
        }

        var usuarioId = LeerGuid(principal, ClaimTypes.NameIdentifier)
                        ?? LeerGuid(principal, "sub");

        if (usuarioId is null)
        {
            // Token autenticado pero sin identificador de usuario: esta mal formado.
            registro.LogWarning("Se recibió un token autenticado sin identificador de usuario.");

            await siguiente(contextoHttp);
            return;
        }

        var correo = principal.FindFirstValue(ClaimTypes.Email)
                     ?? principal.FindFirstValue("email");

        var esAdministrador = principal.IsInRole(RolesPlataforma.AdministradorPlataforma);

        var espacioDelToken = LeerGuid(principal, ClaimsRumbo.Espacio);

        RolEspacio? rolVerificado = null;

        if (espacioDelToken is not null)
        {
            rolVerificado = await ObtenerRolVerificadoAsync(
                baseDatos, cache, usuarioId.Value, espacioDelToken.Value);

            if (rolVerificado is null)
            {
                // El token dice que pertenece al espacio, pero la base de datos dice que no.
                // Lo habitual es que le hayan revocado el acceso hace un momento. Se sigue
                // adelante SIN espacio: las consultas no devolveran nada y las operaciones
                // que lo necesiten fallaran con un error claro.
                registro.LogWarning(
                    "El usuario {UsuarioId} presentó un token para el espacio {EspacioId} sin "
                    + "membresía activa. Se ignora el espacio.",
                    usuarioId, espacioDelToken);
            }
            else
            {
                contextoEspacio.Establecer(espacioDelToken.Value);
            }
        }

        usuarioActual.Establecer(usuarioId.Value, correo, rolVerificado, esAdministrador);

        await siguiente(contextoHttp);
    }

    /// <summary>
    /// Comprueba en la base de datos que la membresia esta activa y devuelve su rol.
    /// </summary>
    /// <returns>El rol, o <c>null</c> si no hay membresia activa.</returns>
    private static async Task<RolEspacio?> ObtenerRolVerificadoAsync(
        ContextoRumbo baseDatos,
        IMemoryCache cache,
        Guid usuarioId,
        Guid espacioId)
    {
        var clave = $"membresia:{usuarioId}:{espacioId}";

        if (cache.TryGetValue<RolEspacio?>(clave, out var enCache))
        {
            return enCache;
        }

        // MembresiasEspacio es una tabla global, sin filtro de aislamiento: tiene que serlo,
        // porque se consulta justo para averiguar a que espacio puede entrar alguien.
        var membresia = await baseDatos.MembresiasEspacio
            .AsNoTracking()
            .Where(m => m.UsuarioId == usuarioId
                        && m.EspacioId == espacioId
                        && m.Estado == EstadoMembresia.Activa)
            .Select(m => (RolEspacio?)m.Rol)
            .FirstOrDefaultAsync();

        cache.Set(clave, membresia, DuracionCache);

        return membresia;
    }

    /// <summary>Lee una reclamacion como <see cref="Guid"/>.</summary>
    /// <returns>El valor, o <c>null</c> si falta o no es un Guid valido.</returns>
    private static Guid? LeerGuid(ClaimsPrincipal principal, string tipoReclamacion) =>
        Guid.TryParse(principal.FindFirstValue(tipoReclamacion), out var valor) ? valor : null;
}
