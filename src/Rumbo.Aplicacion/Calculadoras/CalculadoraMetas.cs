namespace Rumbo.Aplicacion.Calculadoras;

/// <summary>
/// Cuanto falta para una meta y a que ritmo hay que aportar para llegar a tiempo.
/// </summary>
/// <param name="MontoObjetivo">Lo que se quiere reunir.</param>
/// <param name="MontoActual">Lo reunido hasta ahora.</param>
/// <param name="MontoFaltante">Lo que queda. Nunca negativo.</param>
/// <param name="PorcentajeCompletado">Progreso, entre 0 y 100.</param>
/// <param name="FechaObjetivo">Fecha en que se quiere tener el dinero.</param>
/// <param name="DiasRestantes">Dias hasta la fecha objetivo. Negativo si ya paso.</param>
/// <param name="MesesRestantes">Meses completos hasta la fecha objetivo.</param>
/// <param name="AporteMensualNecesario">Lo que habria que aportar cada mes.</param>
/// <param name="AporteSemanalNecesario">Lo mismo repartido por semanas.</param>
/// <param name="EstaAlcanzada">Si ya se llego al objetivo.</param>
/// <param name="PlazoVencido">Si la fecha objetivo ya paso sin alcanzarla.</param>
/// <param name="SinFechaObjetivo">
/// Si la meta no tiene fecha. En ese caso no hay ritmo que calcular: se puede aportar al
/// ritmo que se quiera.
/// </param>
public record ProyeccionMeta(
    decimal MontoObjetivo,
    decimal MontoActual,
    decimal MontoFaltante,
    decimal PorcentajeCompletado,
    DateOnly? FechaObjetivo,
    int? DiasRestantes,
    int? MesesRestantes,
    decimal? AporteMensualNecesario,
    decimal? AporteSemanalNecesario,
    bool EstaAlcanzada,
    bool PlazoVencido,
    bool SinFechaObjetivo);

/// <summary>
/// Calcula el ritmo de ahorro que exige una meta.
/// </summary>
/// <remarks>
/// <para>
/// Es aritmetica pura, sin acceso a datos: recibe los numeros y devuelve la proyeccion. Por
/// eso puede probarse sola, y por eso los ejemplos del usuario se convierten en pruebas
/// directas.
/// </para>
/// <para>
/// <b>Ejemplo de referencia.</b> Meta de RD$180,000, ahorrados RD$30,000, quedan 15 meses:
/// faltan RD$150,000 y el aporte necesario es RD$10,000 al mes.
/// </para>
/// </remarks>
public static class CalculadoraMetas
{
    /// <summary>
    /// Proyecta una meta a partir de sus cifras y de la fecha de hoy.
    /// </summary>
    /// <param name="montoObjetivo">Lo que se quiere reunir.</param>
    /// <param name="montoActual">Lo reunido.</param>
    /// <param name="fechaObjetivo">Fecha limite, o <c>null</c> si no la tiene.</param>
    /// <param name="hoy">Fecha actual en la zona horaria del espacio.</param>
    /// <returns>La proyeccion completa.</returns>
    public static ProyeccionMeta Proyectar(
        decimal montoObjetivo,
        decimal montoActual,
        DateOnly? fechaObjetivo,
        DateOnly hoy)
    {
        var faltante = Math.Max(0m, montoObjetivo - montoActual);
        var alcanzada = faltante == 0m;

        var porcentaje = montoObjetivo <= 0m
            ? 100m
            : Math.Min(100m, Math.Round(montoActual / montoObjetivo * 100m, 2, MidpointRounding.ToEven));

        if (fechaObjetivo is null)
        {
            // Sin fecha no hay ritmo que calcular. Devolver un aporte "necesario" seria
            // inventarse una urgencia que el usuario no ha pedido.
            return new ProyeccionMeta(
                montoObjetivo, montoActual, faltante, porcentaje,
                null, null, null, null, null,
                alcanzada, false, true);
        }

        var dias = fechaObjetivo.Value.DayNumber - hoy.DayNumber;
        var meses = MesesCompletosEntre(hoy, fechaObjetivo.Value);

        var vencido = dias < 0 && !alcanzada;

        if (alcanzada)
        {
            return new ProyeccionMeta(
                montoObjetivo, montoActual, 0m, porcentaje,
                fechaObjetivo, dias, meses, 0m, 0m,
                true, false, false);
        }

        // Si el plazo ya venció o vence hoy, el aporte necesario es todo lo que falta: no
        // queda tiempo que repartir. Dividir entre cero o entre un numero negativo daria un
        // resultado sin sentido.
        if (dias <= 0)
        {
            return new ProyeccionMeta(
                montoObjetivo, montoActual, faltante, porcentaje,
                fechaObjetivo, dias, meses, faltante, faltante,
                false, vencido, false);
        }

        // Con menos de un mes por delante, repartir entre "0 meses" seria imposible: se
        // considera un mes, que es lo que queda.
        var mesesParaRepartir = Math.Max(1, meses);

        var mensual = Math.Round(faltante / mesesParaRepartir, 2, MidpointRounding.ToEven);

        // El semanal se calcula sobre los dias reales y no dividiendo el mensual entre
        // cuatro: un mes no tiene cuatro semanas, y ese atajo subestimaria el ahorro anual.
        var semanas = Math.Max(1m, dias / 7m);
        var semanal = Math.Round(faltante / semanas, 2, MidpointRounding.ToEven);

        return new ProyeccionMeta(
            montoObjetivo, montoActual, faltante, porcentaje,
            fechaObjetivo, dias, meses, mensual, semanal,
            false, false, false);
    }

    /// <summary>
    /// Cuenta los meses completos entre dos fechas.
    /// </summary>
    /// <param name="desde">Fecha inicial.</param>
    /// <param name="hasta">Fecha final.</param>
    /// <returns>Meses completos, nunca negativo.</returns>
    /// <remarks>
    /// Cuenta meses COMPLETOS: del 15 de septiembre al 14 de octubre hay 0 meses, no 1. Es
    /// lo prudente para un plan de ahorro, porque redondear al alza haria creer que hay mas
    /// tiempo del que queda y el aporte calculado se quedaria corto.
    /// </remarks>
    public static int MesesCompletosEntre(DateOnly desde, DateOnly hasta)
    {
        if (hasta <= desde)
        {
            return 0;
        }

        var meses = ((hasta.Year - desde.Year) * 12) + hasta.Month - desde.Month;

        if (hasta.Day < desde.Day)
        {
            meses--;
        }

        return Math.Max(0, meses);
    }
}
