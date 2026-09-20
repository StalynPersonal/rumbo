namespace Rumbo.Contratos.Administracion;

/// <summary>
/// Espacio tal como lo ve el administrador de plataforma.
/// </summary>
/// <remarks>
/// <b>No incluye ningun dato financiero</b>: ni saldos, ni movimientos, ni metas. El
/// administrador gestiona altas y estados, no finanzas ajenas.
/// </remarks>
/// <param name="Id">Identificador del espacio.</param>
/// <param name="Nombre">Nombre del hogar.</param>
/// <param name="Tipo">Personal, Pareja, Familia o Negocio.</param>
/// <param name="Estado">Activo o Suspendido.</param>
/// <param name="MonedaBase">Moneda de consolidacion.</param>
/// <param name="FechaCreacion">Cuando se creo.</param>
/// <param name="CantidadMiembros">Cuantas personas tienen acceso.</param>
/// <param name="CorreoPropietario">Correo de quien lo creo, para poder contactarlo.</param>
public record EspacioAdminResumen(
    Guid Id,
    string Nombre,
    string Tipo,
    string Estado,
    string MonedaBase,
    DateTimeOffset FechaCreacion,
    int CantidadMiembros,
    string? CorreoPropietario);

/// <summary>Usuario tal como lo ve el administrador de plataforma.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Correo">Correo de acceso.</param>
/// <param name="NombreCompleto">Nombre para mostrar.</param>
/// <param name="Activo">Si la cuenta esta habilitada.</param>
/// <param name="EsAdministradorPlataforma">Si gestiona la plataforma.</param>
/// <param name="FechaCreacion">Cuando se dio de alta.</param>
/// <param name="UltimoAcceso">Ultimo inicio de sesion correcto.</param>
/// <param name="CantidadEspacios">A cuantos espacios pertenece.</param>
public record UsuarioAdminResumen(
    Guid Id,
    string Correo,
    string NombreCompleto,
    bool Activo,
    bool EsAdministradorPlataforma,
    DateTimeOffset FechaCreacion,
    DateTimeOffset? UltimoAcceso,
    int CantidadEspacios);

/// <summary>
/// Metricas agregadas de la plataforma.
/// </summary>
/// <remarks>
/// Son recuentos, nunca importes. Saber cuantos hogares hay es gestion; saber cuanto dinero
/// mueven seria acceder a sus finanzas.
/// </remarks>
/// <param name="TotalEspacios">Espacios creados.</param>
/// <param name="EspaciosActivos">Espacios no suspendidos.</param>
/// <param name="TotalUsuarios">Cuentas creadas.</param>
/// <param name="UsuariosActivos">Cuentas habilitadas.</param>
/// <param name="UsuariosConAccesoUltimos30Dias">Cuentas que entraron en el ultimo mes.</param>
/// <param name="InvitacionesPendientes">Invitaciones emitidas y sin usar.</param>
public record MetricasPlataforma(
    int TotalEspacios,
    int EspaciosActivos,
    int TotalUsuarios,
    int UsuariosActivos,
    int UsuariosConAccesoUltimos30Dias,
    int InvitacionesPendientes);

/// <summary>Datos para cambiar el estado de un espacio.</summary>
/// <param name="Estado">Activo o Suspendido.</param>
/// <param name="Motivo">Razon del cambio, que queda en la auditoria.</param>
public record SolicitudCambiarEstadoEspacio(string Estado, string? Motivo);
