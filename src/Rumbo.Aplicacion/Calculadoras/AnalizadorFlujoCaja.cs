using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Modulos.Movimientos;

namespace Rumbo.Aplicacion.Calculadoras;

/// <summary>
/// Resumen de lo que entra y sale del hogar en un periodo.
/// </summary>
/// <param name="MesesAnalizados">Meses de historial que se pudieron analizar.</param>
/// <param name="IngresoPromedioMensual">Media de ingresos por mes.</param>
/// <param name="GastoPromedioMensual">Media de gastos por mes.</param>
/// <param name="ExcedentePromedioMensual">
/// Lo que sobra al mes de media. Negativo si se gasta mas de lo que entra.
/// </param>
/// <param name="CompromisoMensualRecurrente">
/// Lo que ya esta comprometido cada mes en obligaciones periodicas.
/// </param>
/// <param name="DisponibleParaAhorro">
/// Excedente menos compromisos recurrentes que todavia no se han pagado. Es la cifra
/// honesta sobre la que hacer recomendaciones.
/// </param>
/// <param name="ConfianzaBaja">
/// Indica que hay poco historial y las medias son poco fiables.
/// </param>
/// <param name="Moneda">Moneda en la que se expresan todas las cifras.</param>
public record ResumenFlujoCaja(
    int MesesAnalizados,
    decimal IngresoPromedioMensual,
    decimal GastoPromedioMensual,
    decimal ExcedentePromedioMensual,
    decimal CompromisoMensualRecurrente,
    decimal DisponibleParaAhorro,
    bool ConfianzaBaja,
    string Moneda);

/// <summary>
/// Analiza el historial del hogar para estimar cuanto dinero sobra cada mes.
/// </summary>
/// <remarks>
/// <para>
/// Es la base de todas las recomendaciones. Sin esto, sugerir un aporte mensual seria
/// adivinar.
/// </para>
/// <para>
/// <b>Con poco historial no se calla, pero avisa.</b> Con menos de tres meses de datos las
/// medias son poco representativas —un mes con un gasto extraordinario las distorsiona por
/// completo—, asi que el resultado se marca con <c>ConfianzaBaja</c> y la aplicacion debe
/// mostrarlo como estimacion, no como hecho.
/// </para>
/// </remarks>
/// <param name="contexto">Acceso al libro mayor.</param>
public class AnalizadorFlujoCaja(IContextoRumbo contexto)
{
    /// <summary>Meses minimos para considerar fiables las medias.</summary>
    /// <remarks>
    /// Con menos de tres, un solo mes atipico manda sobre el resultado. Proyectar un ano a
    /// partir de dos semanas es adivinar.
    /// </remarks>
    private const int MesesMinimosParaConfiar = 3;

    /// <summary>
    /// Analiza el flujo de caja de los ultimos meses.
    /// </summary>
    /// <param name="hoy">Fecha actual en la zona horaria del espacio.</param>
    /// <param name="mesesHistorial">Meses hacia atras que se analizan.</param>
    /// <param name="monedaBase">Moneda de consolidacion del espacio.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El resumen del periodo.</returns>
    public async Task<ResumenFlujoCaja> AnalizarAsync(
        DateOnly hoy,
        int mesesHistorial,
        string monedaBase,
        CancellationToken cancelacion = default)
    {
        var meses = Math.Clamp(mesesHistorial, 1, 60);

        // Se analizan meses COMPLETOS y se excluye el mes en curso. Incluirlo a mitad
        // hundiria la media de gastos: al dia 5 solo se ha gastado una fraccion del mes.
        var primerDiaDelMesActual = new DateOnly(hoy.Year, hoy.Month, 1);
        var desde = primerDiaDelMesActual.AddMonths(-meses);
        var hasta = primerDiaDelMesActual.AddDays(-1);

        var totales = await contexto.Movimientos
            .AsNoTracking()
            .IngresosYGastos()
            .EnRango(desde, hasta)
            .GroupBy(m => m.Signo)
            .Select(g => new { Signo = g.Key, Total = g.Sum(m => m.MontoEnMonedaBase) })
            .ToListAsync(cancelacion);

        var ingresos = totales.FirstOrDefault(t => t.Signo == 1)?.Total ?? 0m;
        var gastos = totales.FirstOrDefault(t => t.Signo == -1)?.Total ?? 0m;

        // Cuantos meses tienen datos de verdad. Si el hogar se dio de alta hace dos meses,
        // dividir entre seis daria una media artificialmente baja.
        var mesesConDatos = await ContarMesesConDatosAsync(desde, hasta, cancelacion);
        var divisor = Math.Max(1, mesesConDatos);

        var ingresoMedio = Redondear(ingresos / divisor);
        var gastoMedio = Redondear(gastos / divisor);
        var excedente = Redondear(ingresoMedio - gastoMedio);

        var compromiso = await CalcularCompromisoRecurrenteAsync(cancelacion);

        // El disponible resta los compromisos: prometer que sobran RD$18,500 cuando
        // RD$8,000 ya estan comprometidos en recibos seria enganar al usuario.
        var disponible = Redondear(excedente - compromiso);

        return new ResumenFlujoCaja(
            mesesConDatos,
            ingresoMedio,
            gastoMedio,
            excedente,
            compromiso,
            disponible,
            mesesConDatos < MesesMinimosParaConfiar,
            monedaBase);
    }

    /// <summary>Cuenta los meses del periodo que tienen al menos un movimiento.</summary>
    /// <param name="desde">Fecha inicial.</param>
    /// <param name="hasta">Fecha final.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Cantidad de meses con datos.</returns>
    private async Task<int> ContarMesesConDatosAsync(
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancelacion) =>
        await contexto.Movimientos
            .AsNoTracking()
            .IngresosYGastos()
            .EnRango(desde, hasta)
            .Select(m => new { m.FechaMovimiento.Year, m.FechaMovimiento.Month })
            .Distinct()
            .CountAsync(cancelacion);

    /// <summary>
    /// Suma lo que las obligaciones periodicas activas suponen cada mes.
    /// </summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El compromiso mensual.</returns>
    /// <remarks>
    /// Se normaliza a mensual con <c>CalculadoraFrecuencia</c>: un seguro anual de
    /// RD$12,000 pesa RD$1,000 al mes, igual que un servicio mensual de RD$1,000.
    /// </remarks>
    private async Task<decimal> CalcularCompromisoRecurrenteAsync(CancellationToken cancelacion)
    {
        var activos = await contexto.GastosRecurrentes
            .AsNoTracking()
            .Where(g => g.Estado == Dominio.Enums.EstadoRecurrencia.Activa)
            .Select(g => new { g.MontoEstimado, g.Frecuencia })
            .ToListAsync(cancelacion);

        var total = activos.Sum(g =>
            Modulos.Recurrentes.CalculadoraFrecuencia.EquivalenteMensual(
                g.MontoEstimado, g.Frecuencia));

        return Redondear(total);
    }

    /// <summary>Redondea a dos decimales con redondeo bancario.</summary>
    /// <param name="valor">Importe.</param>
    /// <returns>El importe redondeado.</returns>
    private static decimal Redondear(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.ToEven);
}
