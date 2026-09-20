namespace Rumbo.Contratos.Autenticacion;

/// <summary>
/// Credenciales y contexto que devuelve la API tras un inicio de sesion correcto.
/// </summary>
/// <param name="TokenAcceso">
/// JWT de corta duracion que autoriza cada peticion. Se envia en la cabecera
/// <c>Authorization: Bearer</c>.
/// </param>
/// <param name="ExpiraEn">Instante en que caduca el token de acceso.</param>
/// <param name="TokenRenovacion">
/// Token opaco de larga duracion para obtener un token de acceso nuevo sin volver a pedir la
/// contrasena. En la app movil se guarda en SecureStorage, nunca en preferencias normales.
/// </param>
/// <param name="Usuario">Datos de la persona que inicio sesion.</param>
/// <param name="EspacioActivo">
/// Espacio sobre el que operan las peticiones. Es <c>null</c> si la persona todavia no
/// pertenece a ninguno.
/// </param>
/// <param name="EspaciosDisponibles">
/// Todos los espacios a los que puede cambiar sin volver a autenticarse.
/// </param>
public record RespuestaAutenticacion(
    string TokenAcceso,
    DateTimeOffset ExpiraEn,
    string TokenRenovacion,
    UsuarioAutenticado Usuario,
    EspacioResumen? EspacioActivo,
    IReadOnlyList<EspacioResumen> EspaciosDisponibles);

/// <summary>Datos basicos de la persona autenticada.</summary>
/// <param name="Id">Identificador del usuario.</param>
/// <param name="Correo">Correo de acceso.</param>
/// <param name="NombreCompleto">Nombre para mostrar.</param>
/// <param name="EsAdministradorPlataforma">
/// Si gestiona la plataforma. Ese rol administra espacios e invitaciones, pero NO puede ver
/// datos financieros de ningun hogar.
/// </param>
public record UsuarioAutenticado(
    Guid Id,
    string Correo,
    string NombreCompleto,
    bool EsAdministradorPlataforma);

/// <summary>Resumen de un espacio al que pertenece la persona.</summary>
/// <param name="Id">Identificador del espacio.</param>
/// <param name="Nombre">Nombre del hogar, pareja, familia o negocio.</param>
/// <param name="Tipo">Naturaleza del espacio.</param>
/// <param name="Rol">Rol que ejerce la persona en ese espacio.</param>
/// <param name="MonedaBase">Moneda en la que se consolidan sus informes.</param>
public record EspacioResumen(
    Guid Id,
    string Nombre,
    string Tipo,
    string Rol,
    string MonedaBase);
