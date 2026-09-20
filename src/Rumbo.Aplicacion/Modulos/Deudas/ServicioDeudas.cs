using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Deudas;
using Rumbo.Dominio.Entidades.Planificacion;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Deudas;

/// <summary>
/// Deudas del espacio activo y su avance.
/// </summary>
/// <remarks>
/// Una deuda no es una cuenta con saldo negativo: es una obligacion con su propio calendario,
/// su interes y su cuota. Por eso vive aparte del libro mayor, aunque cada pago si genere un
/// movimiento real.
/// </remarks>
/// <param name="contexto">Acceso a los datos.</param>
/// <param name="movimientos">Servicio del libro mayor, que registra los pagos.</param>
/// <param name="contextoEspacio">Espacio activo.</param>
public partial class ServicioDeudas(
    IContextoRumbo contexto,
    IServicioMovimientos movimientos,
    IContextoEspacio contextoEspacio) : IServicioDeudas
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<DeudaDetalle>> ListarAsync(
        bool incluirSaldadas = false,
        CancellationToken cancelacion = default)
    {
        var consulta = contexto.Deudas.AsNoTracking();

        if (!incluirSaldadas)
        {
            consulta = consulta.Where(d => d.Estado == EstadoDeuda.Activa);
        }

        var deudas = await consulta
            .OrderByDescending(d => d.SaldoActual)
            .ToListAsync(cancelacion);

        var resultado = new List<DeudaDetalle>(deudas.Count);

        foreach (var deuda in deudas)
        {
            resultado.Add(await ProyectarAsync(deuda, cancelacion));
        }

        return resultado;
    }

    /// <inheritdoc />
    public async Task<DeudaDetalle> ObtenerAsync(
        Guid deudaId,
        CancellationToken cancelacion = default)
    {
        var deuda = await contexto.Deudas
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == deudaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la deuda");

        return await ProyectarAsync(deuda, cancelacion);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PagoDeudaDto>> ListarPagosAsync(
        Guid deudaId,
        CancellationToken cancelacion = default)
    {
        var existe = await contexto.Deudas.AnyAsync(d => d.Id == deudaId, cancelacion);

        if (!existe)
        {
            throw new ExcepcionNoEncontrado("la deuda");
        }

        return await contexto.PagosDeuda
            .AsNoTracking()
            .Where(p => p.DeudaId == deudaId)
            .OrderByDescending(p => p.Fecha)
            .ThenByDescending(p => p.FechaCreacion)
            .Select(p => new PagoDeudaDto(
                p.Id, p.Fecha, p.MontoTotal, p.MontoCapital, p.MontoInteres,
                p.MontoCargos, p.Moneda, p.SaldoPosterior, p.MovimientoId, p.Notas))
            .ToListAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<PagoDeudaDto> PagarAsync(
        Guid deudaId,
        SolicitudPagarDeuda solicitud,
        CancellationToken cancelacion = default) =>
        await movimientos.PagarDeudaAsync(deudaId, solicitud, cancelacion);

    /// <summary>Convierte una deuda en su DTO, con lo que se lleva pagado.</summary>
    /// <param name="deuda">Deuda de origen.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El detalle de la deuda.</returns>
    private async Task<DeudaDetalle> ProyectarAsync(Deuda deuda, CancellationToken cancelacion)
    {
        var interesPagado = await contexto.PagosDeuda
            .AsNoTracking()
            .Where(p => p.DeudaId == deuda.Id)
            .SumAsync(p => p.MontoInteres, cancelacion);

        // La cuota con la que se estima el plazo restante: la habitual si existe, y si no el
        // pago minimo. Sin ninguna de las dos no se estima nada, porque inventar un plazo
        // seria peor que no dar ninguno.
        var cuota = deuda.PagoMensual ?? deuda.PagoMinimo;

        int? mesesRestantes = cuota is > 0m && deuda.SaldoActual > 0m
            ? (int)Math.Ceiling(deuda.SaldoActual / cuota.Value)
            : null;

        return new DeudaDetalle(
            deuda.Id,
            deuda.Nombre,
            deuda.Tipo.ToString(),
            deuda.Acreedor,
            deuda.MontoOriginal,
            deuda.SaldoActual,
            Math.Max(0m, deuda.MontoOriginal - deuda.SaldoActual),
            Math.Round(deuda.PorcentajePagado, 2, MidpointRounding.ToEven),
            deuda.Moneda,
            deuda.TasaInteres,
            deuda.PagoMinimo,
            deuda.PagoMensual,
            deuda.DiaVencimiento,
            deuda.FechaInicio,
            deuda.FechaEstimadaLiquidacion,
            mesesRestantes,
            interesPagado,
            deuda.Estado.ToString(),
            deuda.CuentaVinculadaId,
            deuda.ResponsableUsuarioId,
            deuda.Notas);
    }
}
