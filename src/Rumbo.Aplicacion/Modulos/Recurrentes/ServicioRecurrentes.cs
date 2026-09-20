using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Recurrentes;
using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Recurrentes;

/// <summary>
/// Obligaciones e ingresos que se repiten.
/// </summary>
/// <remarks>
/// <b>Rumbo nunca crea el movimiento por su cuenta al llegar la fecha.</b> Avisa, y el
/// movimiento se registra al confirmar el pago. Generarlo automaticamente haria que el saldo
/// dejara de reflejar la realidad en cuanto un pago se retrasara o cambiara de importe, que
/// es lo habitual en un recibo de luz.
/// </remarks>
/// <param name="contexto">Acceso a los datos.</param>
/// <param name="movimientos">Servicio del libro mayor, para registrar los pagos.</param>
/// <param name="contextoEspacio">Espacio activo.</param>
/// <param name="fechaHora">Proveedor de fecha y hora.</param>
public partial class ServicioRecurrentes(
    IContextoRumbo contexto,
    IServicioMovimientos movimientos,
    IContextoEspacio contextoEspacio,
    IProveedorFechaHora fechaHora) : IServicioRecurrentes
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<GastoRecurrenteDto>> ListarGastosAsync(
        bool incluirInactivos = false,
        CancellationToken cancelacion = default)
    {
        var consulta = contexto.GastosRecurrentes
            .AsNoTracking()
            .Include(g => g.Categoria)
            .Include(g => g.Cuenta)
            .AsQueryable();

        if (!incluirInactivos)
        {
            consulta = consulta.Where(g => g.Estado == EstadoRecurrencia.Activa);
        }

        var gastos = await consulta
            .OrderBy(g => g.ProximaFechaPago)
            .ToListAsync(cancelacion);

        var hoy = await ObtenerHoyAsync(cancelacion);

        return [.. gastos.Select(g => ProyectarGasto(g, hoy))];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IngresoRecurrenteDto>> ListarIngresosAsync(
        bool incluirInactivos = false,
        CancellationToken cancelacion = default)
    {
        var consulta = contexto.IngresosRecurrentes
            .AsNoTracking()
            .Include(i => i.Categoria)
            .Include(i => i.Cuenta)
            .AsQueryable();

        if (!incluirInactivos)
        {
            consulta = consulta.Where(i => i.Estado == EstadoRecurrencia.Activa);
        }

        var ingresos = await consulta
            .OrderBy(i => i.ProximaFechaCobro)
            .ToListAsync(cancelacion);

        var hoy = await ObtenerHoyAsync(cancelacion);

        return [.. ingresos.Select(i => ProyectarIngreso(i, hoy))];
    }

    /// <summary>Convierte un gasto recurrente en su DTO.</summary>
    /// <param name="gasto">Entidad.</param>
    /// <param name="hoy">Fecha de hoy en la zona horaria del espacio.</param>
    /// <returns>El DTO.</returns>
    private static GastoRecurrenteDto ProyectarGasto(GastoRecurrente gasto, DateOnly hoy) =>
        new(gasto.Id,
            gasto.Nombre,
            gasto.CategoriaId,
            gasto.Categoria?.Nombre ?? string.Empty,
            gasto.CuentaId,
            gasto.Cuenta?.Nombre ?? string.Empty,
            gasto.MontoEstimado,
            gasto.Moneda,
            gasto.EsMontoFijo,
            gasto.Frecuencia.ToString(),
            gasto.ProximaFechaPago,
            gasto.UltimaFechaPago,
            gasto.DiasAvisoPrevio,
            gasto.Estado.ToString(),
            gasto.Reparto.ToString(),

            // Negativo si ya vencio: asi la aplicacion puede destacar lo atrasado.
            gasto.ProximaFechaPago.DayNumber - hoy.DayNumber);

    /// <summary>Convierte un ingreso recurrente en su DTO.</summary>
    /// <param name="ingreso">Entidad.</param>
    /// <param name="hoy">Fecha de hoy en la zona horaria del espacio.</param>
    /// <returns>El DTO.</returns>
    private static IngresoRecurrenteDto ProyectarIngreso(IngresoRecurrente ingreso, DateOnly hoy) =>
        new(ingreso.Id,
            ingreso.Nombre,
            ingreso.CategoriaId,
            ingreso.Categoria?.Nombre ?? string.Empty,
            ingreso.CuentaId,
            ingreso.Cuenta?.Nombre ?? string.Empty,
            ingreso.MontoEstimado,
            ingreso.Moneda,
            ingreso.EsMontoFijo,
            ingreso.Frecuencia.ToString(),
            ingreso.ProximaFechaCobro,
            ingreso.RecibidoPorUsuarioId,
            ingreso.Estado.ToString(),
            ingreso.ProximaFechaCobro.DayNumber - hoy.DayNumber);

    /// <summary>Fecha de hoy en la zona horaria del espacio.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La fecha contable de hoy.</returns>
    private async Task<DateOnly> ObtenerHoyAsync(CancellationToken cancelacion)
    {
        var espacioId = contextoEspacio.ObtenerEspacioObligatorio();

        var zona = await contexto.Espacios
            .AsNoTracking()
            .Where(e => e.Id == espacioId)
            .Select(e => e.ZonaHoraria)
            .FirstOrDefaultAsync(cancelacion);

        return fechaHora.HoyEn(zona ?? "America/Santo_Domingo");
    }

    /// <summary>Interpreta la frecuencia recibida.</summary>
    /// <param name="valor">Texto del cliente.</param>
    /// <returns>El valor de la enumeracion.</returns>
    private static Frecuencia LeerFrecuencia(string? valor) =>
        Enum.TryParse<Frecuencia>(valor, ignoreCase: true, out var frecuencia)
            ? frecuencia
            : throw new ExcepcionDominio(
                "La frecuencia debe ser Semanal, Quincenal, Mensual, Bimestral, Trimestral, "
                + "Semestral o Anual.");
}
