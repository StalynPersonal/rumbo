using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using Rumbo.Dominio.Enums;
using Rumbo.Infraestructura.Persistencia;

namespace Rumbo.Infraestructura.MultiEspacio;

/// <summary>
/// Cachea la comprobacion de si un usuario tiene membresia activa en un espacio.
/// </summary>
/// <remarks>
/// <para>
/// El middleware de resolucion verifica la membresia en CADA peticion, porque el token
/// refleja la situacion del momento en que se emitio y podria estar desactualizado. Hacer esa
/// consulta siempre seria caro, asi que se cachea 30 segundos.
/// </para>
/// <para>
/// <b>La caché se invalida explicitamente</b> cuando cambia una membresia. Sin eso, suspender
/// a alguien tardaria hasta medio minuto en surtir efecto, y retirar un acceso es justo la
/// operacion que no debe hacerse esperar.
/// </para>
/// <para>
/// <b>Limitacion conocida:</b> la caché es de memoria, por instancia. Con varias instancias
/// en Azure, la invalidacion solo alcanza a la que atendio la peticion; en las demas el
/// cambio tardara los 30 segundos. Con una sola instancia, que es el escenario previsto, no
/// aplica. Si algun dia se escala horizontalmente, habra que sustituirla por una caché
/// distribuida o bajar mucho su duracion.
/// </para>
/// </remarks>
/// <param name="cache">Caché en memoria del proceso.</param>
public class CacheMembresias(IMemoryCache cache)
{
    /// <summary>Cuanto se conserva el resultado de una comprobacion.</summary>
    private static readonly TimeSpan Duracion = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Devuelve el rol del usuario en el espacio, o <c>null</c> si no tiene membresia activa.
    /// </summary>
    /// <param name="baseDatos">Contexto de base de datos.</param>
    /// <param name="usuarioId">Usuario que hace la peticion.</param>
    /// <param name="espacioId">Espacio que dice su token.</param>
    /// <returns>El rol verificado o <c>null</c>.</returns>
    public async Task<RolEspacio?> ObtenerRolVerificadoAsync(
        ContextoRumbo baseDatos,
        Guid usuarioId,
        Guid espacioId)
    {
        var clave = Clave(usuarioId, espacioId);

        if (cache.TryGetValue<RolEspacio?>(clave, out var enCache))
        {
            return enCache;
        }

        // MembresiasEspacio es una tabla GLOBAL, sin filtro de aislamiento: tiene que serlo,
        // porque se consulta justo para averiguar a que espacio puede entrar alguien.
        //
        // Se comprueba TAMBIEN que el espacio este activo. Sin esta condicion, suspender un
        // espacio no serviria de nada: sus miembros seguirian entrando porque su membresia
        // sigue siendo valida.
        var rol = await baseDatos.MembresiasEspacio
            .AsNoTracking()
            .Where(m => m.UsuarioId == usuarioId
                        && m.EspacioId == espacioId
                        && m.Estado == EstadoMembresia.Activa
                        && m.Espacio!.Estado == EstadoEspacio.Activo)
            .Select(m => (RolEspacio?)m.Rol)
            .FirstOrDefaultAsync();

        cache.Set(clave, rol, Duracion);

        return rol;
    }

    /// <summary>
    /// Olvida lo cacheado sobre una membresia, para que el proximo acceso vuelva a mirarlo
    /// en la base de datos.
    /// </summary>
    /// <param name="usuarioId">Usuario afectado.</param>
    /// <param name="espacioId">Espacio afectado.</param>
    /// <remarks>
    /// Debe llamarse SIEMPRE que cambie el rol o el estado de una membresia. Es lo que hace
    /// que suspender a alguien tenga efecto en la siguiente peticion y no medio minuto
    /// despues.
    /// </remarks>
    public void Invalidar(Guid usuarioId, Guid espacioId) => cache.Remove(Clave(usuarioId, espacioId));

    private static string Clave(Guid usuarioId, Guid espacioId) =>
        $"membresia:{usuarioId}:{espacioId}";
}
