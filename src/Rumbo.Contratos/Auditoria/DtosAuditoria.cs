namespace Rumbo.Contratos.Auditoria;

/// <summary>Entrada del historial de auditoria.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="UsuarioId">Quien realizo la accion.</param>
/// <param name="CorreoUsuario">Su correo en el momento de la accion.</param>
/// <param name="Accion">Creacion, Actualizacion, Eliminacion, InicioSesion...</param>
/// <param name="TipoEntidad">Entidad afectada, por ejemplo <c>Movimiento</c>.</param>
/// <param name="EntidadId">Identificador del registro afectado.</param>
/// <param name="FechaHora">Instante exacto.</param>
/// <param name="Descripcion">Descripcion legible de lo ocurrido.</param>
/// <param name="Exitosa">Si la accion se completo o fue rechazada.</param>
/// <remarks>
/// <b>No incluye el detalle de los cambios.</b> Ese campo guarda un JSON con los valores
/// anteriores y nuevos, que en un movimiento son importes: exponerlo por la API convertiria
/// el historial en una segunda via para leer las finanzas, saltandose los permisos del
/// modulo correspondiente.
/// </remarks>
public record EntradaAuditoria(
    Guid Id,
    Guid? UsuarioId,
    string? CorreoUsuario,
    string Accion,
    string? TipoEntidad,
    Guid? EntidadId,
    DateTimeOffset FechaHora,
    string? Descripcion,
    bool Exitosa);

/// <summary>Filtros para consultar el historial.</summary>
/// <param name="Desde">Fecha inicial.</param>
/// <param name="Hasta">Fecha final.</param>
/// <param name="UsuarioId">Filtrar por quien hizo la accion.</param>
/// <param name="TipoEntidad">Filtrar por tipo de entidad.</param>
/// <param name="EntidadId">Filtrar por un registro concreto.</param>
/// <param name="Accion">Filtrar por tipo de accion.</param>
/// <param name="Pagina">Numero de pagina, empezando en 1.</param>
/// <param name="TamanoPagina">Elementos por pagina.</param>
public record FiltroAuditoria(
    DateTimeOffset? Desde = null,
    DateTimeOffset? Hasta = null,
    Guid? UsuarioId = null,
    string? TipoEntidad = null,
    Guid? EntidadId = null,
    string? Accion = null,
    int Pagina = 1,
    int TamanoPagina = 50);
