namespace Rumbo.Dominio.Enums;

/// <summary>
/// Clase de sugerencia generada por el motor de recomendaciones.
/// </summary>
/// <remarks>
/// Cada tipo lo produce una regla distinta que implementa <c>IReglaRecomendacion</c>, y toda
/// recomendacion guarda los datos con los que se calculo para poder explicarse.
/// </remarks>
public enum TipoRecomendacion
{
    /// <summary>Cuanto conviene aportar al mes para alcanzar una meta a tiempo.</summary>
    AporteMensualParaMeta = 1,

    /// <summary>Analisis de si un viaje encaja con el flujo financiero historico.</summary>
    ViabilidadDeViaje = 2,

    /// <summary>Posibles destinos para un ingreso extraordinario recien registrado.</summary>
    DestinoDeIngresoExtra = 3,

    /// <summary>Aviso de que una partida del presupuesto se acerca a su limite.</summary>
    AlertaDePresupuesto = 4,

    /// <summary>La meta no llegara a tiempo al ritmo de aporte actual.</summary>
    MetaAtrasada = 5,

    /// <summary>Se detecto un excedente recurrente sin asignar.</summary>
    OportunidadDeAhorro = 6,
}
