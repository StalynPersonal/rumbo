namespace Rumbo.Contratos.Autenticacion;

/// <summary>Datos para renovar el token de acceso.</summary>
/// <param name="TokenRenovacion">Token de renovacion vigente.</param>
public record SolicitudRenovar(string TokenRenovacion);

/// <summary>Datos para cerrar sesion.</summary>
/// <param name="TokenRenovacion">
/// Token que se quiere revocar. Cerrar sesion sin revocarlo dejaria una puerta abierta: el
/// token seguiria sirviendo para pedir credenciales nuevas durante semanas.
/// </param>
public record SolicitudCerrarSesion(string TokenRenovacion);

/// <summary>Datos para cambiar la contrasena estando autenticado.</summary>
/// <param name="ClaveActual">Contrasena vigente, para confirmar que es la persona.</param>
/// <param name="ClaveNueva">Contrasena nueva.</param>
public record SolicitudCambiarClave(string ClaveActual, string ClaveNueva);

/// <summary>Datos para pedir un enlace de restablecimiento.</summary>
/// <param name="Correo">Correo de la cuenta.</param>
public record SolicitudOlvideClave(string Correo);

/// <summary>Datos para fijar una contrasena nueva con el codigo recibido por correo.</summary>
/// <param name="Correo">Correo de la cuenta.</param>
/// <param name="Codigo">Codigo del enlace de restablecimiento.</param>
/// <param name="ClaveNueva">Contrasena nueva.</param>
public record SolicitudRestablecerClave(string Correo, string Codigo, string ClaveNueva);

/// <summary>Datos para cambiar de espacio activo sin volver a iniciar sesion.</summary>
/// <param name="EspacioId">Espacio al que se quiere cambiar.</param>
public record SolicitudCambiarEspacio(Guid EspacioId);
