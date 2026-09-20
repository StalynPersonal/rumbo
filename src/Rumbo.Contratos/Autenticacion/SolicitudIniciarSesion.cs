namespace Rumbo.Contratos.Autenticacion;

/// <summary>Datos para iniciar sesion.</summary>
/// <param name="Correo">Correo con el que se registro la persona.</param>
/// <param name="Clave">Contrasena en claro. Viaja por HTTPS y nunca se registra en los logs.</param>
public record SolicitudIniciarSesion(string Correo, string Clave);
