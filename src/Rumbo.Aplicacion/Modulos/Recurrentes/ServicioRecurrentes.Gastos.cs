using Microsoft.EntityFrameworkCore;

using Rumbo.Contratos.Movimientos;
using Rumbo.Contratos.Recurrentes;
using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Recurrentes;

/// <summary>
/// Parte del servicio de recurrentes dedicada a los gastos.
/// </summary>
public partial class ServicioRecurrentes
{
    /// <inheritdoc />
    public async Task<GastoRecurrenteDto> CrearGastoAsync(
        SolicitudGuardarGastoRecurrente solicitud,
        CancellationToken cancelacion = default)
    {
        var gasto = new GastoRecurrente { Nombre = string.Empty, Moneda = string.Empty };

        await AplicarAsync(gasto, solicitud, cancelacion);

        contexto.GastosRecurrentes.Add(gasto);
        await contexto.SaveChangesAsync(cancelacion);

        return ProyectarGasto(gasto, await ObtenerHoyAsync(cancelacion));
    }

    /// <inheritdoc />
    public async Task<GastoRecurrenteDto> ActualizarGastoAsync(
        Guid id,
        SolicitudGuardarGastoRecurrente solicitud,
        CancellationToken cancelacion = default)
    {
        var gasto = await contexto.GastosRecurrentes
            .FirstOrDefaultAsync(g => g.Id == id, cancelacion)
            ?? throw new ExcepcionNoEncontrado("el gasto recurrente");

        await AplicarAsync(gasto, solicitud, cancelacion);
        await contexto.SaveChangesAsync(cancelacion);

        return ProyectarGasto(gasto, await ObtenerHoyAsync(cancelacion));
    }

    /// <inheritdoc />
    public async Task<GastoRecurrenteDto> CambiarEstadoGastoAsync(
        Guid id,
        string estado,
        CancellationToken cancelacion = default)
    {
        if (!Enum.TryParse<EstadoRecurrencia>(estado, ignoreCase: true, out var nuevo))
        {
            throw new ExcepcionDominio("El estado debe ser Activa, Pausada o Finalizada.");
        }

        var gasto = await contexto.GastosRecurrentes
            .FirstOrDefaultAsync(g => g.Id == id, cancelacion)
            ?? throw new ExcepcionNoEncontrado("el gasto recurrente");

        gasto.Estado = nuevo;
        await contexto.SaveChangesAsync(cancelacion);

        return ProyectarGasto(gasto, await ObtenerHoyAsync(cancelacion));
    }

    /// <inheritdoc />
    public async Task EliminarGastoAsync(Guid id, CancellationToken cancelacion = default)
    {
        var gasto = await contexto.GastosRecurrentes
            .FirstOrDefaultAsync(g => g.Id == id, cancelacion)
            ?? throw new ExcepcionNoEncontrado("el gasto recurrente");

        contexto.GastosRecurrentes.Remove(gasto);
        await contexto.SaveChangesAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<MovimientoResumen> RegistrarPagoAsync(
        Guid id,
        SolicitudRegistrarPagoRecurrente solicitud,
        CancellationToken cancelacion = default)
    {
        var gasto = await contexto.GastosRecurrentes
            .FirstOrDefaultAsync(g => g.Id == id, cancelacion)
            ?? throw new ExcepcionNoEncontrado("el gasto recurrente");

        if (gasto.Estado != EstadoRecurrencia.Activa)
        {
            throw new ExcepcionDominio(
                $"«{gasto.Nombre}» está {gasto.Estado} y no admite pagos. Actívalo primero.");
        }

        // El importe real manda sobre el estimado: en un recibo de luz casi nunca coinciden.
        var monto = solicitud.Monto ?? gasto.MontoEstimado;
        var fecha = solicitud.Fecha ?? gasto.ProximaFechaPago;

        if (monto <= 0m)
        {
            throw new ExcepcionDominio("El importe del pago debe ser mayor que cero.");
        }

        var movimiento = await movimientos.RegistrarAsync(
            new SolicitudRegistrarMovimiento(
                nameof(TipoMovimiento.Gasto),
                gasto.CuentaId,
                gasto.CategoriaId,
                monto,
                gasto.Moneda,
                fecha,
                gasto.Nombre,
                null,
                null,
                gasto.Reparto.ToString(),
                solicitud.PagadoPorUsuarioId,
                null),
            cancelacion);

        gasto.UltimaFechaPago = fecha;

        // La siguiente fecha se calcula desde la que VENCIA, no desde la del pago. Si se
        // pagara con tres dias de retraso cada mes, calcularla desde el pago iria corriendo
        // el vencimiento y en un ano el recibo cambiaria de semana.
        gasto.ProximaFechaPago = CalculadoraFrecuencia.Siguiente(
            gasto.ProximaFechaPago, gasto.Frecuencia);

        // Se enlaza el movimiento con la obligacion que lo origino, para poder consultar
        // despues el historial de pagos de ese servicio.
        var asiento = await contexto.Movimientos
            .FirstOrDefaultAsync(m => m.Id == movimiento.Id, cancelacion);

        if (asiento is not null)
        {
            asiento.GastoRecurrenteId = gasto.Id;
        }

        await contexto.SaveChangesAsync(cancelacion);

        return movimiento;
    }
}
