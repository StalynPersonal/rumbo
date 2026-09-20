using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Enums;

namespace Rumbo.Aplicacion.Modulos.Movimientos;

/// <summary>
/// Consultas reutilizables sobre el libro mayor.
/// </summary>
/// <remarks>
/// <para>
/// <b>Existe para que la regla mas importante del sistema se escriba UNA sola vez.</b> Una
/// transferencia no es un gasto, y un ajuste tampoco. Si cada informe repitiera ese filtro
/// por su cuenta, tarde o temprano alguno lo olvidaria y empezaria a contar los traspasos
/// entre cuentas propias como dinero que sale del hogar.
/// </para>
/// <para>
/// Todo informe que sume ingresos o gastos debe partir de <see cref="SoloIngresos"/>,
/// <see cref="SoloGastos"/> o <see cref="IngresosYGastos"/>.
/// </para>
/// </remarks>
public static class ConsultasMovimientos
{
    /// <summary>
    /// Deja solo los movimientos que cuentan como ingreso o como gasto.
    /// </summary>
    /// <param name="consulta">Consulta de partida.</param>
    /// <returns>La consulta filtrada.</returns>
    /// <remarks>
    /// Excluye transferencias y ajustes. Un traspaso mueve dinero dentro del hogar, y un
    /// ajuste solo corrige un saldo: ninguno de los dos representa dinero que entre o salga.
    /// </remarks>
    public static IQueryable<Movimiento> IngresosYGastos(this IQueryable<Movimiento> consulta) =>
        consulta.Where(m => m.Tipo == TipoMovimiento.Ingreso || m.Tipo == TipoMovimiento.Gasto);

    /// <summary>Deja solo los ingresos.</summary>
    /// <param name="consulta">Consulta de partida.</param>
    /// <returns>La consulta filtrada.</returns>
    public static IQueryable<Movimiento> SoloIngresos(this IQueryable<Movimiento> consulta) =>
        consulta.Where(m => m.Tipo == TipoMovimiento.Ingreso);

    /// <summary>Deja solo los gastos.</summary>
    /// <param name="consulta">Consulta de partida.</param>
    /// <returns>La consulta filtrada.</returns>
    public static IQueryable<Movimiento> SoloGastos(this IQueryable<Movimiento> consulta) =>
        consulta.Where(m => m.Tipo == TipoMovimiento.Gasto);

    /// <summary>Deja solo los movimientos de un rango de fechas, ambas incluidas.</summary>
    /// <param name="consulta">Consulta de partida.</param>
    /// <param name="desde">Fecha inicial.</param>
    /// <param name="hasta">Fecha final.</param>
    /// <returns>La consulta filtrada.</returns>
    public static IQueryable<Movimiento> EnRango(
        this IQueryable<Movimiento> consulta,
        DateOnly desde,
        DateOnly hasta) =>
        consulta.Where(m => m.FechaMovimiento >= desde && m.FechaMovimiento <= hasta);

    /// <summary>
    /// Determina el signo que corresponde a un tipo de movimiento.
    /// </summary>
    /// <param name="tipo">Tipo del movimiento.</param>
    /// <param name="esSalida">
    /// En una transferencia, si se trata de la pata de salida. Se ignora en el resto.
    /// </param>
    /// <returns><c>+1</c> si suma al saldo, <c>-1</c> si resta.</returns>
    /// <remarks>
    /// El signo lo decide SIEMPRE el servidor a partir del tipo, nunca el cliente. Si
    /// llegara del exterior, bastaria enviar un gasto con signo positivo para inflar el
    /// saldo.
    /// </remarks>
    public static int SignoPara(TipoMovimiento tipo, bool esSalida = false) => tipo switch
    {
        TipoMovimiento.Ingreso => 1,
        TipoMovimiento.Gasto => -1,
        TipoMovimiento.Transferencia => esSalida ? -1 : 1,

        // Un ajuste puede ir en cualquier direccion; quien lo registra indica el signo
        // aparte, asi que aqui se devuelve el neutro positivo y el servicio lo ajusta.
        TipoMovimiento.Ajuste => 1,
        _ => throw new ArgumentOutOfRangeException(
            nameof(tipo), tipo, "Tipo de movimiento sin signo definido."),
    };
}
