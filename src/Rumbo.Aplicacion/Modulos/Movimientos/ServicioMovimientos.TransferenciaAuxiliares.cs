using Microsoft.EntityFrameworkCore;

using Rumbo.Contratos.Movimientos;
using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Entidades.Planificacion;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Movimientos;

/// <summary>
/// Piezas que arman una transferencia.
/// </summary>
public partial class ServicioMovimientos
{
    /// <summary>
    /// Determina cuanto dinero llega a la cuenta destino y con que tasa.
    /// </summary>
    /// <param name="solicitud">Datos del traspaso.</param>
    /// <param name="origen">Cuenta de origen.</param>
    /// <param name="destino">Cuenta de destino.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El importe que entra y la tasa aplicada.</returns>
    /// <remarks>
    /// Entre cuentas de la misma moneda, entra exactamente lo que sale. Si las monedas
    /// difieren se admite indicar el importe exacto que llego, que es lo habitual porque es
    /// el que figura en el extracto, y la tasa se deduce de ambos importes. Se prefiere ese
    /// dato al calculado: la tasa que aplica un banco casi nunca coincide con la de
    /// referencia del dia.
    /// </remarks>
    private async Task<(decimal MontoDestino, decimal Tasa)> CalcularMontoDestinoAsync(
        SolicitudTransferir solicitud,
        Cuenta origen,
        Cuenta destino,
        CancellationToken cancelacion)
    {
        if (string.Equals(origen.Moneda, destino.Moneda, StringComparison.OrdinalIgnoreCase))
        {
            if (solicitud.MontoDestino.HasValue
                && solicitud.MontoDestino.Value != solicitud.Monto)
            {
                throw new ExcepcionDominio(
                    "Entre cuentas de la misma moneda debe entrar exactamente lo que sale. "
                    + "Si hubo una comisión, regístrala en su campo.");
            }

            return (solicitud.Monto, 1m);
        }

        if (solicitud.MontoDestino is > 0m)
        {
            return (solicitud.MontoDestino.Value, solicitud.MontoDestino.Value / solicitud.Monto);
        }

        var conversion = await conversor.ConvertirAsync(
            solicitud.Monto, origen.Moneda, destino.Moneda, solicitud.Fecha, cancelacion);

        return (conversion.Monto, conversion.Tasa);
    }

    /// <summary>Crea una de las dos patas del traspaso.</summary>
    /// <param name="cuenta">Cuenta afectada.</param>
    /// <param name="monto">Importe del asiento.</param>
    /// <param name="fecha">Fecha contable.</param>
    /// <param name="descripcion">Descripcion del traspaso.</param>
    /// <param name="esSalida">Si es la pata que resta saldo.</param>
    /// <param name="transferenciaId">Traspaso al que pertenece.</param>
    /// <param name="metaId">Meta a la que se destina, si aplica.</param>
    /// <param name="realizadoPor">Persona que ordena el traspaso.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El asiento listo para guardar.</returns>
    /// <remarks>
    /// Sin categoria a proposito: un traspaso no representa consumo ni ingreso, y
    /// clasificarlo ensuciaria todos los informes por categoria.
    /// </remarks>
    private async Task<Movimiento> CrearAsientoDeTraspasoAsync(
        Cuenta cuenta,
        decimal monto,
        DateOnly fecha,
        string descripcion,
        bool esSalida,
        Guid transferenciaId,
        Guid? metaId,
        Guid? realizadoPor,
        CancellationToken cancelacion)
    {
        var (montoEnBase, aproximada) = await ConvertirAMonedaBaseAsync(
            monto, cuenta.Moneda, fecha, cancelacion);

        return new Movimiento
        {
            Tipo = TipoMovimiento.Transferencia,
            CuentaId = cuenta.Id,
            CategoriaId = null,
            Monto = monto,
            Signo = ConsultasMovimientos.SignoPara(TipoMovimiento.Transferencia, esSalida),
            Moneda = cuenta.Moneda,
            MontoEnMonedaBase = montoEnBase,
            TasaEsAproximada = aproximada,
            TasaCambioAplicada = monto == 0m ? 1m : montoEnBase / monto,
            FechaMovimiento = fecha,
            Descripcion = esSalida ? $"{descripcion} (salida)" : $"{descripcion} (entrada)",
            Reparto = TipoReparto.Compartido,
            PagadoPorUsuarioId = realizadoPor,
            TransferenciaId = transferenciaId,
            MetaId = metaId,
        };
    }

    /// <summary>Crea el gasto correspondiente a la comision del traspaso.</summary>
    /// <param name="cuenta">Cuenta de la que se cobra.</param>
    /// <param name="comision">Importe de la comision.</param>
    /// <param name="fecha">Fecha contable.</param>
    /// <param name="categoriaId">Categoria del gasto.</param>
    /// <param name="transferenciaId">Traspaso que la origino.</param>
    /// <param name="realizadoPor">Persona que ordena el traspaso.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El asiento de gasto.</returns>
    /// <remarks>
    /// Se registra como GASTO de verdad, no como parte del traspaso: ese dinero sale del
    /// hogar y no vuelve. Queda ligado a la transferencia para que borrarla lo deshaga
    /// tambien.
    /// </remarks>
    private async Task<Movimiento> CrearGastoDeComisionAsync(
        Cuenta cuenta,
        decimal comision,
        DateOnly fecha,
        Guid categoriaId,
        Guid transferenciaId,
        Guid? realizadoPor,
        CancellationToken cancelacion)
    {
        var (montoEnBase, aproximada) = await ConvertirAMonedaBaseAsync(
            comision, cuenta.Moneda, fecha, cancelacion);

        return new Movimiento
        {
            Tipo = TipoMovimiento.Gasto,
            CuentaId = cuenta.Id,
            CategoriaId = categoriaId,
            Monto = comision,
            Signo = ConsultasMovimientos.SignoPara(TipoMovimiento.Gasto),
            Moneda = cuenta.Moneda,
            MontoEnMonedaBase = montoEnBase,
            TasaEsAproximada = aproximada,
            TasaCambioAplicada = comision == 0m ? 1m : montoEnBase / comision,
            FechaMovimiento = fecha,
            Descripcion = "Comisión por transferencia",
            Reparto = TipoReparto.Compartido,
            PagadoPorUsuarioId = realizadoPor,
            TransferenciaId = transferenciaId,
        };
    }

    /// <summary>Comprueba que la meta pertenece al espacio activo y admite aportes.</summary>
    /// <param name="metaId">Meta indicada.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando termina la comprobacion.</returns>
    private async Task VerificarQueLaMetaExisteAsync(Guid metaId, CancellationToken cancelacion)
    {
        var estado = await contexto.Metas
            .Where(m => m.Id == metaId)
            .Select(m => (EstadoMeta?)m.Estado)
            .FirstOrDefaultAsync(cancelacion)
            ?? throw new ExcepcionNoEncontrado("la meta");

        if (estado is EstadoMeta.Cancelada)
        {
            throw new ExcepcionDominio("Esa meta está cancelada y no admite aportes.");
        }
    }

    /// <summary>Deja constancia del aporte y actualiza el acumulado de la meta.</summary>
    /// <param name="metaId">Meta que recibe el dinero.</param>
    /// <param name="monto">Importe aportado.</param>
    /// <param name="moneda">Moneda del aporte.</param>
    /// <param name="fecha">Fecha contable.</param>
    /// <param name="movimientoId">Asiento que movio el dinero.</param>
    /// <param name="aportadoPor">Persona que aporta.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando se registra el aporte.</returns>
    /// <remarks>
    /// El aporte NO crea dinero: apunta al movimiento real que lo movio. Esta tabla existe
    /// para poder responder de donde salieron los RD$50,000 de una meta sin tener que
    /// recorrer todo el libro mayor.
    /// </remarks>
    private async Task RegistrarAporteAMetaAsync(
        Guid metaId,
        decimal monto,
        string moneda,
        DateOnly fecha,
        Guid movimientoId,
        Guid? aportadoPor,
        CancellationToken cancelacion)
    {
        var meta = await contexto.Metas.FirstAsync(m => m.Id == metaId, cancelacion);

        contexto.AportesMeta.Add(new AporteMeta
        {
            MetaId = metaId,
            MovimientoId = movimientoId,
            Monto = monto,
            Moneda = moneda,
            Fecha = fecha,
            AportadoPorUsuarioId = aportadoPor,
        });

        // MontoActual es una instantanea, igual que el saldo de una cuenta: la verdad es la
        // suma de los aportes, y esto se mantiene al dia para no recalcularla en cada
        // apertura del panel.
        meta.MontoActual += monto;

        if (meta.MontoActual >= meta.MontoObjetivo && meta.Estado == EstadoMeta.Activa)
        {
            meta.Estado = EstadoMeta.Alcanzada;
            meta.FechaAlcanzada = fecha;
        }
    }
}
