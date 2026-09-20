using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Calculadoras;
using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Aplicacion.Modulos.Movimientos;
using Rumbo.Contratos.Presupuestos;
using Rumbo.Dominio.Entidades.Planificacion;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Presupuestos;

/// <summary>
/// Presupuestos del espacio activo.
/// </summary>
/// <remarks>
/// Un presupuesto no mueve ni reserva dinero: es una intencion contra la que se comparan los
/// gastos reales. El consumo siempre se calcula desde el libro mayor.
/// </remarks>
/// <param name="contexto">Acceso a los datos.</param>
/// <param name="contextoEspacio">Espacio activo.</param>
/// <param name="fechaHora">Proveedor de fecha y hora.</param>
public partial class ServicioPresupuestos(
    IContextoRumbo contexto,
    IContextoEspacio contextoEspacio,
    IProveedorFechaHora fechaHora) : IServicioPresupuestos
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PresupuestoDetalle>> ListarAsync(
        bool soloVigente = false,
        CancellationToken cancelacion = default)
    {
        var hoy = await ObtenerHoyAsync(cancelacion);

        var consulta = contexto.Presupuestos
            .AsNoTracking()
            .Include(p => p.Lineas)
            .ThenInclude(l => l.Categoria)
            .AsQueryable();

        if (soloVigente)
        {
            consulta = consulta.Where(p =>
                p.Estado == EstadoPresupuesto.Activo
                && p.InicioPeriodo <= hoy
                && p.FinPeriodo >= hoy);
        }

        var presupuestos = await consulta
            .OrderByDescending(p => p.InicioPeriodo)
            .ToListAsync(cancelacion);

        var resultado = new List<PresupuestoDetalle>(presupuestos.Count);

        foreach (var presupuesto in presupuestos)
        {
            resultado.Add(await ProyectarAsync(presupuesto, hoy, cancelacion));
        }

        return resultado;
    }

    /// <inheritdoc />
    public async Task<PresupuestoDetalle> ObtenerAsync(
        Guid presupuestoId,
        CancellationToken cancelacion = default)
    {
        var presupuesto = await contexto.Presupuestos
            .AsNoTracking()
            .Include(p => p.Lineas)
            .ThenInclude(l => l.Categoria)
            .FirstOrDefaultAsync(p => p.Id == presupuestoId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("el presupuesto");

        return await ProyectarAsync(presupuesto, await ObtenerHoyAsync(cancelacion), cancelacion);
    }

    /// <summary>
    /// Calcula el consumo real de cada partida y arma el detalle.
    /// </summary>
    /// <param name="presupuesto">Presupuesto con sus lineas cargadas.</param>
    /// <param name="hoy">Fecha actual.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El detalle completo.</returns>
    /// <remarks>
    /// El gasto sale de <c>SoloGastos()</c>: si se contaran las transferencias, mover dinero
    /// a la cuenta de ahorro consumiria el presupuesto, que es justo lo contrario de lo que
    /// ocurre en realidad.
    /// </remarks>
    private async Task<PresupuestoDetalle> ProyectarAsync(
        Presupuesto presupuesto,
        DateOnly hoy,
        CancellationToken cancelacion)
    {
        var umbrales = await ObtenerUmbralesPorDefectoAsync(cancelacion);

        var categorias = presupuesto.Lineas.Select(l => l.CategoriaId).ToList();

        var gastos = await contexto.Movimientos
            .AsNoTracking()
            .SoloGastos()
            .EnRango(presupuesto.InicioPeriodo, presupuesto.FinPeriodo)
            .Where(m => m.CategoriaId != null && categorias.Contains(m.CategoriaId.Value))
            .GroupBy(m => m.CategoriaId!.Value)
            .Select(g => new { CategoriaId = g.Key, Total = g.Sum(m => m.MontoEnMonedaBase) })
            .ToDictionaryAsync(g => g.CategoriaId, g => g.Total, cancelacion);

        var lineas = new List<LineaPresupuestoDto>(presupuesto.Lineas.Count);

        foreach (var linea in presupuesto.Lineas.OrderBy(l => l.Categoria!.Nombre))
        {
            var gastado = gastos.TryGetValue(linea.CategoriaId, out var total) ? total : 0m;

            var estado = CalculadoraPresupuestos.Evaluar(
                linea.MontoAsignado,
                gastado,
                linea.UmbralAviso ?? umbrales.Aviso,
                linea.UmbralCritico ?? umbrales.Critico,
                linea.UmbralExcedido ?? umbrales.Excedido,
                presupuesto.InicioPeriodo,
                presupuesto.FinPeriodo,
                hoy);

            lineas.Add(new LineaPresupuestoDto(
                linea.Id,
                linea.CategoriaId,
                linea.Categoria?.Nombre ?? string.Empty,
                estado.MontoAsignado,
                estado.MontoGastado,
                estado.MontoDisponible,
                estado.PorcentajeConsumido,
                estado.Nivel.ToString(),
                estado.RitmoDiarioNecesario,
                estado.ProyeccionAlCierre));
        }

        var totalAsignado = lineas.Sum(l => l.MontoAsignado);
        var totalGastado = lineas.Sum(l => l.MontoGastado);

        var porcentaje = totalAsignado <= 0m
            ? (totalGastado > 0m ? 100m : 0m)
            : Math.Round(totalGastado / totalAsignado * 100m, 2, MidpointRounding.ToEven);

        return new PresupuestoDetalle(
            presupuesto.Id,
            presupuesto.Nombre,
            presupuesto.TipoPeriodo.ToString(),
            presupuesto.InicioPeriodo,
            presupuesto.FinPeriodo,
            presupuesto.Moneda,
            presupuesto.Estado.ToString(),
            totalAsignado,
            totalGastado,
            totalAsignado - totalGastado,
            porcentaje,
            presupuesto.InicioPeriodo <= hoy && presupuesto.FinPeriodo >= hoy,
            lineas);
    }

    /// <summary>Umbrales por defecto del espacio.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Los tres umbrales.</returns>
    private async Task<(decimal Aviso, decimal Critico, decimal Excedido)>
        ObtenerUmbralesPorDefectoAsync(CancellationToken cancelacion)
    {
        var espacioId = contextoEspacio.ObtenerEspacioObligatorio();

        var configuracion = await contexto.ConfiguracionesEspacio
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.EspacioId == espacioId, cancelacion);

        return configuracion is null
            ? (80m, 90m, 100m)
            : (configuracion.UmbralAvisoPresupuesto,
               configuracion.UmbralCriticoPresupuesto,
               configuracion.UmbralExcedidoPresupuesto);
    }

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
}
