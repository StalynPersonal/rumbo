namespace Rumbo.Dominio.Enums;

/// <summary>
/// Naturaleza del espacio, es decir, del grupo que comparte unas finanzas.
/// </summary>
/// <remarks>
/// El tipo no cambia las reglas de aislamiento, que son identicas para todos: sirve para
/// adaptar la interfaz y los informes por defecto.
/// </remarks>
public enum TipoEspacio
{
    /// <summary>Una sola persona administra sus finanzas.</summary>
    Personal = 1,

    /// <summary>Dos personas comparten total o parcialmente sus finanzas.</summary>
    Pareja = 2,

    /// <summary>Varios miembros de un hogar, posiblemente con roles distintos.</summary>
    Familia = 3,

    /// <summary>Finanzas de una actividad economica, separadas de las personales.</summary>
    Negocio = 4,
}
