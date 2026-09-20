using Microsoft.EntityFrameworkCore;

using Rumbo.Contratos.Deudas;
using Rumbo.Dominio.Entidades.Planificacion;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Deudas;

/// <summary>
/// Parte del servicio de deudas que crea, modifica y elimina.
/// </summary>
public partial class ServicioDeudas
{
    /// <inheritdoc />
    public async Task<DeudaDetalle> CrearAsync(
        SolicitudGuardarDeuda solicitud,
        CancellationToken cancelacion = default)
    {
        var deuda = new Deuda { Nombre = string.Empty, Moneda = string.Empty };

        await AplicarAsync(deuda, solicitud, esNueva: true, cancelacion);

        contexto.Deudas.Add(deuda);
        await contexto.SaveChangesAsync(cancelacion);

        return await ObtenerAsync(deuda.Id, cancelacion);
    }

    /// <inheritdoc />
    public async Task<DeudaDetalle> ActualizarAsync(
        Guid deudaId,
        SolicitudGuardarDeuda solicitud,
        CancellationToken cancelacion = default)
    {
        var deuda = await contexto.Deudas
            .FirstOrDefaultAsync(d => d.Id == deudaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la deuda");

        await AplicarAsync(deuda, solicitud, esNueva: false, cancelacion);
        await contexto.SaveChangesAsync(cancelacion);

        return await ObtenerAsync(deudaId, cancelacion);
    }

    /// <inheritdoc />
    public async Task<DeudaDetalle> CambiarEstadoAsync(
        Guid deudaId,
        SolicitudCambiarEstadoDeuda solicitud,
        CancellationToken cancelacion = default)
    {
        if (!Enum.TryParse<EstadoDeuda>(solicitud.Estado, ignoreCase: true, out var estado))
        {
            throw new ExcepcionDominio("El estado debe ser Activa, Saldada o Refinanciada.");
        }

        var deuda = await contexto.Deudas
            .FirstOrDefaultAsync(d => d.Id == deudaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la deuda");

        deuda.Estado = estado;
        await contexto.SaveChangesAsync(cancelacion);

        return await ObtenerAsync(deudaId, cancelacion);
    }

    /// <inheritdoc />
    public async Task EliminarAsync(Guid deudaId, CancellationToken cancelacion = default)
    {
        var deuda = await contexto.Deudas
            .FirstOrDefaultAsync(d => d.Id == deudaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la deuda");

        var tienePagos = await contexto.PagosDeuda
            .AnyAsync(p => p.DeudaId == deudaId, cancelacion);

        if (tienePagos)
        {
            // Cada pago tiene detras un movimiento real. Borrar la deuda dejaria esos
            // gastos sin explicacion en el historial.
            throw new ExcepcionDominio(
                $"«{deuda.Nombre}» ya tiene pagos registrados y no se puede eliminar. "
                + "Cámbiale el estado a Saldada o Refinanciada.");
        }

        contexto.Deudas.Remove(deuda);
        await contexto.SaveChangesAsync(cancelacion);
    }

    /// <summary>Vuelca la solicitud sobre la entidad.</summary>
    /// <param name="deuda">Entidad que se rellena.</param>
    /// <param name="solicitud">Datos recibidos.</param>
    /// <param name="esNueva">Si la deuda se esta creando.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando se aplican los datos.</returns>
    private async Task AplicarAsync(
        Deuda deuda,
        SolicitudGuardarDeuda solicitud,
        bool esNueva,
        CancellationToken cancelacion)
    {
        var nombre = solicitud.Nombre?.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ExcepcionDominio("La deuda necesita un nombre.");
        }

        if (!Enum.TryParse<TipoDeuda>(solicitud.Tipo, ignoreCase: true, out var tipo))
        {
            throw new ExcepcionDominio(
                "El tipo debe ser TarjetaCredito, Prestamo, PrestamoPersonal, Vehiculo, "
                + "Hipoteca u Otra.");
        }

        if (solicitud.MontoOriginal <= 0m)
        {
            throw new ExcepcionDominio("El importe original debe ser mayor que cero.");
        }

        if (solicitud.SaldoActual < 0m)
        {
            throw new ExcepcionDominio("El saldo pendiente no puede ser negativo.");
        }

        if (solicitud.SaldoActual > solicitud.MontoOriginal)
        {
            // Casi siempre es un error de tecleo. Deber mas de lo que se pidio pasa con los
            // intereses capitalizados, pero entonces lo correcto es corregir el original.
            throw new ExcepcionDominio(
                "El saldo pendiente no puede superar al importe original de la deuda.");
        }

        if (solicitud.DiaVencimiento is < 1 or > 31)
        {
            throw new ExcepcionDominio("El día de vencimiento debe estar entre 1 y 31.");
        }

        if (solicitud.TasaInteres is < 0m)
        {
            throw new ExcepcionDominio("La tasa de interés no puede ser negativa.");
        }

        if (solicitud.CuentaVinculadaId is { } cuentaId)
        {
            var existe = await contexto.Cuentas.AnyAsync(c => c.Id == cuentaId, cancelacion);

            if (!existe)
            {
                throw new ExcepcionNoEncontrado("la cuenta vinculada");
            }
        }

        deuda.Nombre = nombre;
        deuda.Tipo = tipo;
        deuda.Acreedor = solicitud.Acreedor?.Trim();
        deuda.MontoOriginal = solicitud.MontoOriginal;
        deuda.TasaInteres = solicitud.TasaInteres;
        deuda.PagoMinimo = solicitud.PagoMinimo;
        deuda.PagoMensual = solicitud.PagoMensual;
        deuda.DiaVencimiento = solicitud.DiaVencimiento;
        deuda.FechaInicio = solicitud.FechaInicio;
        deuda.CuentaVinculadaId = solicitud.CuentaVinculadaId;
        deuda.ResponsableUsuarioId = solicitud.ResponsableUsuarioId;
        deuda.Notas = solicitud.Notas?.Trim();

        deuda.Moneda = string.IsNullOrWhiteSpace(solicitud.Moneda)
            ? await ObtenerMonedaBaseAsync(cancelacion)
            : solicitud.Moneda.Trim().ToUpperInvariant();

        // El saldo solo se fija al crear. Despues lo mueven los pagos, y solo ellos: si se
        // pudiera editar a mano, el saldo y el historial de pagos dirian cosas distintas y
        // no habria forma de saber cual es la buena.
        if (esNueva)
        {
            deuda.SaldoActual = solicitud.SaldoActual;
        }
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
