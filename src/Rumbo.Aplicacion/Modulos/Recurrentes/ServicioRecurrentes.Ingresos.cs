using Microsoft.EntityFrameworkCore;

using Rumbo.Contratos.Movimientos;
using Rumbo.Contratos.Recurrentes;
using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Recurrentes;

/// <summary>
/// Parte del servicio de recurrentes dedicada a los ingresos.
/// </summary>
public partial class ServicioRecurrentes
{
    /// <inheritdoc />
    public async Task<IngresoRecurrenteDto> CrearIngresoAsync(
        SolicitudGuardarIngresoRecurrente solicitud,
        CancellationToken cancelacion = default)
    {
        var ingreso = new IngresoRecurrente { Nombre = string.Empty, Moneda = string.Empty };

        await AplicarAsync(ingreso, solicitud, cancelacion);

        contexto.IngresosRecurrentes.Add(ingreso);
        await contexto.SaveChangesAsync(cancelacion);

        return ProyectarIngreso(ingreso, await ObtenerHoyAsync(cancelacion));
    }

    /// <inheritdoc />
    public async Task<IngresoRecurrenteDto> ActualizarIngresoAsync(
        Guid id,
        SolicitudGuardarIngresoRecurrente solicitud,
        CancellationToken cancelacion = default)
    {
        var ingreso = await contexto.IngresosRecurrentes
            .FirstOrDefaultAsync(i => i.Id == id, cancelacion)
            ?? throw new ExcepcionNoEncontrado("el ingreso recurrente");

        await AplicarAsync(ingreso, solicitud, cancelacion);
        await contexto.SaveChangesAsync(cancelacion);

        return ProyectarIngreso(ingreso, await ObtenerHoyAsync(cancelacion));
    }

    /// <inheritdoc />
    public async Task EliminarIngresoAsync(Guid id, CancellationToken cancelacion = default)
    {
        var ingreso = await contexto.IngresosRecurrentes
            .FirstOrDefaultAsync(i => i.Id == id, cancelacion)
            ?? throw new ExcepcionNoEncontrado("el ingreso recurrente");

        contexto.IngresosRecurrentes.Remove(ingreso);
        await contexto.SaveChangesAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<MovimientoResumen> RegistrarCobroAsync(
        Guid id,
        SolicitudRegistrarPagoRecurrente solicitud,
        CancellationToken cancelacion = default)
    {
        var ingreso = await contexto.IngresosRecurrentes
            .FirstOrDefaultAsync(i => i.Id == id, cancelacion)
            ?? throw new ExcepcionNoEncontrado("el ingreso recurrente");

        if (ingreso.Estado != EstadoRecurrencia.Activa)
        {
            throw new ExcepcionDominio(
                $"«{ingreso.Nombre}» está {ingreso.Estado} y no admite cobros.");
        }

        var monto = solicitud.Monto ?? ingreso.MontoEstimado;
        var fecha = solicitud.Fecha ?? ingreso.ProximaFechaCobro;

        if (monto <= 0m)
        {
            throw new ExcepcionDominio("El importe del cobro debe ser mayor que cero.");
        }

        var movimiento = await movimientos.RegistrarAsync(
            new SolicitudRegistrarMovimiento(
                nameof(TipoMovimiento.Ingreso),
                ingreso.CuentaId,
                ingreso.CategoriaId,
                monto,
                ingreso.Moneda,
                fecha,
                ingreso.Nombre,
                null,
                null,
                nameof(TipoReparto.Compartido),
                solicitud.PagadoPorUsuarioId ?? ingreso.RecibidoPorUsuarioId,
                null),
            cancelacion);

        ingreso.ProximaFechaCobro = CalculadoraFrecuencia.Siguiente(
            ingreso.ProximaFechaCobro, ingreso.Frecuencia);

        await contexto.SaveChangesAsync(cancelacion);

        return movimiento;
    }
}
