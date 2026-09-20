using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Calculadoras;
using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Metas;
using Rumbo.Dominio.Entidades.Planificacion;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Metas;

/// <summary>
/// Metas de ahorro del espacio activo.
/// </summary>
/// <remarks>
/// Una meta no guarda dinero: el dinero esta siempre en una cuenta, y la meta es la etiqueta
/// que dice para que se esta guardando. Por eso aportar es una transferencia.
/// </remarks>
/// <param name="contexto">Acceso a los datos.</param>
/// <param name="movimientos">Servicio del libro mayor, para registrar los aportes.</param>
/// <param name="contextoEspacio">Espacio activo.</param>
/// <param name="fechaHora">Proveedor de fecha y hora.</param>
public partial class ServicioMetas(
    IContextoRumbo contexto,
    IServicioMovimientos movimientos,
    IContextoEspacio contextoEspacio,
    IProveedorFechaHora fechaHora) : IServicioMetas
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<MetaDetalle>> ListarAsync(
        bool incluirCerradas = false,
        CancellationToken cancelacion = default)
    {
        var consulta = contexto.Metas.AsNoTracking();

        if (!incluirCerradas)
        {
            consulta = consulta.Where(m =>
                m.Estado == EstadoMeta.Activa || m.Estado == EstadoMeta.Pausada);
        }

        var metas = await consulta
            .OrderByDescending(m => m.Prioridad)
            .ThenBy(m => m.FechaObjetivo)
            .ToListAsync(cancelacion);

        var hoy = await ObtenerHoyAsync(cancelacion);

        return [.. metas.Select(m => Proyectar(m, hoy))];
    }

    /// <inheritdoc />
    public async Task<MetaDetalle> ObtenerAsync(
        Guid metaId,
        CancellationToken cancelacion = default)
    {
        var meta = await contexto.Metas
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == metaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la meta");

        return Proyectar(meta, await ObtenerHoyAsync(cancelacion));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AporteMetaDto>> ListarAportesAsync(
        Guid metaId,
        CancellationToken cancelacion = default)
    {
        var existe = await contexto.Metas.AnyAsync(m => m.Id == metaId, cancelacion);

        if (!existe)
        {
            throw new ExcepcionNoEncontrado("la meta");
        }

        return await contexto.AportesMeta
            .AsNoTracking()
            .Where(a => a.MetaId == metaId)
            .OrderByDescending(a => a.Fecha)
            .ThenByDescending(a => a.FechaCreacion)
            .Select(a => new AporteMetaDto(
                a.Id, a.Monto, a.Moneda, a.Fecha, a.MovimientoId,
                a.AportadoPorUsuarioId, a.OrigenRecomendacion))
            .ToListAsync(cancelacion);
    }

    /// <summary>Convierte la meta en su DTO, calculando la proyeccion.</summary>
    /// <param name="meta">Entidad.</param>
    /// <param name="hoy">Fecha actual en la zona horaria del espacio.</param>
    /// <returns>El detalle con su proyeccion.</returns>
    private static MetaDetalle Proyectar(Meta meta, DateOnly hoy)
    {
        var proyeccion = CalculadoraMetas.Proyectar(
            meta.MontoObjetivo, meta.MontoActual, meta.FechaObjetivo, hoy);

        // Va atrasada si lo que la persona se comprometio a aportar no alcanza para llegar
        // a tiempo. Es la comparacion que convierte un numero en un aviso util.
        var atrasada = meta.AporteMensualMinimo is { } minimo
                       && proyeccion.AporteMensualNecesario is { } necesario
                       && necesario > minimo;

        return new MetaDetalle(
            meta.Id,
            meta.Nombre,
            meta.Descripcion,
            meta.MontoObjetivo,
            meta.MontoActual,
            proyeccion.MontoFaltante,
            proyeccion.PorcentajeCompletado,
            meta.Moneda,
            meta.FechaObjetivo,
            proyeccion.DiasRestantes,
            proyeccion.MesesRestantes,
            proyeccion.AporteMensualNecesario,
            proyeccion.AporteSemanalNecesario,
            meta.AporteMensualMinimo,
            atrasada,
            proyeccion.PlazoVencido,
            meta.Prioridad.ToString(),
            meta.Estado.ToString(),
            meta.CuentaVinculadaId,
            meta.Icono);
    }

    /// <summary>Fecha de hoy en la zona horaria del espacio.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La fecha contable de hoy.</returns>
    private async Task<DateOnly> ObtenerHoyAsync(CancellationToken cancelacion)
    {
        var espacioId = contextoEspacio.ObtenerEspacioObligatorio();

        var zona = await contexto.Espacios
            .AsNoTracking()
            .Where(e => e.Id == espacioId)
            .Select(e => e.ZonaHoraria)
            .FirstOrDefaultAsync(cancelacion);

        return fechaHora.HoyEn(zona ?? "America/Santo_Domingo");
    }

    /// <summary>Moneda base del espacio.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El codigo ISO-4217.</returns>
    private async Task<string> ObtenerMonedaBaseAsync(CancellationToken cancelacion)
    {
        var espacioId = contextoEspacio.ObtenerEspacioObligatorio();

        return await contexto.Espacios
            .AsNoTracking()
            .Where(e => e.Id == espacioId)
            .Select(e => e.MonedaBase)
            .FirstOrDefaultAsync(cancelacion) ?? "DOP";
    }
}
