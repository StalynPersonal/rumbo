using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Entidades.Planificacion;

namespace Rumbo.Dominio.Entidades.Soporte;

/// <summary>
/// Sugerencia generada por el motor de recomendaciones, con los datos que la justifican.
/// </summary>
/// <remarks>
/// <para>
/// <b>Principio innegociable: una recomendacion nunca mueve dinero.</b> Nace en estado
/// <see cref="EstadoRecomendacion.Pendiente"/> y solo se convierte en un movimiento real
/// cuando la persona pulsa "Crear aporte". Que el sistema transfiera dinero por su cuenta,
/// aunque fuera con buena intencion, destruiria la confianza en la aplicacion.
/// </para>
/// <para>
/// <b>Por que se guardan los insumos.</b> Una recomendacion que dice "ahorra RD$10,000 al mes"
/// sin explicar de donde sale ese numero es un acto de fe. En <see cref="Insumos"/> queda el
/// JSON con los datos y la formula usados, de modo que la aplicacion siempre puede responder
/// "porque en los ultimos 3 meses tu excedente promedio fue de RD$18,500".
/// </para>
/// <para>
/// El motor es deterministico: reglas y aritmetica financiera, sin inteligencia artificial. Si
/// mas adelante se anade IA, sera otra fuente de recomendaciones con este mismo contrato.
/// </para>
/// </remarks>
public class Recomendacion : EntidadDeEspacio
{
    /// <summary>Clase de sugerencia.</summary>
    public TipoRecomendacion Tipo { get; set; }

    /// <summary>Titulo corto que se muestra en el panel.</summary>
    public required string Titulo { get; set; }

    /// <summary>Texto explicativo, redactado en espanol y sin tono imperativo.</summary>
    public required string Cuerpo { get; set; }

    /// <summary>
    /// Datos y calculos con los que se genero, en JSON. Es lo que permite explicarla.
    /// </summary>
    public string? Insumos { get; set; }

    /// <summary>Importe sugerido, cuando la recomendacion propone una cifra.</summary>
    public decimal? MontoSugerido { get; set; }

    /// <summary>Codigo ISO-4217 de la moneda del importe sugerido.</summary>
    public string? Moneda { get; set; }

    /// <summary>
    /// Indica que se calculo con pocos datos y por tanto es una estimacion debil.
    /// </summary>
    /// <remarks>
    /// Con menos de tres meses de historial, proyectar un ano es adivinar. Marcarlo es mas
    /// honesto que ocultar la incertidumbre.
    /// </remarks>
    public bool ConfianzaBaja { get; set; }

    /// <summary>Meta relacionada, si la hay.</summary>
    public Guid? MetaId { get; set; }

    /// <summary>Meta relacionada.</summary>
    public Meta? Meta { get; set; }

    /// <summary>Viaje relacionado, si lo hay.</summary>
    public Guid? ViajeId { get; set; }

    /// <summary>Viaje relacionado.</summary>
    public Viaje? Viaje { get; set; }

    /// <summary>Instante en que se genero.</summary>
    public DateTimeOffset FechaGeneracion { get; set; }

    /// <summary>
    /// Instante a partir del cual deja de mostrarse, porque los datos que la sustentan
    /// probablemente hayan cambiado.
    /// </summary>
    public DateTimeOffset? FechaExpiracion { get; set; }

    /// <summary>Que decidio el usuario.</summary>
    public EstadoRecomendacion Estado { get; set; } = EstadoRecomendacion.Pendiente;

    /// <summary>Instante en que el usuario la acepto o la descarto.</summary>
    public DateTimeOffset? FechaRespuesta { get; set; }

    /// <summary>Usuario que respondio.</summary>
    public Guid? RespondidaPorUsuarioId { get; set; }
}
