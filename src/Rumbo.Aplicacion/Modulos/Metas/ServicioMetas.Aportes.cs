using Microsoft.EntityFrameworkCore;

using Rumbo.Contratos.Metas;
using Rumbo.Contratos.Movimientos;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Metas;

/// <summary>
/// Parte del servicio de metas dedicada a los aportes.
/// </summary>
public partial class ServicioMetas
{
    /// <inheritdoc />
    public async Task<MetaDetalle> AportarAsync(
        Guid metaId,
        SolicitudAportarAMeta solicitud,
        CancellationToken cancelacion = default)
    {
        var meta = await contexto.Metas
            .FirstOrDefaultAsync(m => m.Id == metaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la meta");

        if (meta.Estado is EstadoMeta.Cancelada)
        {
            throw new ExcepcionDominio($"«{meta.Nombre}» está cancelada y no admite aportes.");
        }

        if (meta.CuentaVinculadaId is not { } cuentaDestinoId)
        {
            // El dinero tiene que estar en algun sitio. Una meta sin cuenta seria un numero
            // que sube sin que ningun saldo baje: el hogar creeria tener ese dinero dos veces.
            throw new ExcepcionDominio(
                $"«{meta.Nombre}» no tiene una cuenta de ahorro vinculada. Asígnale una antes "
                + "de aportar: el dinero tiene que estar en alguna cuenta real.");
        }

        if (solicitud.Monto <= 0m)
        {
            throw new ExcepcionDominio("El aporte debe ser mayor que cero.");
        }

        // El aporte es una TRANSFERENCIA hacia la cuenta de ahorro, no un gasto. Ahorrar no
        // es gastar.
        //
        // El servicio de movimientos crea los dos asientos, mueve los dos saldos, registra
        // el AporteMeta y actualiza el acumulado, todo en una sola transaccion.
        await movimientos.TransferirAsync(
            new SolicitudTransferir(
                solicitud.CuentaOrigenId,
                cuentaDestinoId,
                solicitud.Monto,
                null,
                solicitud.Fecha,
                string.IsNullOrWhiteSpace(solicitud.Descripcion)
                    ? $"Aporte a «{meta.Nombre}»"
                    : solicitud.Descripcion.Trim(),
                0m,
                null,
                metaId,
                null),
            cancelacion);

        if (solicitud.OrigenRecomendacion)
        {
            await MarcarAporteComoSugeridoAsync(metaId, solicitud.Fecha, cancelacion);
        }

        return await ObtenerAsync(metaId, cancelacion);
    }

    /// <summary>Marca el aporte recien creado como nacido de una recomendacion.</summary>
    /// <param name="metaId">Meta que recibio el aporte.</param>
    /// <param name="fecha">Fecha del aporte.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando se marca.</returns>
    /// <remarks>
    /// Permite medir despues si las recomendaciones sirven de algo, y deja constancia de que
    /// hubo una confirmacion explicita del usuario: el sistema nunca mueve dinero solo.
    /// </remarks>
    private async Task MarcarAporteComoSugeridoAsync(
        Guid metaId,
        DateOnly fecha,
        CancellationToken cancelacion)
    {
        var aporte = await contexto.AportesMeta
            .Where(a => a.MetaId == metaId && a.Fecha == fecha)
            .OrderByDescending(a => a.FechaCreacion)
            .FirstOrDefaultAsync(cancelacion);

        if (aporte is not null)
        {
            aporte.OrigenRecomendacion = true;
            await contexto.SaveChangesAsync(cancelacion);
        }
    }
}
