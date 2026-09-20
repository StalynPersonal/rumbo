using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Enums;

namespace Rumbo.Dominio.Entidades.Soporte;

/// <summary>
/// Huella de quien hizo que, cuando y sobre que registro.
/// </summary>
/// <remarks>
/// <para>
/// Las filas las escribe <c>InterceptorAuditoria</c> al guardar cambios, no el codigo de cada
/// servicio: si dependiera de que alguien se acuerde de registrar la accion, el historial
/// tendria huecos justo en las operaciones menos habituales.
/// </para>
/// <para>
/// <b>Que NO se guarda aqui.</b> Nunca contrasenas, tokens, ni el detalle de importes junto a
/// datos personales. La auditoria sirve para saber que alguien modifico un movimiento, no para
/// reconstruir las finanzas de nadie desde los logs.
/// </para>
/// <para>
/// <see cref="EspacioId"/> es opcional porque hay acciones sin espacio: un inicio de sesion
/// fallido o una gestion del administrador de plataforma.
/// </para>
/// </remarks>
public class RegistroAuditoria : EntidadBase
{
    /// <summary>Espacio afectado, si la accion ocurrio dentro de uno.</summary>
    public Guid? EspacioId { get; set; }

    /// <summary>Usuario que realizo la accion.</summary>
    public Guid? UsuarioId { get; set; }

    /// <summary>Correo del usuario en el momento de la accion.</summary>
    /// <remarks>
    /// Se guarda por separado para que el historial siga siendo legible aunque el usuario
    /// cambie de correo despues.
    /// </remarks>
    public string? CorreoUsuario { get; set; }

    /// <summary>Operacion realizada.</summary>
    public AccionAuditoria Accion { get; set; }

    /// <summary>Nombre de la entidad afectada, por ejemplo <c>Movimiento</c>.</summary>
    public string? TipoEntidad { get; set; }

    /// <summary>Identificador del registro afectado.</summary>
    public Guid? EntidadId { get; set; }

    /// <summary>
    /// Cambios aplicados, en JSON, con la forma <c>{"Campo": {"antes": x, "despues": y}}</c>.
    /// </summary>
    public string? Cambios { get; set; }

    /// <summary>Instante exacto de la accion.</summary>
    public DateTimeOffset FechaHora { get; set; }

    /// <summary>Direccion IP desde la que se realizo.</summary>
    public string? DireccionIp { get; set; }

    /// <summary>
    /// Identificador de la peticion HTTP, para cruzar esta fila con los registros de
    /// Application Insights.
    /// </summary>
    public string? IdCorrelacion { get; set; }

    /// <summary>Descripcion legible de lo ocurrido.</summary>
    public string? Descripcion { get; set; }

    /// <summary>Indica si la accion se completo o fue rechazada.</summary>
    public bool Exitosa { get; set; } = true;
}
