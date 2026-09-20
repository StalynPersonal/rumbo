using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Enums;

namespace Rumbo.Dominio.Entidades.Soporte;

/// <summary>
/// Aviso dirigido a los miembros de un espacio: un pago que vence, un presupuesto al limite o
/// una meta alcanzada.
/// </summary>
/// <remarks>
/// <para>
/// En esta version las notificaciones se guardan y se muestran dentro de la aplicacion. No hay
/// envio push todavia.
/// </para>
/// <para>
/// Se persisten desde ya, en lugar de generarlas al vuelo, porque eso es lo que permitira
/// anadir push mas adelante sin tocar la logica que las produce: bastara con que un
/// <c>IDespachadorNotificaciones</c> recorra las pendientes y las entregue por el canal que
/// corresponda.
/// </para>
/// </remarks>
public class Notificacion : EntidadDeEspacio
{
    /// <summary>Motivo del aviso.</summary>
    public TipoNotificacion Tipo { get; set; }

    /// <summary>Titulo corto.</summary>
    public required string Titulo { get; set; }

    /// <summary>Texto del aviso.</summary>
    public required string Cuerpo { get; set; }

    /// <summary>
    /// Usuario concreto al que va dirigida, o <c>null</c> si es para todo el espacio.
    /// </summary>
    public Guid? UsuarioDestinoId { get; set; }

    /// <summary>
    /// Datos adicionales en JSON, por ejemplo el identificador de la meta implicada, para que
    /// la aplicacion pueda navegar directamente a la pantalla correspondiente.
    /// </summary>
    public string? Datos { get; set; }

    /// <summary>Momento en que debe mostrarse.</summary>
    public DateTimeOffset ProgramadaPara { get; set; }

    /// <summary>Momento en que se entrego.</summary>
    public DateTimeOffset? FechaEnvio { get; set; }

    /// <summary>Momento en que el usuario la leyo.</summary>
    public DateTimeOffset? FechaLectura { get; set; }

    /// <summary>Situacion de la notificacion.</summary>
    public EstadoNotificacion Estado { get; set; } = EstadoNotificacion.Pendiente;
}
