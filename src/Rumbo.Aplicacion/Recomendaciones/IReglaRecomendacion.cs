using Rumbo.Aplicacion.Calculadoras;
using Rumbo.Dominio.Enums;

namespace Rumbo.Aplicacion.Recomendaciones;

/// <summary>
/// Todo lo que una regla necesita saber para decidir si tiene algo que decir.
/// </summary>
/// <param name="EspacioId">Espacio que se analiza.</param>
/// <param name="Hoy">Fecha actual en la zona horaria del espacio.</param>
/// <param name="MonedaBase">Moneda en la que se expresan los importes.</param>
/// <param name="FlujoCaja">Resumen de ingresos, gastos y excedente del hogar.</param>
/// <remarks>
/// Se pasa ya calculado para que todas las reglas partan de los mismos numeros y para no
/// repetir el analisis del historial una vez por regla.
/// </remarks>
public record ContextoRecomendacion(
    Guid EspacioId,
    DateOnly Hoy,
    string MonedaBase,
    ResumenFlujoCaja FlujoCaja);

/// <summary>
/// Una sugerencia generada por una regla, con los datos que la justifican.
/// </summary>
/// <param name="Tipo">Clase de recomendacion.</param>
/// <param name="Titulo">Titulo corto para el panel.</param>
/// <param name="Cuerpo">Texto explicativo, en espanol y sin tono imperativo.</param>
/// <param name="Insumos">
/// Datos y calculos con los que se genero. Es lo que permite que la aplicacion responda
/// "de donde sale este numero".
/// </param>
/// <param name="MontoSugerido">Importe propuesto, si la recomendacion propone una cifra.</param>
/// <param name="ConfianzaBaja">Si se calculo con poco historial.</param>
/// <param name="MetaId">Meta relacionada, si la hay.</param>
/// <param name="ViajeId">Viaje relacionado, si lo hay.</param>
/// <param name="DiasDeVigencia">
/// Cuantos dias tiene sentido mostrarla antes de recalcularla.
/// </param>
public record SugerenciaGenerada(
    TipoRecomendacion Tipo,
    string Titulo,
    string Cuerpo,
    IReadOnlyDictionary<string, object?> Insumos,
    decimal? MontoSugerido,
    bool ConfianzaBaja,
    Guid? MetaId,
    Guid? ViajeId,
    int DiasDeVigencia);

/// <summary>
/// Una regla del motor de recomendaciones.
/// </summary>
/// <remarks>
/// <para>
/// El motor es <b>deterministico</b>: reglas y aritmetica financiera, sin inteligencia
/// artificial. Dos ejecuciones con los mismos datos dan el mismo resultado, y cada
/// recomendacion puede explicarse.
/// </para>
/// <para>
/// Cada regla es una clase independiente y probable por separado. Si algun dia se anade IA,
/// sera <i>otra</i> fuente de sugerencias con este mismo contrato, no un reemplazo.
/// </para>
/// <para>
/// <b>Ninguna regla mueve dinero.</b> Devuelven sugerencias; el usuario decide. Es el
/// principio innegociable del proyecto.
/// </para>
/// </remarks>
public interface IReglaRecomendacion
{
    /// <summary>
    /// Orden en que se evalua. Menor valor, antes.
    /// </summary>
    /// <remarks>
    /// Determina el orden en que aparecen las sugerencias: lo urgente antes que lo
    /// conveniente.
    /// </remarks>
    int Prioridad { get; }

    /// <summary>Nombre de la regla, para los registros y el diagnostico.</summary>
    string Nombre { get; }

    /// <summary>
    /// Evalua el estado del hogar y devuelve las sugerencias que procedan.
    /// </summary>
    /// <param name="contexto">Datos ya calculados del espacio.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>
    /// Las sugerencias, o una lista vacia si la regla no tiene nada que decir. Callar es una
    /// respuesta valida y frecuente: una aplicacion que sugiere algo cada vez que se abre
    /// acaba ignorandose.
    /// </returns>
    Task<IReadOnlyList<SugerenciaGenerada>> EvaluarAsync(
        ContextoRecomendacion contexto,
        CancellationToken cancelacion = default);
}
