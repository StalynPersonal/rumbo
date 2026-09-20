namespace Rumbo.Dominio.Enums;

/// <summary>
/// Que hizo el usuario con una recomendacion.
/// </summary>
/// <remarks>
/// PRINCIPIO INNEGOCIABLE: una recomendacion nunca mueve dinero por su cuenta. Nace
/// Pendiente y solo se convierte en movimiento cuando la persona lo confirma de forma
/// explicita.
/// </remarks>
public enum EstadoRecomendacion
{
    /// <summary>Mostrada, a la espera de decision.</summary>
    Pendiente = 1,

    /// <summary>El usuario confirmo y se creo el movimiento correspondiente.</summary>
    Aceptada = 2,

    /// <summary>El usuario la ignoro.</summary>
    Descartada = 3,

    /// <summary>Dejo de tener sentido porque los datos cambiaron.</summary>
    Expirada = 4,
}
