using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Espacios;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;
using Rumbo.Infraestructura.MultiEspacio;
using Rumbo.Infraestructura.Persistencia;

namespace Rumbo.Infraestructura.Identidad;

/// <summary>
/// Gestion del espacio activo y de sus miembros.
/// </summary>
/// <remarks>
/// <para>
/// Vive en infraestructura porque necesita leer nombres y correos de la tabla de usuarios de
/// Identity, que es de esta capa.
/// </para>
/// <para>
/// <b>Todas las consultas filtran por el espacio activo de forma explicita</b>, ademas del
/// filtro global de EF Core. Puede parecer redundante, pero <c>Espacios</c> y
/// <c>MembresiasEspacio</c> son tablas GLOBALES, sin filtro: tienen que serlo, porque se
/// consultan justo para averiguar a que espacio puede entrar alguien. Aqui el aislamiento
/// depende de esta condicion escrita a mano, asi que no puede faltar.
/// </para>
/// </remarks>
/// <param name="contexto">Contexto de base de datos.</param>
/// <param name="cacheMembresias">Cache de comprobaciones de membresia.</param>
/// <param name="registro">Registro de eventos.</param>
public partial class ServicioEspacios(
    ContextoRumbo contexto,
    CacheMembresias cacheMembresias,
    ILogger<ServicioEspacios> registro) : IServicioEspacios
{
    /// <inheritdoc />
    public async Task<EspacioDetalle> ObtenerAsync(
        Guid espacioId,
        CancellationToken cancelacion = default) =>
        await contexto.Espacios
            .AsNoTracking()
            .Where(e => e.Id == espacioId)
            .Select(e => new EspacioDetalle(
                e.Id,
                e.Nombre,
                e.Tipo.ToString(),
                e.MonedaBase,
                e.ZonaHoraria,
                e.Estado.ToString(),
                e.FechaCreacion,
                e.Membresias.Count(m => m.Estado == EstadoMembresia.Activa)))
            .FirstOrDefaultAsync(cancelacion)
        ?? throw new ExcepcionNoEncontrado("el espacio");

    /// <inheritdoc />
    public async Task<EspacioDetalle> ActualizarAsync(
        Guid espacioId,
        SolicitudActualizarEspacio solicitud,
        CancellationToken cancelacion = default)
    {
        var espacio = await contexto.Espacios
            .FirstOrDefaultAsync(e => e.Id == espacioId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("el espacio");

        var nombre = solicitud.Nombre?.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ExcepcionDominio("El nombre del espacio no puede estar vacío.");
        }

        if (!Enum.TryParse<TipoEspacio>(solicitud.Tipo, ignoreCase: true, out var tipo))
        {
            throw new ExcepcionDominio(
                "El tipo debe ser Personal, Pareja, Familia o Negocio.");
        }

        espacio.Nombre = nombre;
        espacio.Tipo = tipo;

        await contexto.SaveChangesAsync(cancelacion);

        registro.LogInformation("Espacio {EspacioId} actualizado.", espacioId);

        return await ObtenerAsync(espacioId, cancelacion);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MiembroEspacio>> ListarMiembrosAsync(
        Guid espacioId,
        CancellationToken cancelacion = default)
    {
        var miembros = await ConsultarMiembrosAsync(espacioId, usuarioId: null, cancelacion);

        return miembros;
    }

    /// <summary>
    /// Reune las membresias del espacio con el nombre y el correo de cada persona.
    /// </summary>
    /// <param name="espacioId">Espacio activo.</param>
    /// <param name="usuarioId">Una persona concreta, o <c>null</c> para todas.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Los miembros proyectados al DTO.</returns>
    /// <remarks>
    /// <para>
    /// Se hace en DOS consultas y se combinan en memoria, en lugar de con un <c>join</c> de
    /// LINQ. Motivo: las entidades del dominio se refieren a las personas por su
    /// identificador y no tienen navegacion hacia <c>Usuario</c>, porque ese tipo pertenece
    /// a infraestructura. Sin navegacion, EF Core no logra traducir el <c>join</c> y falla
    /// en tiempo de ejecucion.
    /// </para>
    /// <para>
    /// El coste es irrelevante: un hogar tiene dos o tres personas, y una familia rara vez
    /// pasa de diez. Son dos consultas por indice sobre conjuntos diminutos.
    /// </para>
    /// </remarks>
    private async Task<List<MiembroEspacio>> ConsultarMiembrosAsync(
        Guid espacioId,
        Guid? usuarioId,
        CancellationToken cancelacion)
    {
        var consulta = contexto.MembresiasEspacio
            .AsNoTracking()
            .Where(m => m.EspacioId == espacioId);

        if (usuarioId.HasValue)
        {
            consulta = consulta.Where(m => m.UsuarioId == usuarioId.Value);
        }

        var membresias = await consulta
            .OrderBy(m => m.Rol)
            .ThenBy(m => m.FechaIngreso)
            .Select(m => new
            {
                m.UsuarioId,
                Rol = m.Rol.ToString(),
                Estado = m.Estado.ToString(),
                m.FechaIngreso,
            })
            .ToListAsync(cancelacion);

        if (membresias.Count == 0)
        {
            return [];
        }

        var identificadores = membresias.Select(m => m.UsuarioId).ToList();

        var personas = await contexto.Users
            .AsNoTracking()
            .Where(u => identificadores.Contains(u.Id))
            .Select(u => new { u.Id, u.NombreCompleto, u.Email })
            .ToDictionaryAsync(u => u.Id, cancelacion);

        return [.. membresias
            .Where(m => personas.ContainsKey(m.UsuarioId))
            .Select(m => new MiembroEspacio(
                m.UsuarioId,
                personas[m.UsuarioId].NombreCompleto,
                personas[m.UsuarioId].Email ?? string.Empty,
                m.Rol,
                m.Estado,
                m.FechaIngreso))];
    }
}
