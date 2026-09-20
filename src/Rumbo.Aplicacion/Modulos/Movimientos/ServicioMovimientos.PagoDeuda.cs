using Microsoft.EntityFrameworkCore;

using Rumbo.Contratos.Deudas;
using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Entidades.Planificacion;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Movimientos;

/// <summary>
/// Parte del servicio del libro mayor dedicada a los pagos de deuda.
/// </summary>
/// <remarks>
/// Vive aqui y no en el modulo de deudas porque un pago mueve el saldo de una cuenta, y toda
/// la logica que toca saldos esta en un solo sitio. Asi el asiento, el saldo de la cuenta, el
/// registro del pago y el saldo de la deuda se mueven en la <b>misma</b> transaccion: o pasa
/// todo o no pasa nada.
/// </remarks>
public partial class ServicioMovimientos
{
    /// <inheritdoc />
    public async Task<PagoDeudaDto> PagarDeudaAsync(
        Guid deudaId,
        SolicitudPagarDeuda solicitud,
        CancellationToken cancelacion = default)
    {
        var deuda = await contexto.Deudas
            .FirstOrDefaultAsync(d => d.Id == deudaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la deuda");

        if (deuda.Estado != EstadoDeuda.Activa)
        {
            throw new ExcepcionDominio(
                $"«{deuda.Nombre}» ya no está activa y no admite pagos nuevos.");
        }

        ValidarImportes(solicitud, deuda);

        var total = solicitud.MontoCapital + solicitud.MontoInteres + solicitud.MontoCargos;

        var cuenta = await contexto.Cuentas
            .FirstOrDefaultAsync(c => c.Id == solicitud.CuentaOrigenId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la cuenta de origen");

        if (!cuenta.Activa)
        {
            throw new ExcepcionDominio(
                $"La cuenta «{cuenta.Nombre}» está desactivada y no admite movimientos nuevos.");
        }

        var categoria = await ValidarCategoriaAsync(
            solicitud.CategoriaId, TipoMovimiento.Gasto, cancelacion);

        var (montoEnBase, tasaAproximada) = await ConvertirAMonedaBaseAsync(
            total, cuenta.Moneda, solicitud.Fecha, cancelacion);

        // El pago ES un gasto: el dinero sale de la cuenta de verdad. A diferencia de una
        // transferencia, no aparece en ningun otro sitio del hogar. Que la parte de capital
        // reduzca ademas una deuda no cambia que ese dinero ya no esta disponible este mes,
        // y el presupuesto familiar tiene que contarlo.
        var movimiento = new Movimiento
        {
            Tipo = TipoMovimiento.Gasto,
            CuentaId = cuenta.Id,
            CategoriaId = categoria?.Id,
            Monto = total,
            Signo = ConsultasMovimientos.SignoPara(TipoMovimiento.Gasto),
            Moneda = cuenta.Moneda,
            MontoEnMonedaBase = montoEnBase,
            TasaEsAproximada = tasaAproximada,
            TasaCambioAplicada = total == 0m ? 1m : montoEnBase / total,
            FechaMovimiento = solicitud.Fecha,
            Descripcion = string.IsNullOrWhiteSpace(solicitud.Descripcion)
                ? $"Pago de «{deuda.Nombre}»"
                : solicitud.Descripcion.Trim(),
            Notas = solicitud.Notas?.Trim(),
            Reparto = TipoReparto.Compartido,
            PagadoPorUsuarioId = usuarioActual.UsuarioId,
            DeudaId = deuda.Id,
        };

        var saldoPosterior = deuda.SaldoActual - solicitud.MontoCapital;

        var pago = new PagoDeuda
        {
            DeudaId = deuda.Id,
            MontoTotal = total,
            MontoCapital = solicitud.MontoCapital,
            MontoInteres = solicitud.MontoInteres,
            MontoCargos = solicitud.MontoCargos,
            Moneda = cuenta.Moneda,
            Fecha = solicitud.Fecha,
            SaldoPosterior = saldoPosterior,
            Notas = solicitud.Notas?.Trim(),
        };

        await contexto.EjecutarEnTransaccionAsync(async token =>
        {
            contexto.Movimientos.Add(movimiento);
            cuenta.SaldoActual += movimiento.Monto * movimiento.Signo;

            pago.MovimientoId = movimiento.Id;
            contexto.PagosDeuda.Add(pago);

            deuda.SaldoActual = saldoPosterior;

            // Cuando el capital llega a cero la deuda se cierra sola. Dejarla activa con
            // saldo cero obligaria a acordarse de cerrarla a mano, y la lista de deudas
            // acabaria llena de deudas que ya no existen.
            if (saldoPosterior <= 0m)
            {
                deuda.SaldoActual = 0m;
                deuda.Estado = EstadoDeuda.Saldada;
                deuda.FechaEstimadaLiquidacion = solicitud.Fecha;
            }

            return await contexto.SaveChangesAsync(token);
        }, cancelacion);

        return new PagoDeudaDto(
            pago.Id,
            pago.Fecha,
            pago.MontoTotal,
            pago.MontoCapital,
            pago.MontoInteres,
            pago.MontoCargos,
            pago.Moneda,
            pago.SaldoPosterior,
            pago.MovimientoId,
            pago.Notas);
    }

    /// <summary>Comprueba que los importes del pago tienen sentido.</summary>
    /// <param name="solicitud">Datos del pago.</param>
    /// <param name="deuda">Deuda que se paga.</param>
    private static void ValidarImportes(SolicitudPagarDeuda solicitud, Deuda deuda)
    {
        if (solicitud.MontoCapital < 0m || solicitud.MontoInteres < 0m
            || solicitud.MontoCargos < 0m)
        {
            throw new ExcepcionDominio("Los importes de un pago no pueden ser negativos.");
        }

        if (solicitud.MontoCapital + solicitud.MontoInteres + solicitud.MontoCargos <= 0m)
        {
            throw new ExcepcionDominio("El pago debe ser mayor que cero.");
        }

        if (solicitud.MontoCapital > deuda.SaldoActual)
        {
            // Aceptarlo dejaria la deuda en negativo, es decir, el banco debiendo dinero al
            // hogar. Casi siempre es un error de tecleo.
            throw new ExcepcionDominio(
                $"El capital del pago ({solicitud.MontoCapital:N2}) supera lo que queda por "
                + $"pagar de «{deuda.Nombre}» ({deuda.SaldoActual:N2}).");
        }
    }
}
