namespace Rumbo.Dominio.Enums;

/// <summary>
/// Situacion de una notificacion en su ciclo de vida.
/// </summary>
public enum EstadoNotificacion
{
    /// <summary>Creada y a la espera de mostrarse o enviarse.</summary>
    Pendiente = 1,

    /// <summary>Ya se entrego por el canal correspondiente.</summary>
    Enviada = 2,

    /// <summary>El usuario la vio.</summary>
    Leida = 3,

    /// <summary>El usuario la cerro sin actuar.</summary>
    Descartada = 4,
}
