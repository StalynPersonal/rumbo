using Microsoft.EntityFrameworkCore;

using Rumbo.Contratos.Movimientos;
using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Movimientos;

/// <summary>
/// Parte del servicio de movimientos que traspasa dinero entre cuentas.
/// </summary>
/// <remarks>
/// <para>
/// <b>Un traspaso son SIEMPRE dos asientos</b>, la salida y la entrada, unidos por el mismo
/// identificador de transferencia. Asi el saldo de cualquier cuenta sigue siendo la simple
/// suma de sus movimientos, y excluir los traspasos de los informes es un unico filtro por
/// tipo.
/// </para>
/// <para>
/// <b>Un traspaso no es un gasto.</b> Mover RD$10,000 de la cuenta de nomina a la de ahorro
/// no es gastar RD$10,000: el dinero sigue en el hogar. Contarlo como gasto haria que el
/// informe del mes fuera falso, y que ahorrar pareciera empobrecer.
/// </para>
/// <para>
/// La comision SI es un gasto real y se registra aparte: ese dinero si sale del hogar.
/// </para>
/// </remarks>
public partial class ServicioMovimientos
{
    /// <inheritdoc />
    public async Task<TransferenciaResumen> TransferirAsync(
        SolicitudTransferir solicitud,
        CancellationToken cancelacion = default)
    {
        if (solicitud.Monto <= 0m)
        {
            throw new ExcepcionDominio("El importe de la transferencia debe ser mayor que cero.");
        }

        if (solicitud.CuentaOrigenId == solicitud.CuentaDestinoId)
        {
            throw new ExcepcionDominio(
                "El origen y el destino deben ser cuentas distintas. Transferir a la misma "
                + "cuenta no movería nada y dejaría dos asientos que se anulan.");
        }

        if (solicitud.Comision < 0m)
        {
            throw new ExcepcionDominio("La comisión no puede ser negativa.");
        }

        var origen = await contexto.Cuentas
            .FirstOrDefaultAsync(c => c.Id == solicitud.CuentaOrigenId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la cuenta de origen");

        var destino = await contexto.Cuentas
            .FirstOrDefaultAsync(c => c.Id == solicitud.CuentaDestinoId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la cuenta de destino");

        if (!origen.Activa || !destino.Activa)
        {
            throw new ExcepcionDominio(
                "Las dos cuentas deben estar activas para poder transferir entre ellas.");
        }

        Categoria? categoriaComision = null;

        if (solicitud.Comision > 0m)
        {
            categoriaComision = await ValidarCategoriaAsync(
                solicitud.CategoriaComisionId, TipoMovimiento.Gasto, cancelacion);
        }

        if (solicitud.MetaId.HasValue)
        {
            await VerificarQueLaMetaExisteAsync(solicitud.MetaId.Value, cancelacion);
        }

        var (montoDestino, tasa) = await CalcularMontoDestinoAsync(
            solicitud, origen, destino, cancelacion);

        var realizadoPor = solicitud.RealizadoPorUsuarioId ?? usuarioActual.UsuarioId;
        var descripcion = solicitud.Descripcion.Trim();

        var transferencia = new Transferencia
        {
            CuentaOrigenId = origen.Id,
            CuentaDestinoId = destino.Id,
            MontoOrigen = solicitud.Monto,
            MontoDestino = montoDestino,
            TasaCambioAplicada = tasa,
            Fecha = solicitud.Fecha,
            Descripcion = descripcion,
            Comision = solicitud.Comision,
            MovimientoOrigenId = Guid.Empty,
            MovimientoDestinoId = Guid.Empty,
        };

        var salida = await CrearAsientoDeTraspasoAsync(
            origen, solicitud.Monto, solicitud.Fecha, descripcion,
            esSalida: true, transferencia.Id, solicitud.MetaId, realizadoPor, cancelacion);

        var entrada = await CrearAsientoDeTraspasoAsync(
            destino, montoDestino, solicitud.Fecha, descripcion,
            esSalida: false, transferencia.Id, solicitud.MetaId, realizadoPor, cancelacion);

        transferencia.MovimientoOrigenId = salida.Id;
        transferencia.MovimientoDestinoId = entrada.Id;

        // Los dos asientos, los dos saldos, la comision y el aporte a la meta se aplican
        // como una sola unidad. Si algo fallara a mitad, no se aplica nada: dejar la salida
        // sin su entrada haria desaparecer dinero.
        await contexto.EjecutarEnTransaccionAsync(async token =>
        {
            contexto.Transferencias.Add(transferencia);
            contexto.Movimientos.AddRange(salida, entrada);

            origen.SaldoActual -= solicitud.Monto;
            destino.SaldoActual += montoDestino;

            if (solicitud.Comision > 0m && categoriaComision is not null)
            {
                var gastoComision = await CrearGastoDeComisionAsync(
                    origen, solicitud.Comision, solicitud.Fecha, categoriaComision.Id,
                    transferencia.Id, realizadoPor, token);

                contexto.Movimientos.Add(gastoComision);
                origen.SaldoActual -= solicitud.Comision;
            }

            if (solicitud.MetaId.HasValue)
            {
                await RegistrarAporteAMetaAsync(
                    solicitud.MetaId.Value, montoDestino, destino.Moneda,
                    solicitud.Fecha, entrada.Id, realizadoPor, token);
            }

            return await contexto.SaveChangesAsync(token);
        }, cancelacion);

        return new TransferenciaResumen(
            transferencia.Id,
            origen.Id, origen.Nombre,
            destino.Id, destino.Nombre,
            transferencia.MontoOrigen,
            transferencia.MontoDestino,
            transferencia.TasaCambioAplicada,
            transferencia.Fecha,
            transferencia.Descripcion,
            transferencia.Comision,
            salida.Id,
            entrada.Id);
    }

    /// <inheritdoc />
    public async Task EliminarTransferenciaAsync(
        Guid transferenciaId,
        CancellationToken cancelacion = default)
    {
        var transferencia = await contexto.Transferencias
            .FirstOrDefaultAsync(t => t.Id == transferenciaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la transferencia");

        // Se recuperan TODOS los asientos del traspaso, no solo los dos principales: asi la
        // comision, si la hubo, tambien se deshace.
        var asientos = await contexto.Movimientos
            .Where(m => m.TransferenciaId == transferenciaId)
            .ToListAsync(cancelacion);

        var identificadores = asientos.Select(a => a.CuentaId).Distinct().ToList();

        var cuentas = await contexto.Cuentas
            .Where(c => identificadores.Contains(c.Id))
            .ToListAsync(cancelacion);

        await contexto.EjecutarEnTransaccionAsync(async token =>
        {
            foreach (var asiento in asientos)
            {
                var cuenta = cuentas.First(c => c.Id == asiento.CuentaId);

                // Se revierte exactamente lo que cada asiento aplico en su dia.
                cuenta.SaldoActual -= asiento.Monto * asiento.Signo;

                contexto.Movimientos.Remove(asiento);
            }

            contexto.Transferencias.Remove(transferencia);

            return await contexto.SaveChangesAsync(token);
        }, cancelacion);
    }
}
