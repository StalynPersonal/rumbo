using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Panel;
using Rumbo.Contratos.Reportes;
using Rumbo.Dominio.Enums;

namespace Rumbo.Aplicacion.Modulos.Panel;

/// <summary>
/// Arma la pantalla de inicio del hogar en una sola respuesta.
/// </summary>
/// <remarks>
/// <para>
/// No calcula nada por su cuenta: reutiliza los servicios que ya existen. Si el panel
/// recalculara los presupuestos o las metas con su propia aritmetica, tarde o temprano
/// mostraria una cifra distinta a la de la pantalla de presupuestos, y entonces ninguna de
/// las dos seria creible.
/// </para>
/// <para>
/// Devuelve todo junto a proposito: la aplicacion movil se abre con una llamada en lugar de
/// ocho. En una conexion lenta, ocho peticiones son ocho oportunidades de que la pantalla se
/// quede a medias.
/// </para>
/// </remarks>
/// <param name="contexto">Acceso a los datos.</param>
/// <param name="reportes">Servicio de informes.</param>
/// <param name="presupuestos">Servicio de presupuestos.</param>
/// <param name="metas">Servicio de metas.</param>
/// <param name="recomendaciones">Servicio de recomendaciones.</param>
/// <param name="contextoEspacio">Espacio activo.</param>
/// <param name="fechaHora">Proveedor de fecha y hora.</param>
public class ServicioPanel(
    IContextoRumbo contexto,
    IServicioReportes reportes,
    IServicioPresupuestos presupuestos,
    IServicioMetas metas,
    IServicioRecomendaciones recomendaciones,
    IContextoEspacio contextoEspacio,
    IProveedorFechaHora fechaHora) : IServicioPanel
{
    /// <summary>Dias hacia delante en los que se buscan compromisos.</summary>
    /// <remarks>
    /// Quince dias: lo bastante para reaccionar y lo bastante poco para que la lista quepa
    /// en una pantalla y siga significando «esto es inminente».
    /// </remarks>
    private const int DiasDeHorizonte = 15;

    /// <summary>Cuantas categorias de gasto se muestran en el panel.</summary>
    private const int MayoresGastosQueSeMuestran = 5;

    /// <inheritdoc />
    public async Task<PanelInicio> ObtenerAsync(CancellationToken cancelacion = default)
    {
        var espacioId = contextoEspacio.ObtenerEspacioObligatorio();

        var espacio = await contexto.Espacios
            .AsNoTracking()
            .Where(e => e.Id == espacioId)
            .Select(e => new { e.Nombre, e.MonedaBase, e.ZonaHoraria })
            .FirstAsync(cancelacion);

        var hoy = fechaHora.HoyEn(espacio.ZonaHoraria);

        var inicioDeMes = new DateOnly(hoy.Year, hoy.Month, 1);
        var finDeMes = inicioDeMes.AddMonths(1).AddDays(-1);
        var inicioMesAnterior = inicioDeMes.AddMonths(-1);
        var finMesAnterior = inicioDeMes.AddDays(-1);

        var mesEnCurso = await reportes.ResumirAsync(
            new FiltroReporte(inicioDeMes, finDeMes, null, null, null, null), cancelacion);

        var mesAnterior = await reportes.ResumirAsync(
            new FiltroReporte(inicioMesAnterior, finMesAnterior, null, null, null, null),
            cancelacion);

        var porCategoria = await reportes.PorCategoriaAsync(
            new FiltroReporte(inicioDeMes, finDeMes, null, null, null, null), cancelacion);

        return new PanelInicio(
            hoy,
            espacio.Nombre,
            espacio.MonedaBase,
            await ResumirPatrimonioAsync(cancelacion),
            mesEnCurso,
            mesAnterior,
            [.. porCategoria.Categorias.Take(MayoresGastosQueSeMuestran)],
            await ObtenerAlertasAsync(cancelacion),
            await ObtenerCompromisosAsync(hoy, cancelacion),
            await ObtenerMetasAsync(cancelacion),
            await recomendaciones.ListarAsync(incluirRespondidas: false, cancelacion),
            await ContarNotificacionesSinLeerAsync(cancelacion));
    }

    /// <summary>Consolida cuentas y deudas.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El resumen del patrimonio.</returns>
    /// <remarks>
    /// El patrimonio neto resta las deudas. Mostrar solo el saldo de las cuentas daria una
    /// sensacion de holgura que no se corresponde con la realidad de un hogar endeudado.
    /// </remarks>
    private async Task<ResumenPatrimonio> ResumirPatrimonioAsync(CancellationToken cancelacion)
    {
        var cuentas = await contexto.Cuentas
            .AsNoTracking()
            .Where(c => c.Activa)
            .Select(c => new { c.Tipo, c.SaldoActual })
            .ToListAsync(cancelacion);

        var deudas = await contexto.Deudas
            .AsNoTracking()
            .Where(d => d.Estado == EstadoDeuda.Activa)
            .SumAsync(d => d.SaldoActual, cancelacion);

        var total = cuentas.Sum(c => c.SaldoActual);

        var enAhorro = cuentas
            .Where(c => c.Tipo is TipoCuenta.Ahorro or TipoCuenta.Inversion)
            .Sum(c => c.SaldoActual);

        return new ResumenPatrimonio(
            total, enAhorro, deudas, total - deudas, cuentas.Count);
    }

    /// <summary>Partidas de presupuesto que necesitan atencion.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Las alertas, de la mas consumida a la menos.</returns>
    private async Task<IReadOnlyList<AlertaPresupuesto>> ObtenerAlertasAsync(
        CancellationToken cancelacion)
    {
        var vigentes = await presupuestos.ListarAsync(soloVigente: true, cancelacion);

        return
        [
            .. vigentes
                .SelectMany(p => p.Lineas.Select(l => new { Presupuesto = p, Linea = l }))
                .Where(x => x.Linea.Nivel != "Normal")
                .OrderByDescending(x => x.Linea.PorcentajeConsumido)
                .Select(x => new AlertaPresupuesto(
                    x.Presupuesto.Id,
                    x.Linea.NombreCategoria,
                    x.Linea.MontoAsignado,
                    x.Linea.MontoGastado,
                    x.Linea.PorcentajeConsumido,
                    x.Linea.Nivel)),
        ];
    }

    /// <summary>Recibos y cuotas que vencen en los proximos dias.</summary>
    /// <param name="hoy">Fecha actual.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Los compromisos, del mas cercano al mas lejano.</returns>
    private async Task<IReadOnlyList<CompromisoProximo>> ObtenerCompromisosAsync(
        DateOnly hoy,
        CancellationToken cancelacion)
    {
        var limite = hoy.AddDays(DiasDeHorizonte);

        var recurrentes = await contexto.GastosRecurrentes
            .AsNoTracking()
            .Where(g => g.Estado == EstadoRecurrencia.Activa
                        && g.ProximaFechaPago >= hoy
                        && g.ProximaFechaPago <= limite)
            .Select(g => new CompromisoProximo(
                "GastoRecurrente", g.Id, g.Nombre, g.MontoEstimado, g.ProximaFechaPago, 0))
            .ToListAsync(cancelacion);

        var deudas = await contexto.Deudas
            .AsNoTracking()
            .Where(d => d.Estado == EstadoDeuda.Activa && d.DiaVencimiento != null)
            .Select(d => new
            {
                d.Id,
                d.Nombre,
                Cuota = d.PagoMensual ?? d.PagoMinimo ?? 0m,
                Dia = d.DiaVencimiento!.Value,
            })
            .ToListAsync(cancelacion);

        var compromisos = new List<CompromisoProximo>(recurrentes);

        foreach (var deuda in deudas)
        {
            var vencimiento = ProximoVencimiento(hoy, deuda.Dia);

            if (vencimiento <= limite)
            {
                compromisos.Add(new CompromisoProximo(
                    "Deuda", deuda.Id, deuda.Nombre, deuda.Cuota, vencimiento, 0));
            }
        }

        return
        [
            .. compromisos
                .Select(c => c with { DiasRestantes = c.Fecha.DayNumber - hoy.DayNumber })
                .OrderBy(c => c.Fecha),
        ];
    }

    /// <summary>Calcula el proximo vencimiento mensual de una deuda.</summary>
    /// <param name="hoy">Fecha actual.</param>
    /// <param name="diaDelMes">Dia en que vence cada mes.</param>
    /// <returns>La proxima fecha de vencimiento.</returns>
    /// <remarks>
    /// El dia se recorta a la longitud del mes: una cuota que vence el 31 vence el 28 en
    /// febrero, no el 3 de marzo.
    /// </remarks>
    private static DateOnly ProximoVencimiento(DateOnly hoy, int diaDelMes)
    {
        var esteMes = new DateOnly(
            hoy.Year, hoy.Month,
            Math.Min(diaDelMes, DateTime.DaysInMonth(hoy.Year, hoy.Month)));

        if (esteMes >= hoy)
        {
            return esteMes;
        }

        var siguiente = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(1);

        return new DateOnly(
            siguiente.Year, siguiente.Month,
            Math.Min(diaDelMes, DateTime.DaysInMonth(siguiente.Year, siguiente.Month)));
    }

    /// <summary>Avance de las metas activas.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Las metas resumidas.</returns>
    private async Task<IReadOnlyList<AvanceMeta>> ObtenerMetasAsync(
        CancellationToken cancelacion)
    {
        var activas = await metas.ListarAsync(incluirCerradas: false, cancelacion);

        return
        [
            .. activas.Select(m => new AvanceMeta(
                m.Id, m.Nombre, m.MontoObjetivo, m.MontoActual,
                m.PorcentajeCompletado, m.FechaObjetivo, m.VaAtrasada)),
        ];
    }

    /// <summary>Cuenta los avisos sin leer del espacio.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Cuantos hay.</returns>
    private async Task<int> ContarNotificacionesSinLeerAsync(CancellationToken cancelacion) =>
        await contexto.Notificaciones
            .AsNoTracking()
            .CountAsync(n => n.FechaLectura == null
                             && n.Estado != EstadoNotificacion.Descartada, cancelacion);
}
