namespace Rumbo.Aplicacion.Calculadoras;

/// <summary>
/// Nivel de alerta de una partida de presupuesto.
/// </summary>
public enum NivelAlertaPresupuesto
{
    /// <summary>Consumo por debajo del umbral de aviso.</summary>
    Normal = 1,

    /// <summary>Se alcanzo el umbral de aviso.</summary>
    Aviso = 2,

    /// <summary>Se alcanzo el umbral critico.</summary>
    Critico = 3,

    /// <summary>Se supero el importe asignado.</summary>
    Excedido = 4,
}

/// <summary>
/// Estado de una partida de presupuesto.
/// </summary>
/// <param name="MontoAsignado">Lo presupuestado.</param>
/// <param name="MontoGastado">Lo gastado realmente.</param>
/// <param name="MontoDisponible">Lo que queda. Negativo si se excedio.</param>
/// <param name="PorcentajeConsumido">Cuanto se lleva gastado, en porcentaje.</param>
/// <param name="Nivel">Nivel de alerta alcanzado.</param>
/// <param name="RitmoDiarioNecesario">
/// Cuanto se puede gastar al dia con lo que queda para llegar al final del periodo.
/// </param>
/// <param name="ProyeccionAlCierre">
/// Lo que se habra gastado al cerrar el periodo si se mantiene el ritmo actual.
/// </param>
public record EstadoPartida(
    decimal MontoAsignado,
    decimal MontoGastado,
    decimal MontoDisponible,
    decimal PorcentajeConsumido,
    NivelAlertaPresupuesto Nivel,
    decimal? RitmoDiarioNecesario,
    decimal? ProyeccionAlCierre);

/// <summary>
/// Calcula el estado de una partida de presupuesto y su nivel de alerta.
/// </summary>
/// <remarks>
/// Aritmetica pura y sin acceso a datos, para poder probarse sola.
/// </remarks>
public static class CalculadoraPresupuestos
{
    /// <summary>
    /// Evalua una partida a una fecha dada.
    /// </summary>
    /// <param name="montoAsignado">Lo presupuestado para la categoria.</param>
    /// <param name="montoGastado">Lo gastado hasta ahora en el periodo.</param>
    /// <param name="umbralAviso">Porcentaje de consumo que dispara el aviso.</param>
    /// <param name="umbralCritico">Porcentaje que dispara el aviso critico.</param>
    /// <param name="umbralExcedido">Porcentaje a partir del cual se considera excedido.</param>
    /// <param name="inicioPeriodo">Primer dia del periodo.</param>
    /// <param name="finPeriodo">Ultimo dia del periodo.</param>
    /// <param name="hoy">Fecha actual.</param>
    /// <returns>El estado de la partida.</returns>
    public static EstadoPartida Evaluar(
        decimal montoAsignado,
        decimal montoGastado,
        decimal umbralAviso,
        decimal umbralCritico,
        decimal umbralExcedido,
        DateOnly inicioPeriodo,
        DateOnly finPeriodo,
        DateOnly hoy)
    {
        var disponible = montoAsignado - montoGastado;

        // Una partida de cero esta excedida en cuanto se gasta algo, y en cero si no se
        // gasto nada. Dividir entre cero daria infinito.
        var porcentaje = montoAsignado <= 0m
            ? (montoGastado > 0m ? 100m : 0m)
            : Math.Round(montoGastado / montoAsignado * 100m, 2, MidpointRounding.ToEven);

        var nivel = DeterminarNivel(porcentaje, umbralAviso, umbralCritico, umbralExcedido);

        var diasTotales = Math.Max(1, finPeriodo.DayNumber - inicioPeriodo.DayNumber + 1);
        var diasTranscurridos = Math.Clamp(hoy.DayNumber - inicioPeriodo.DayNumber + 1, 0, diasTotales);
        var diasRestantes = diasTotales - diasTranscurridos;

        decimal? ritmoDiario = diasRestantes > 0
            ? Math.Round(Math.Max(0m, disponible) / diasRestantes, 2, MidpointRounding.ToEven)
            : null;

        // La proyeccion extrapola el ritmo actual al periodo completo. Es lo que permite
        // avisar el dia 15 de que, al paso que va, el mes cerrara excedido, en lugar de
        // avisar el dia 28 cuando ya no hay margen de reaccion.
        decimal? proyeccion = diasTranscurridos > 0
            ? Math.Round(montoGastado / diasTranscurridos * diasTotales, 2, MidpointRounding.ToEven)
            : null;

        return new EstadoPartida(
            montoAsignado, montoGastado, disponible, porcentaje, nivel, ritmoDiario, proyeccion);
    }

    /// <summary>Traduce el porcentaje consumido a un nivel de alerta.</summary>
    /// <param name="porcentaje">Consumo actual.</param>
    /// <param name="aviso">Umbral de aviso.</param>
    /// <param name="critico">Umbral critico.</param>
    /// <param name="excedido">Umbral de exceso.</param>
    /// <returns>El nivel alcanzado.</returns>
    /// <remarks>
    /// Se comprueba de mayor a menor. Al reves, un consumo del 120 % entraria en el primer
    /// umbral que superara y se quedaria en "Aviso": las alertas graves no saltarian nunca.
    /// </remarks>
    private static NivelAlertaPresupuesto DeterminarNivel(
        decimal porcentaje,
        decimal aviso,
        decimal critico,
        decimal excedido)
    {
        if (porcentaje >= excedido)
        {
            return NivelAlertaPresupuesto.Excedido;
        }

        if (porcentaje >= critico)
        {
            return NivelAlertaPresupuesto.Critico;
        }

        return porcentaje >= aviso
            ? NivelAlertaPresupuesto.Aviso
            : NivelAlertaPresupuesto.Normal;
    }
}
