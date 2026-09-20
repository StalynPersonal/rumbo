using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Calculadoras;
using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Aplicacion.Modulos.Movimientos;
using Rumbo.Contratos.Viajes;
using Rumbo.Dominio.Entidades.Planificacion;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Viajes;

/// <summary>
/// Viajes del espacio activo.
/// </summary>
/// <remarks>
/// <para>
/// Un viaje tiene dos caras que no hay que confundir. El <b>presupuesto</b> es lo que se
/// piensa gastar, desglosado en partidas. El <b>fondo</b> es el dinero que se lleva ahorrado
/// para pagarlo, y eso es una meta con su cuenta de ahorro.
/// </para>
/// <para>
/// Los gastos del viaje <b>sí</b> son gastos normales del hogar: se registran como cualquier
/// otro, solo que etiquetados con el viaje y su partida.
/// </para>
/// </remarks>
/// <param name="contexto">Acceso a los datos.</param>
/// <param name="analizador">Analizador del flujo de caja del hogar.</param>
/// <param name="contextoEspacio">Espacio activo.</param>
/// <param name="fechaHora">Proveedor de fecha y hora.</param>
public partial class ServicioViajes(
    IContextoRumbo contexto,
    AnalizadorFlujoCaja analizador,
    IContextoEspacio contextoEspacio,
    IProveedorFechaHora fechaHora) : IServicioViajes
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ViajeDetalle>> ListarAsync(
        bool incluirCerrados = false,
        CancellationToken cancelacion = default)
    {
        var consulta = contexto.Viajes.AsNoTracking().Include(v => v.Lineas);

        var viajes = incluirCerrados
            ? await consulta.OrderBy(v => v.FechaInicio).ToListAsync(cancelacion)
            : await consulta
                .Where(v => v.Estado == EstadoViaje.Planificado
                            || v.Estado == EstadoViaje.EnCurso)
                .OrderBy(v => v.FechaInicio)
                .ToListAsync(cancelacion);

        var hoy = await ObtenerHoyAsync(cancelacion);
        var resultado = new List<ViajeDetalle>(viajes.Count);

        foreach (var viaje in viajes)
        {
            resultado.Add(await ProyectarAsync(viaje, hoy, cancelacion));
        }

        return resultado;
    }

    /// <inheritdoc />
    public async Task<ViajeDetalle> ObtenerAsync(
        Guid viajeId,
        CancellationToken cancelacion = default)
    {
        var viaje = await contexto.Viajes
            .AsNoTracking()
            .Include(v => v.Lineas)
            .FirstOrDefaultAsync(v => v.Id == viajeId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("el viaje");

        return await ProyectarAsync(viaje, await ObtenerHoyAsync(cancelacion), cancelacion);
    }

    /// <inheritdoc />
    public async Task<ViabilidadViajeDto> AnalizarViabilidadAsync(
        Guid viajeId,
        CancellationToken cancelacion = default)
    {
        var viaje = await contexto.Viajes
            .AsNoTracking()
            .Include(v => v.Lineas)
            .FirstOrDefaultAsync(v => v.Id == viajeId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("el viaje");

        var espacioId = contextoEspacio.ObtenerEspacioObligatorio();

        var espacio = await contexto.Espacios
            .AsNoTracking()
            .Where(e => e.Id == espacioId)
            .Select(e => new { e.MonedaBase, e.ZonaHoraria })
            .FirstAsync(cancelacion);

        var configuracion = await contexto.ConfiguracionesEspacio
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.EspacioId == espacioId, cancelacion);

        var hoy = fechaHora.HoyEn(espacio.ZonaHoraria);

        var flujo = await analizador.AnalizarAsync(
            hoy,
            configuracion?.MesesHistorialParaAnalisis ?? 6,
            espacio.MonedaBase,
            cancelacion);

        var fondo = await ObtenerFondoAsync(viaje, cancelacion);

        var proyeccion = CalculadoraViajes.Proyectar(
            viaje.PresupuestoTotal,
            fondo,
            viaje.FechaInicio,
            hoy,
            flujo.DisponibleParaAhorro,
            flujo.ConfianzaBaja,
            viaje.Moneda);

        return new ViabilidadViajeDto(
            viaje.Id,
            viaje.Nombre,
            proyeccion.CostoTotal,
            proyeccion.FondoActual,
            proyeccion.Faltante,
            proyeccion.MesesHastaLaSalida,
            proyeccion.DiasHastaLaSalida,
            proyeccion.AporteMensualNecesario,
            proyeccion.DisponibleMensual,
            proyeccion.EsfuerzoRequerido,
            proyeccion.Veredicto,
            proyeccion.Explicacion,
            [.. proyeccion.Escenarios.Select(e => new EscenarioViajeDto(
                e.Nombre, e.AporteMensualSupuesto, e.PorcentajeDelDisponible,
                e.AhorroProyectado, e.Faltante, e.PorcentajeCubierto, e.Alcanza,
                e.MesesQueFaltarian))],
            proyeccion.FechaViableMasCercana,
            proyeccion.ConfianzaBaja,
            proyeccion.Moneda);
    }

    /// <summary>Convierte un viaje en su DTO, con el gasto real de cada partida.</summary>
    /// <param name="viaje">Viaje de origen.</param>
    /// <param name="hoy">Fecha actual en la zona horaria del espacio.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El detalle del viaje.</returns>
    private async Task<ViajeDetalle> ProyectarAsync(
        Viaje viaje,
        DateOnly hoy,
        CancellationToken cancelacion)
    {
        // El gasto real sale del libro mayor, agrupado por partida. Nunca de un contador
        // guardado en el viaje, que podria desincronizarse del dinero de verdad.
        var gastoPorPartida = await contexto.Movimientos
            .AsNoTracking()
            .SoloGastos()
            .Where(m => m.ViajeId == viaje.Id)
            .GroupBy(m => m.CategoriaViaje)
            .Select(g => new { Partida = g.Key, Total = g.Sum(m => m.Monto) })
            .ToListAsync(cancelacion);

        var totalGastado = gastoPorPartida.Sum(g => g.Total);

        var lineas = viaje.Lineas
            .OrderBy(l => l.Orden)
            .Select(l =>
            {
                var gastado = gastoPorPartida
                    .FirstOrDefault(g => g.Partida == l.Categoria)?.Total ?? 0m;

                var consumido = l.MontoPlanificado <= 0m
                    ? 0m
                    : Redondear(gastado / l.MontoPlanificado * 100m);

                return new LineaViajeDto(
                    l.Id,
                    l.Categoria.ToString(),
                    l.MontoPlanificado,
                    gastado,
                    Redondear(l.MontoPlanificado - gastado),
                    consumido,
                    l.Notas,
                    l.Orden);
            })
            .ToList();

        var fondo = await ObtenerFondoAsync(viaje, cancelacion);

        var financiado = viaje.PresupuestoTotal <= 0m
            ? 0m
            : Math.Min(100m, Redondear(fondo / viaje.PresupuestoTotal * 100m));

        var porViajero = viaje.NumeroViajeros <= 0
            ? viaje.PresupuestoTotal
            : Redondear(viaje.PresupuestoTotal / viaje.NumeroViajeros);

        return new ViajeDetalle(
            viaje.Id,
            viaje.Nombre,
            viaje.Destino,
            viaje.Descripcion,
            viaje.FechaInicio,
            viaje.FechaFin,
            viaje.DuracionEnDias,
            viaje.PresupuestoTotal,
            viaje.Moneda,
            viaje.NumeroViajeros,
            porViajero,
            viaje.Estado.ToString(),
            viaje.MetaId,
            fondo,
            financiado,
            totalGastado,
            lineas.Sum(l => l.MontoPlanificado),
            viaje.FechaInicio.DayNumber - hoy.DayNumber,
            lineas);
    }

    /// <summary>Devuelve lo ahorrado en la meta que financia el viaje.</summary>
    /// <param name="viaje">Viaje consultado.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El fondo, o cero si el viaje no tiene meta.</returns>
    /// <remarks>
    /// El fondo no se guarda en el viaje: se lee de la meta. Duplicarlo abriria la puerta a
    /// que las dos cifras dijeran cosas distintas sobre el mismo dinero.
    /// </remarks>
    private async Task<decimal> ObtenerFondoAsync(Viaje viaje, CancellationToken cancelacion)
    {
        if (viaje.MetaId is not { } metaId)
        {
            return 0m;
        }

        return await contexto.Metas
            .AsNoTracking()
            .Where(m => m.Id == metaId)
            .Select(m => (decimal?)m.MontoActual)
            .FirstOrDefaultAsync(cancelacion) ?? 0m;
    }

    /// <summary>Fecha de hoy en la zona horaria del espacio.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La fecha actual.</returns>
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

    /// <summary>Redondea a dos decimales con redondeo bancario.</summary>
    /// <param name="valor">Importe.</param>
    /// <returns>El importe redondeado.</returns>
    private static decimal Redondear(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.ToEven);
}
