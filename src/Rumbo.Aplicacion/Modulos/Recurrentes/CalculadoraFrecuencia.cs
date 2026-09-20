using Rumbo.Dominio.Enums;

namespace Rumbo.Aplicacion.Modulos.Recurrentes;

/// <summary>
/// Calcula cuando toca la siguiente repeticion de una obligacion periodica.
/// </summary>
/// <remarks>
/// Vive aparte porque el caso de los meses no es trivial y conviene poder probarlo solo.
/// </remarks>
public static class CalculadoraFrecuencia
{
    /// <summary>
    /// Devuelve la fecha del siguiente vencimiento.
    /// </summary>
    /// <param name="fechaActual">Fecha del vencimiento que se acaba de cubrir.</param>
    /// <param name="frecuencia">Cada cuanto se repite.</param>
    /// <returns>La fecha siguiente.</returns>
    /// <remarks>
    /// <para>
    /// <b>El caso delicado son los meses.</b> Un alquiler que vence el 31 de enero no puede
    /// vencer el 31 de febrero. <c>AddMonths</c> de .NET resuelve esto ajustando al ultimo
    /// dia del mes: el 31 de enero mas un mes es el 28 de febrero. Es el comportamiento
    /// correcto y es la razon de no hacer la aritmetica a mano.
    /// </para>
    /// <para>
    /// Efecto secundario conocido: a partir de ese ajuste, la serie se queda en el dia 28.
    /// Es lo mismo que hacen los bancos, y evitarlo exigiria guardar aparte el dia original
    /// pretendido, complejidad que no compensa para un gasto domestico.
    /// </para>
    /// </remarks>
    public static DateOnly Siguiente(DateOnly fechaActual, Frecuencia frecuencia) => frecuencia switch
    {
        Frecuencia.Semanal => fechaActual.AddDays(7),

        // Quincenal se resuelve como 15 dias y no como "dos veces al mes" porque esto ultimo
        // obligaria a decidir que dias concretos, y varia segun el pagador.
        Frecuencia.Quincenal => fechaActual.AddDays(15),

        Frecuencia.Mensual => fechaActual.AddMonths(1),
        Frecuencia.Bimestral => fechaActual.AddMonths(2),
        Frecuencia.Trimestral => fechaActual.AddMonths(3),
        Frecuencia.Semestral => fechaActual.AddMonths(6),
        Frecuencia.Anual => fechaActual.AddYears(1),

        _ => throw new ArgumentOutOfRangeException(
            nameof(frecuencia), frecuencia, "Frecuencia sin cálculo definido."),
    };

    /// <summary>
    /// Cuantas veces se repite al año. Sirve para proyectar el gasto anual.
    /// </summary>
    /// <param name="frecuencia">Periodicidad.</param>
    /// <returns>Repeticiones anuales.</returns>
    public static decimal RepeticionesPorAnio(Frecuencia frecuencia) => frecuencia switch
    {
        Frecuencia.Semanal => 52m,
        Frecuencia.Quincenal => 24m,
        Frecuencia.Mensual => 12m,
        Frecuencia.Bimestral => 6m,
        Frecuencia.Trimestral => 4m,
        Frecuencia.Semestral => 2m,
        Frecuencia.Anual => 1m,

        _ => throw new ArgumentOutOfRangeException(
            nameof(frecuencia), frecuencia, "Frecuencia sin cálculo definido."),
    };

    /// <summary>
    /// Convierte un importe periodico a su equivalente mensual.
    /// </summary>
    /// <param name="monto">Importe de cada repeticion.</param>
    /// <param name="frecuencia">Periodicidad.</param>
    /// <returns>Lo que supone al mes.</returns>
    /// <remarks>
    /// Necesario para comparar obligaciones de periodicidad distinta: un seguro anual de
    /// RD$12,000 pesa lo mismo al mes que un servicio mensual de RD$1,000, y el motor de
    /// recomendaciones debe verlos igual.
    /// </remarks>
    public static decimal EquivalenteMensual(decimal monto, Frecuencia frecuencia) =>
        Math.Round(monto * RepeticionesPorAnio(frecuencia) / 12m, 4, MidpointRounding.ToEven);
}
