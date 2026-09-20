using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Comun;

namespace Rumbo.Aplicacion.Calculadoras;

/// <summary>
/// Calcula el saldo real de una cuenta a partir del libro mayor.
/// </summary>
/// <remarks>
/// <para>
/// <c>Cuenta.SaldoActual</c> es una instantanea que se mantiene al dia para que el panel sea
/// rapido. Esta clase calcula la VERDAD: el saldo inicial mas la suma de todos los
/// movimientos. Sirve para reconciliar y detectar cualquier desviacion.
/// </para>
/// <para>
/// La suma usa <c>Monto * Signo</c>, sin condicionales por tipo. Esa es precisamente la
/// ventaja de guardar siempre importes positivos con un signo aparte, y de registrar las
/// transferencias como dos asientos: el saldo de una cuenta es una suma directa, sin
/// excepciones que recordar.
/// </para>
/// </remarks>
/// <param name="contexto">Acceso al libro mayor.</param>
public class CalculadoraSaldos(IContextoRumbo contexto)
{
    /// <summary>
    /// Calcula el saldo de una cuenta sumando su libro mayor.
    /// </summary>
    /// <param name="cuentaId">Cuenta que se calcula.</param>
    /// <param name="hasta">
    /// Fecha limite, incluida. Si es <c>null</c>, se suman todos los movimientos.
    /// </param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El saldo calculado y cuantos movimientos se consideraron.</returns>
    /// <remarks>
    /// Los movimientos eliminados quedan fuera por efecto del filtro global de borrado
    /// logico, que es justo lo que se quiere: un movimiento borrado no debe afectar al saldo,
    /// aunque siga en la base de datos para la auditoria.
    /// </remarks>
    public async Task<(decimal Saldo, int CantidadMovimientos)> CalcularAsync(
        Guid cuentaId,
        DateOnly? hasta = null,
        CancellationToken cancelacion = default)
    {
        var cuenta = await contexto.Cuentas
            .AsNoTracking()
            .Where(c => c.Id == cuentaId)
            .Select(c => new { c.SaldoInicial })
            .FirstOrDefaultAsync(cancelacion);

        if (cuenta is null)
        {
            return (0m, 0);
        }

        var consulta = contexto.Movimientos
            .AsNoTracking()
            .Where(m => m.CuentaId == cuentaId);

        if (hasta.HasValue)
        {
            consulta = consulta.Where(m => m.FechaMovimiento <= hasta.Value);
        }

        var resumen = await consulta
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Suma = g.Sum(m => m.Monto * m.Signo),
                Cantidad = g.Count(),
            })
            .FirstOrDefaultAsync(cancelacion);

        var suma = resumen?.Suma ?? 0m;
        var cantidad = resumen?.Cantidad ?? 0;

        return (cuenta.SaldoInicial + suma, cantidad);
    }
}
