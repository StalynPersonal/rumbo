using Microsoft.EntityFrameworkCore;

using Rumbo.Contratos.Movimientos;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Movimientos;

/// <summary>
/// Parte del servicio de movimientos que modifica asientos ya registrados.
/// </summary>
public partial class ServicioMovimientos
{
    /// <inheritdoc />
    public async Task<MovimientoResumen> ActualizarAsync(
        Guid movimientoId,
        SolicitudActualizarMovimiento solicitud,
        CancellationToken cancelacion = default)
    {
        if (solicitud.Monto <= 0m)
        {
            throw new ExcepcionDominio("El importe debe ser mayor que cero.");
        }

        var movimiento = await contexto.Movimientos
            .FirstOrDefaultAsync(m => m.Id == movimientoId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("el movimiento");

        // Una pata de transferencia no se edita suelta: cambiarle el importe descuadraria
        // el traspaso, porque la otra pata seguiria con el importe anterior y el dinero
        // apareceria o desapareceria. Para corregir un traspaso hay que borrarlo y rehacerlo.
        if (movimiento.TransferenciaId.HasValue)
        {
            throw new ExcepcionDominio(
                "Este movimiento forma parte de una transferencia y no puede editarse por "
                + "separado. Elimina la transferencia y vuelve a registrarla.");
        }

        var categoria = await ValidarCategoriaAsync(
            solicitud.CategoriaId, movimiento.Tipo, cancelacion);

        if (solicitud.ViajeId.HasValue)
        {
            await VerificarQueElViajeExisteAsync(solicitud.ViajeId.Value, cancelacion);
        }

        var cuenta = await contexto.Cuentas
            .FirstOrDefaultAsync(c => c.Id == movimiento.CuentaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la cuenta");

        // Efecto que el movimiento tenia sobre el saldo ANTES de tocarlo.
        var efectoAnterior = movimiento.Monto * movimiento.Signo;

        var (montoEnBase, aproximada) = await ConvertirAMonedaBaseAsync(
            solicitud.Monto, movimiento.Moneda, solicitud.FechaMovimiento, cancelacion);

        movimiento.CategoriaId = categoria?.Id;
        movimiento.Monto = solicitud.Monto;
        movimiento.MontoEnMonedaBase = montoEnBase;
        movimiento.TasaEsAproximada = aproximada;
        movimiento.TasaCambioAplicada = solicitud.Monto == 0m ? 1m : montoEnBase / solicitud.Monto;
        movimiento.FechaMovimiento = solicitud.FechaMovimiento;
        movimiento.Descripcion = solicitud.Descripcion.Trim();
        movimiento.Notas = solicitud.Notas?.Trim();
        movimiento.MetodoPago = LeerMetodoPago(solicitud.MetodoPago);
        movimiento.Reparto = LeerReparto(solicitud.Reparto);
        movimiento.PagadoPorUsuarioId = solicitud.PagadoPorUsuarioId;
        movimiento.ViajeId = solicitud.ViajeId;

        // El signo no cambia porque el tipo tampoco puede cambiar.
        var efectoNuevo = movimiento.Monto * movimiento.Signo;

        await contexto.EjecutarEnTransaccionAsync(async token =>
        {
            // Se aplica solo la DIFERENCIA. Recalcular el saldo entero desde el libro mayor
            // seria mas lento y, sobre todo, pisaria los movimientos que otra persona del
            // hogar pudiera estar registrando en ese mismo instante.
            cuenta.SaldoActual += efectoNuevo - efectoAnterior;

            return await contexto.SaveChangesAsync(token);
        }, cancelacion);

        return await ObtenerAsync(movimientoId, cancelacion);
    }
}
