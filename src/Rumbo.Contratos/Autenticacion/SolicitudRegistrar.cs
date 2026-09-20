namespace Rumbo.Contratos.Autenticacion;

/// <summary>
/// Datos para darse de alta en Rumbo.
/// </summary>
/// <param name="CodigoInvitacion">
/// Codigo recibido por correo. Es OBLIGATORIO: en Rumbo no existe el auto-registro, toda alta
/// nace de una invitacion.
/// </param>
/// <param name="Correo">Correo de la persona. Debe coincidir con el de la invitacion.</param>
/// <param name="NombreCompleto">Nombre para mostrar.</param>
/// <param name="Clave">Contrasena elegida.</param>
public record SolicitudRegistrar(
    string CodigoInvitacion,
    string Correo,
    string NombreCompleto,
    string Clave);
