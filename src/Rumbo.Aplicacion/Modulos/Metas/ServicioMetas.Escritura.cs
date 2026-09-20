using Microsoft.EntityFrameworkCore;

using Rumbo.Contratos.Metas;
using Rumbo.Dominio.Entidades.Planificacion;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Metas;

/// <summary>
/// Parte del servicio de metas que crea, modifica y elimina.
/// </summary>
public partial class ServicioMetas
{
    /// <inheritdoc />
    public async Task<MetaDetalle> CrearAsync(
        SolicitudGuardarMeta solicitud,
        CancellationToken cancelacion = default)
    {
        var meta = new Meta { Nombre = string.Empty, Moneda = string.Empty };

        await AplicarAsync(meta, solicitud, esNueva: true, cancelacion);

        contexto.Metas.Add(meta);
        await contexto.SaveChangesAsync(cancelacion);

        return await ObtenerAsync(meta.Id, cancelacion);
    }

    /// <inheritdoc />
    public async Task<MetaDetalle> ActualizarAsync(
        Guid metaId,
        SolicitudGuardarMeta solicitud,
        CancellationToken cancelacion = default)
    {
        var meta = await contexto.Metas
            .FirstOrDefaultAsync(m => m.Id == metaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la meta");

        await AplicarAsync(meta, solicitud, esNueva: false, cancelacion);
        await contexto.SaveChangesAsync(cancelacion);

        return await ObtenerAsync(metaId, cancelacion);
    }

    /// <inheritdoc />
    public async Task<MetaDetalle> CambiarEstadoAsync(
        Guid metaId,
        SolicitudCambiarEstadoMeta solicitud,
        CancellationToken cancelacion = default)
    {
        if (!Enum.TryParse<EstadoMeta>(solicitud.Estado, ignoreCase: true, out var estado))
        {
            throw new ExcepcionDominio(
                "El estado debe ser Activa, Pausada, Alcanzada o Cancelada.");
        }

        var meta = await contexto.Metas
            .FirstOrDefaultAsync(m => m.Id == metaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la meta");

        meta.Estado = estado;

        if (estado == EstadoMeta.Alcanzada && meta.FechaAlcanzada is null)
        {
            meta.FechaAlcanzada = await ObtenerHoyAsync(cancelacion);
        }

        await contexto.SaveChangesAsync(cancelacion);

        return await ObtenerAsync(metaId, cancelacion);
    }

    /// <inheritdoc />
    public async Task EliminarAsync(Guid metaId, CancellationToken cancelacion = default)
    {
        var meta = await contexto.Metas
            .FirstOrDefaultAsync(m => m.Id == metaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la meta");

        var tieneAportes = await contexto.AportesMeta
            .AnyAsync(a => a.MetaId == metaId, cancelacion);

        if (tieneAportes)
        {
            // Esos aportes corresponden a movimientos reales que apuntan a esta meta.
            // Borrarla dejaria el historial sin sentido.
            throw new ExcepcionDominio(
                $"«{meta.Nombre}» ya tiene aportes registrados y no se puede eliminar. "
                + "Cancélala si ya no la quieres: desaparece de la vista y su historial se "
                + "conserva.");
        }

        contexto.Metas.Remove(meta);
        await contexto.SaveChangesAsync(cancelacion);
    }

    /// <summary>Vuelca la solicitud sobre la entidad, validando referencias.</summary>
    /// <param name="meta">Entidad que se rellena.</param>
    /// <param name="solicitud">Datos recibidos.</param>
    /// <param name="esNueva">Si la meta se esta creando.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando se aplican los datos.</returns>
    private async Task AplicarAsync(
        Meta meta,
        SolicitudGuardarMeta solicitud,
        bool esNueva,
        CancellationToken cancelacion)
    {
        var nombre = solicitud.Nombre?.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ExcepcionDominio("La meta necesita un nombre.");
        }

        if (solicitud.MontoObjetivo <= 0m)
        {
            throw new ExcepcionDominio("El monto objetivo debe ser mayor que cero.");
        }

        if (!Enum.TryParse<PrioridadMeta>(solicitud.Prioridad, ignoreCase: true, out var prioridad))
        {
            throw new ExcepcionDominio("La prioridad debe ser Baja, Media, Alta o Critica.");
        }

        if (solicitud.AporteMensualMinimo is < 0m)
        {
            throw new ExcepcionDominio("El aporte mensual comprometido no puede ser negativo.");
        }

        // Solo se exige fecha futura al CREAR. Al editar una meta antigua cuyo plazo ya
        // venció, obligar a cambiar la fecha impediría corregir cualquier otro dato.
        if (esNueva && solicitud.FechaObjetivo is { } fecha
            && fecha < await ObtenerHoyAsync(cancelacion))
        {
            throw new ExcepcionDominio("La fecha objetivo no puede estar en el pasado.");
        }

        if (solicitud.CuentaVinculadaId is { } cuentaId)
        {
            var cuenta = await contexto.Cuentas
                .FirstOrDefaultAsync(c => c.Id == cuentaId, cancelacion)
                ?? throw new ExcepcionNoEncontrado("la cuenta vinculada");

            if (!cuenta.Activa)
            {
                throw new ExcepcionDominio(
                    $"La cuenta «{cuenta.Nombre}» está desactivada y no puede recibir aportes.");
            }
        }

        meta.Nombre = nombre;
        meta.Descripcion = solicitud.Descripcion?.Trim();
        meta.MontoObjetivo = solicitud.MontoObjetivo;
        meta.Moneda = string.IsNullOrWhiteSpace(solicitud.Moneda)
            ? await ObtenerMonedaBaseAsync(cancelacion)
            : solicitud.Moneda.Trim().ToUpperInvariant();
        meta.FechaObjetivo = solicitud.FechaObjetivo;
        meta.Prioridad = prioridad;
        meta.AporteMensualMinimo = solicitud.AporteMensualMinimo;
        meta.CuentaVinculadaId = solicitud.CuentaVinculadaId;
        meta.Icono = solicitud.Icono?.Trim();
    }
}
