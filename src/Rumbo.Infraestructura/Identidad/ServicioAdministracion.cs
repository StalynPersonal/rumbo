using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Administracion;
using Rumbo.Contratos.Comun;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;
using Rumbo.Infraestructura.MultiEspacio;
using Rumbo.Infraestructura.Persistencia;

namespace Rumbo.Infraestructura.Identidad;

/// <summary>
/// Gestion de la plataforma: espacios, usuarios y metricas.
/// </summary>
/// <remarks>
/// <para>
/// <b>Ninguna operacion devuelve datos financieros.</b> Este servicio consulta espacios,
/// usuarios e invitaciones. No toca movimientos, cuentas, metas ni viajes, y no existe
/// ningun camino por el que pueda hacerlo.
/// </para>
/// <para>
/// Es el unico servicio autorizado a consultar tablas de varios espacios a la vez. Las que
/// usa (<c>Espacios</c>, <c>MembresiasEspacio</c>, <c>Usuarios</c>, <c>Invitaciones</c>) son
/// globales y no llevan filtro de aislamiento, asi que ni siquiera necesita saltarselo.
/// </para>
/// </remarks>
/// <param name="contexto">Contexto de base de datos.</param>
/// <param name="usuarios">Gestor de usuarios de Identity.</param>
/// <param name="cacheMembresias">Cache de comprobaciones de membresia.</param>
/// <param name="fechaHora">Proveedor de fecha y hora.</param>
/// <param name="registro">Registro de eventos.</param>
public partial class ServicioAdministracion(
    ContextoRumbo contexto,
    UserManager<Usuario> usuarios,
    CacheMembresias cacheMembresias,
    IProveedorFechaHora fechaHora,
    ILogger<ServicioAdministracion> registro) : IServicioAdministracion
{
    /// <summary>Tamano maximo de pagina admitido.</summary>
    private const int TamanoMaximoPagina = 200;

    /// <inheritdoc />
    public async Task<ResultadoPaginado<EspacioAdminResumen>> ListarEspaciosAsync(
        string? busqueda = null,
        int pagina = 1,
        int tamanoPagina = 50,
        CancellationToken cancelacion = default)
    {
        var numero = Math.Max(1, pagina);
        var tamano = Math.Clamp(tamanoPagina, 1, TamanoMaximoPagina);

        var consulta = contexto.Espacios.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            consulta = consulta.Where(e => EF.Functions.Like(e.Nombre, $"%{texto}%"));
        }

        var total = await consulta.CountAsync(cancelacion);

        var espacios = await consulta
            .OrderByDescending(e => e.FechaCreacion)
            .Skip((numero - 1) * tamano)
            .Take(tamano)
            .Select(e => new
            {
                e.Id,
                e.Nombre,
                e.Tipo,
                e.Estado,
                e.MonedaBase,
                e.FechaCreacion,
                Miembros = e.Membresias.Count(m => m.Estado == EstadoMembresia.Activa),
                PropietarioId = e.Membresias
                    .Where(m => m.Rol == RolEspacio.Propietario)
                    .Select(m => (Guid?)m.UsuarioId)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancelacion);

        // Los correos se resuelven aparte: el dominio no navega hacia el tipo Usuario, que
        // pertenece a infraestructura (ver D11 y D20).
        var propietarios = espacios
            .Where(e => e.PropietarioId.HasValue)
            .Select(e => e.PropietarioId!.Value)
            .Distinct()
            .ToList();

        var correos = await contexto.Users
            .AsNoTracking()
            .Where(u => propietarios.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToDictionaryAsync(u => u.Id, u => u.Email, cancelacion);

        var elementos = espacios
            .Select(e => new EspacioAdminResumen(
                e.Id,
                e.Nombre,
                e.Tipo.ToString(),
                e.Estado.ToString(),
                e.MonedaBase,
                e.FechaCreacion,
                e.Miembros,
                e.PropietarioId.HasValue && correos.TryGetValue(e.PropietarioId.Value, out var correo)
                    ? correo
                    : null))
            .ToList();

        return new ResultadoPaginado<EspacioAdminResumen>(elementos, numero, tamano, total);
    }

    /// <inheritdoc />
    public async Task<EspacioAdminResumen> CambiarEstadoEspacioAsync(
        Guid espacioId,
        SolicitudCambiarEstadoEspacio solicitud,
        CancellationToken cancelacion = default)
    {
        if (!Enum.TryParse<EstadoEspacio>(solicitud.Estado, ignoreCase: true, out var estado))
        {
            throw new ExcepcionDominio("El estado debe ser Activo o Suspendido.");
        }

        var espacio = await contexto.Espacios
            .FirstOrDefaultAsync(e => e.Id == espacioId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("el espacio");

        espacio.Estado = estado;
        await contexto.SaveChangesAsync(cancelacion);

        // Suspender debe cortar el acceso YA. Sin invalidar la cache, sus miembros seguirian
        // entrando durante 30 segundos.
        var miembros = await contexto.MembresiasEspacio
            .AsNoTracking()
            .Where(m => m.EspacioId == espacioId)
            .Select(m => m.UsuarioId)
            .ToListAsync(cancelacion);

        foreach (var usuarioId in miembros)
        {
            cacheMembresias.Invalidar(usuarioId, espacioId);
        }

        registro.LogWarning(
            "El espacio {EspacioId} pasó a estado {Estado}. Motivo: {Motivo}",
            espacioId, estado, solicitud.Motivo ?? "no indicado");

        var resultado = await ListarEspaciosAsync(espacio.Nombre, 1, 1, cancelacion);

        return resultado.Elementos.FirstOrDefault(e => e.Id == espacioId)
               ?? throw new ExcepcionNoEncontrado("el espacio");
    }

    /// <inheritdoc />
    public async Task<MetricasPlataforma> ObtenerMetricasAsync(
        CancellationToken cancelacion = default)
    {
        var haceUnMes = fechaHora.AhoraUtc.AddDays(-30);

        // Recuentos, nunca importes. Saber cuantos hogares hay es gestion; saber cuanto
        // dinero mueven seria entrar en sus finanzas.
        return new MetricasPlataforma(
            await contexto.Espacios.CountAsync(cancelacion),
            await contexto.Espacios.CountAsync(e => e.Estado == EstadoEspacio.Activo, cancelacion),
            await contexto.Users.CountAsync(cancelacion),
            await contexto.Users.CountAsync(u => u.Activo, cancelacion),
            await contexto.Users.CountAsync(u => u.UltimoAcceso >= haceUnMes, cancelacion),
            await contexto.Invitaciones.CountAsync(
                i => i.Estado == EstadoInvitacion.Pendiente, cancelacion));
    }
}
