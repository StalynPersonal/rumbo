namespace Rumbo.Aplicacion.Calculadoras;

/// <summary>
/// Uno de los tres escenarios con los que se responde si un viaje es alcanzable.
/// </summary>
/// <param name="Nombre">Conservador, Esperado u Optimista.</param>
/// <param name="AporteMensualSupuesto">Cuanto se supone que se apartara cada mes.</param>
/// <param name="PorcentajeDelDisponible">
/// Que parte del excedente mensual representa ese aporte. Es el supuesto del escenario.
/// </param>
/// <param name="AhorroProyectado">Lo que habria reunido el dia de salida.</param>
/// <param name="Faltante">Lo que faltaria ese dia. Cero si alcanza.</param>
/// <param name="PorcentajeCubierto">Que parte del coste quedaria cubierta.</param>
/// <param name="Alcanza">Si con ese ritmo se llega.</param>
/// <param name="MesesQueFaltarian">
/// Cuantos meses mas de ahorro harian falta si no alcanza. Nulo si alcanza.
/// </param>
public record EscenarioViaje(
    string Nombre,
    decimal AporteMensualSupuesto,
    decimal PorcentajeDelDisponible,
    decimal AhorroProyectado,
    decimal Faltante,
    decimal PorcentajeCubierto,
    bool Alcanza,
    int? MesesQueFaltarian);

/// <summary>
/// Respuesta completa a la pregunta «¿podemos permitirnos este viaje?».
/// </summary>
/// <param name="CostoTotal">Lo que costaria el viaje.</param>
/// <param name="FondoActual">Lo que ya se lleva ahorrado para el.</param>
/// <param name="Faltante">Lo que falta por reunir hoy.</param>
/// <param name="MesesHastaLaSalida">Meses completos que quedan.</param>
/// <param name="DiasHastaLaSalida">Dias que quedan.</param>
/// <param name="AporteMensualNecesario">
/// Lo que habria que apartar cada mes para llegar justo a tiempo.
/// </param>
/// <param name="DisponibleMensual">
/// Lo que le sobra al hogar cada mes segun su historial, ya descontados los compromisos.
/// </param>
/// <param name="EsfuerzoRequerido">
/// Que parte del excedente mensual se comeria el viaje. Por encima de 100 no cabe.
/// </param>
/// <param name="Veredicto">Si, Ajustado o No.</param>
/// <param name="Explicacion">El porque, en una frase, en espanol.</param>
/// <param name="Escenarios">Los tres escenarios, del mas prudente al mas optimista.</param>
/// <param name="FechaViableMasCercana">
/// Cuando se podria viajar sin forzar nada, si hoy no alcanza. Nulo si ya alcanza.
/// </param>
/// <param name="ConfianzaBaja">
/// Si el calculo se apoya en poco historial. Entonces es una estimacion, no un dato.
/// </param>
/// <param name="Moneda">Moneda en la que se expresan todas las cifras.</param>
public record ViabilidadViaje(
    decimal CostoTotal,
    decimal FondoActual,
    decimal Faltante,
    int MesesHastaLaSalida,
    int DiasHastaLaSalida,
    decimal AporteMensualNecesario,
    decimal DisponibleMensual,
    decimal EsfuerzoRequerido,
    string Veredicto,
    string Explicacion,
    IReadOnlyList<EscenarioViaje> Escenarios,
    DateOnly? FechaViableMasCercana,
    bool ConfianzaBaja,
    string Moneda);

/// <summary>
/// Responde si un viaje es alcanzable con el dinero que el hogar realmente tiene.
/// </summary>
/// <remarks>
/// <para>
/// Es aritmetica pura, sin acceso a datos: recibe las cifras y devuelve la respuesta. Por eso
/// puede probarse sola y por eso la respuesta es siempre la misma con los mismos numeros.
/// </para>
/// <para>
/// <b>Por que tres escenarios y no una cifra.</b> Una respuesta unica —«si, podeis»— se lee
/// como una promesa, y el excedente mensual de un hogar no es constante: un mes hay una
/// reparacion, otro una boda. Tres escenarios dicen la verdad: con que supuesto se llega y
/// con cual no. La decision sigue siendo de las personas.
/// </para>
/// <para>
/// <b>Esto no mueve dinero ni reserva nada.</b> Es una proyeccion para decidir.
/// </para>
/// </remarks>
public static class CalculadoraViajes
{
    /// <summary>
    /// Supuestos de cada escenario, como fraccion del excedente mensual del hogar.
    /// </summary>
    /// <remarks>
    /// El conservador no supone que el hogar ahorre todo lo que le sobra: nadie lo hace. El
    /// optimista sí, y por eso es el techo, no la previsión.
    /// </remarks>
    private static readonly (string Nombre, decimal Fraccion)[] Supuestos =
    [
        ("Conservador", 0.60m),
        ("Esperado", 0.85m),
        ("Optimista", 1.00m),
    ];

    /// <summary>
    /// Proyecta si el viaje se puede pagar con el ritmo de ahorro del hogar.
    /// </summary>
    /// <param name="costoTotal">Lo que costaria el viaje.</param>
    /// <param name="fondoActual">Lo ya ahorrado para el.</param>
    /// <param name="fechaSalida">Fecha prevista de salida.</param>
    /// <param name="hoy">Fecha actual en la zona horaria del espacio.</param>
    /// <param name="disponibleMensual">Excedente mensual del hogar, ya sin compromisos.</param>
    /// <param name="confianzaBaja">Si el historial es corto.</param>
    /// <param name="moneda">Moneda de las cifras.</param>
    /// <returns>La respuesta completa con sus tres escenarios.</returns>
    public static ViabilidadViaje Proyectar(
        decimal costoTotal,
        decimal fondoActual,
        DateOnly fechaSalida,
        DateOnly hoy,
        decimal disponibleMensual,
        bool confianzaBaja,
        string moneda)
    {
        var faltante = Math.Max(0m, costoTotal - fondoActual);
        var dias = fechaSalida.DayNumber - hoy.DayNumber;
        var meses = CalculadoraMetas.MesesCompletosEntre(hoy, fechaSalida);

        // Si el fondo ya cubre el viaje no hay nada que proyectar: esta pagado.
        if (faltante == 0m)
        {
            return new ViabilidadViaje(
                costoTotal, fondoActual, 0m, meses, dias, 0m,
                disponibleMensual, 0m, "Si",
                "El fondo del viaje ya cubre el presupuesto completo.",
                Supuestos.Select(s => new EscenarioViaje(
                    s.Nombre, Redondear(Math.Max(0m, disponibleMensual) * s.Fraccion),
                    s.Fraccion * 100m, fondoActual, 0m, 100m, true, null)).ToList(),
                null, confianzaBaja, moneda);
        }

        // Con menos de un mes por delante no hay ritmo que repartir: o esta el dinero o no.
        var mesesParaRepartir = Math.Max(1, meses);
        var necesarioMensual = Redondear(faltante / mesesParaRepartir);

        var disponible = Math.Max(0m, disponibleMensual);

        var esfuerzo = disponible <= 0m
            ? 999m
            : Redondear(necesarioMensual / disponible * 100m);

        var escenarios = Supuestos
            .Select(s => Evaluar(s.Nombre, s.Fraccion, disponible, fondoActual, costoTotal, meses))
            .ToList();

        var (veredicto, explicacion) = Dictaminar(escenarios, disponible, dias, meses);

        var fechaViable = veredicto == "Si"
            ? null
            : CalcularFechaViable(faltante, disponible, hoy);

        return new ViabilidadViaje(
            costoTotal, fondoActual, faltante, meses, dias, necesarioMensual,
            disponibleMensual, esfuerzo, veredicto, explicacion, escenarios,
            fechaViable, confianzaBaja, moneda);
    }

    /// <summary>Evalua un escenario concreto.</summary>
    /// <param name="nombre">Nombre del escenario.</param>
    /// <param name="fraccion">Parte del excedente que se supone ahorrada.</param>
    /// <param name="disponible">Excedente mensual del hogar.</param>
    /// <param name="fondoActual">Lo ya ahorrado.</param>
    /// <param name="costoTotal">Lo que cuesta el viaje.</param>
    /// <param name="meses">Meses hasta la salida.</param>
    /// <returns>El escenario evaluado.</returns>
    private static EscenarioViaje Evaluar(
        string nombre,
        decimal fraccion,
        decimal disponible,
        decimal fondoActual,
        decimal costoTotal,
        int meses)
    {
        var aporte = Redondear(disponible * fraccion);
        var proyectado = Redondear(fondoActual + (aporte * meses));
        var falta = Math.Max(0m, Redondear(costoTotal - proyectado));
        var alcanza = falta == 0m;

        var cubierto = costoTotal <= 0m
            ? 100m
            : Math.Min(100m, Redondear(proyectado / costoTotal * 100m));

        // Si no alcanza, cuantos meses mas harian falta al mismo ritmo. Sin aporte no hay
        // respuesta posible: con cero al mes no se llega nunca, y decir "infinitos meses"
        // no ayuda a nadie.
        int? mesesExtra = alcanza || aporte <= 0m
            ? null
            : (int)Math.Ceiling(falta / aporte);

        return new EscenarioViaje(
            nombre, aporte, fraccion * 100m, proyectado, falta, cubierto, alcanza, mesesExtra);
    }

    /// <summary>Traduce los tres escenarios a un veredicto en espanol.</summary>
    /// <param name="escenarios">Los escenarios ya evaluados.</param>
    /// <param name="disponible">Excedente mensual del hogar.</param>
    /// <param name="dias">Dias hasta la salida.</param>
    /// <param name="meses">Meses hasta la salida.</param>
    /// <returns>El veredicto y su explicacion.</returns>
    private static (string Veredicto, string Explicacion) Dictaminar(
        List<EscenarioViaje> escenarios,
        decimal disponible,
        int dias,
        int meses)
    {
        if (dias < 0)
        {
            return ("No", "La fecha de salida ya pasó.");
        }

        if (disponible <= 0m)
        {
            // Sin excedente no hay ahorro posible, y fingir lo contrario seria el peor
            // consejo que puede dar una aplicacion de finanzas.
            return ("No",
                "Ahora mismo el hogar no tiene excedente mensual: los gastos se comen los "
                + "ingresos. Antes de planificar el viaje hay que abrir un margen.");
        }

        var conservador = escenarios[0];
        var esperado = escenarios[1];
        var optimista = escenarios[2];

        if (conservador.Alcanza)
        {
            return ("Si",
                $"Se llega incluso en el escenario prudente: apartando el "
                + $"{conservador.PorcentajeDelDisponible:0} % de lo que sobra cada mes, en "
                + $"{meses} meses está cubierto.");
        }

        if (esperado.Alcanza)
        {
            return ("Ajustado",
                $"Se llega si el hogar aparta el {esperado.PorcentajeDelDisponible:0} % de su "
                + "excedente todos los meses, sin fallar ninguno. Un imprevisto lo rompe.");
        }

        if (optimista.Alcanza)
        {
            return ("Ajustado",
                "Solo se llega ahorrando absolutamente todo lo que sobra cada mes, sin un "
                + "solo gasto imprevisto. Es posible, pero no es un plan.");
        }

        var cubierto = optimista.PorcentajeCubierto;

        return ("No",
            $"Ni ahorrando todo el excedente se llega: quedaría cubierto un "
            + $"{cubierto:0.##} % del viaje. Hace falta más plazo, menos presupuesto o un "
            + "ingreso extraordinario.");
    }

    /// <summary>
    /// Calcula a partir de que fecha el viaje seria alcanzable sin forzar nada.
    /// </summary>
    /// <param name="faltante">Lo que falta por reunir.</param>
    /// <param name="disponible">Excedente mensual del hogar.</param>
    /// <param name="hoy">Fecha actual.</param>
    /// <returns>La fecha, o <c>null</c> si no hay excedente con el que llegar.</returns>
    /// <remarks>
    /// Se usa el escenario prudente, no el optimista: proponer una fecha que solo se cumple
    /// ahorrando hasta el ultimo peso seria repetir el problema con otra cara.
    /// </remarks>
    private static DateOnly? CalcularFechaViable(
        decimal faltante,
        decimal disponible,
        DateOnly hoy)
    {
        var aportePrudente = disponible * Supuestos[0].Fraccion;

        if (aportePrudente <= 0m)
        {
            return null;
        }

        var mesesNecesarios = (int)Math.Ceiling(faltante / aportePrudente);

        // Un tope razonable: proponer una fecha a veinte años no es una recomendación, es
        // una forma elegante de decir que no.
        return mesesNecesarios > 120 ? null : hoy.AddMonths(mesesNecesarios);
    }

    /// <summary>Redondea a dos decimales con redondeo bancario.</summary>
    /// <param name="valor">Importe.</param>
    /// <returns>El importe redondeado.</returns>
    private static decimal Redondear(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.ToEven);
}
