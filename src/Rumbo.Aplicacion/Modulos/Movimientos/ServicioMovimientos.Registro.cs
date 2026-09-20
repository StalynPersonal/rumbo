using Microsoft.EntityFrameworkCore;

using Rumbo.Contratos.Movimientos;
using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Movimientos;

/// <summary>
/// Parte del servicio de movimientos que registra, modifica y elimina asientos.
/// </summary>
/// <remarks>
/// <b>Toda escritura ocurre dentro de una transaccion de base de datos</b> que abarca el
/// asiento y el saldo de la cuenta. Si se guardara el movimiento y fallara la actualizacion
/// del saldo, la instantanea quedaria descuadrada respecto al libro mayor, que es justo lo
/// que la reconciliacion tendria que ir a arreglar despues.
/// </remarks>
public partial class ServicioMovimientos
{
    /// <inheritdoc />
    public async Task<MovimientoResumen> RegistrarAsync(
        SolicitudRegistrarMovimiento solicitud,
        CancellationToken cancelacion = default)
    {
        if (!Enum.TryParse<TipoMovimiento>(solicitud.Tipo, ignoreCase: true, out var tipo))
        {
            throw new ExcepcionDominio("El tipo debe ser Ingreso, Gasto o Ajuste.");
        }

        // Una transferencia son DOS asientos y no puede crearse por aqui: si se admitiera,
        // aparecerian movimientos de tipo Transferencia sueltos, sin su pata contraria, y el
        // dinero surgiria o desapareceria de la nada.
        if (tipo == TipoMovimiento.Transferencia)
        {
            throw new ExcepcionDominio(
                "Las transferencias se registran con la operación de transferencia, que crea "
                + "los dos asientos a la vez.");
        }

        if (solicitud.Monto <= 0m)
        {
            throw new ExcepcionDominio(
                "El importe debe ser mayor que cero. La dirección la determina el tipo de "
                + "movimiento, no el signo del importe.");
        }

        var cuenta = await contexto.Cuentas
            .FirstOrDefaultAsync(c => c.Id == solicitud.CuentaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la cuenta");

        if (!cuenta.Activa)
        {
            throw new ExcepcionDominio(
                $"La cuenta «{cuenta.Nombre}» está desactivada y no admite movimientos nuevos.");
        }

        var categoria = await ValidarCategoriaAsync(solicitud.CategoriaId, tipo, cancelacion);

        if (solicitud.ViajeId.HasValue)
        {
            await VerificarQueElViajeExisteAsync(solicitud.ViajeId.Value, cancelacion);
        }

        var partidaDeViaje = LeerPartidaDeViaje(solicitud.CategoriaViaje, solicitud.ViajeId);

        var reparto = LeerReparto(solicitud.Reparto);
        var metodoPago = LeerMetodoPago(solicitud.MetodoPago);

        var moneda = string.IsNullOrWhiteSpace(solicitud.Moneda)
            ? cuenta.Moneda
            : solicitud.Moneda.Trim().ToUpperInvariant();

        var (montoEnBase, tasaAproximada) = await ConvertirAMonedaBaseAsync(
            solicitud.Monto, moneda, solicitud.FechaMovimiento, cancelacion);

        var movimiento = new Movimiento
        {
            Tipo = tipo,
            CuentaId = cuenta.Id,
            CategoriaId = categoria?.Id,
            Monto = solicitud.Monto,

            // El signo lo decide el servidor a partir del tipo. Si viniera del cliente,
            // bastaria enviar un gasto con signo positivo para inflar el saldo.
            Signo = ConsultasMovimientos.SignoPara(tipo),

            Moneda = moneda,
            MontoEnMonedaBase = montoEnBase,
            TasaEsAproximada = tasaAproximada,
            TasaCambioAplicada = solicitud.Monto == 0m ? 1m : montoEnBase / solicitud.Monto,
            FechaMovimiento = solicitud.FechaMovimiento,
            Descripcion = solicitud.Descripcion.Trim(),
            Notas = solicitud.Notas?.Trim(),
            MetodoPago = metodoPago,
            Reparto = reparto,
            PagadoPorUsuarioId = solicitud.PagadoPorUsuarioId ?? usuarioActual.UsuarioId,
            ViajeId = solicitud.ViajeId,
            CategoriaViaje = partidaDeViaje,
        };

        await contexto.EjecutarEnTransaccionAsync(async token =>
        {
            contexto.Movimientos.Add(movimiento);

            // El saldo se mueve en la MISMA transaccion que el asiento.
            cuenta.SaldoActual += movimiento.Monto * movimiento.Signo;

            return await contexto.SaveChangesAsync(token);
        }, cancelacion);

        return await ObtenerAsync(movimiento.Id, cancelacion);
    }

    /// <inheritdoc />
    public async Task EliminarAsync(Guid movimientoId, CancellationToken cancelacion = default)
    {
        var movimiento = await contexto.Movimientos
            .FirstOrDefaultAsync(m => m.Id == movimientoId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("el movimiento");

        // Una pata suelta de transferencia no puede borrarse por su cuenta: dejaria dinero
        // apareciendo o desapareciendo en la otra cuenta.
        if (movimiento.TransferenciaId.HasValue)
        {
            throw new ExcepcionDominio(
                "Este movimiento forma parte de una transferencia. Elimina la transferencia "
                + "completa para que se deshagan sus dos asientos a la vez.");
        }

        var cuenta = await contexto.Cuentas
            .FirstOrDefaultAsync(c => c.Id == movimiento.CuentaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la cuenta");

        await contexto.EjecutarEnTransaccionAsync(async token =>
        {
            // Se revierte el efecto sobre el saldo restando lo que en su dia se sumo.
            cuenta.SaldoActual -= movimiento.Monto * movimiento.Signo;

            // Remove se convierte en marca de borrado por el interceptor: la fila permanece
            // para la auditoria, pero desaparece de consultas y saldos.
            contexto.Movimientos.Remove(movimiento);

            return await contexto.SaveChangesAsync(token);
        }, cancelacion);
    }
}
