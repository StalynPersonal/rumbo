using Rumbo.Dominio.Enums;

namespace Rumbo.Aplicacion.Contratos;

/// <summary>
/// Emite los tokens de acceso que autorizan cada peticion.
/// </summary>
/// <remarks>
/// La interfaz vive en la capa de aplicacion y la implementacion en infraestructura, para que
/// los servicios de negocio no dependan de como se firman los tokens.
/// </remarks>
public interface IServicioTokens
{
    /// <summary>
    /// Genera un token de acceso firmado.
    /// </summary>
    /// <param name="usuarioId">Usuario al que pertenece la sesion.</param>
    /// <param name="correo">Correo del usuario.</param>
    /// <param name="esAdministradorPlataforma">Si tiene el rol de plataforma.</param>
    /// <param name="espacioId">Espacio activo, o <c>null</c> si todavia no tiene ninguno.</param>
    /// <param name="rolEnEspacio">Rol dentro de ese espacio.</param>
    /// <returns>El token y el instante en que caduca.</returns>
    /// <remarks>
    /// El espacio y el rol viajan FIRMADOS dentro del token. El cliente no puede alterarlos
    /// sin invalidar la firma, y por eso el servidor nunca acepta un espacio enviado en el
    /// cuerpo o en una cabecera de la peticion.
    /// </remarks>
    (string Token, DateTimeOffset Expiracion) GenerarTokenAcceso(
        Guid usuarioId,
        string correo,
        bool esAdministradorPlataforma,
        Guid? espacioId,
        RolEspacio? rolEnEspacio);

    /// <summary>
    /// Genera un token de renovacion aleatorio.
    /// </summary>
    /// <returns>
    /// El valor en claro, que solo se entrega al cliente, y su hash, que es lo unico que se
    /// guarda en la base de datos.
    /// </returns>
    /// <remarks>
    /// Se guarda solo el hash por la misma razon que con las contrasenas: quien consiguiera
    /// leer la tabla no podria usar las sesiones abiertas.
    /// </remarks>
    (string Token, string Hash) GenerarTokenRenovacion();

    /// <summary>
    /// Calcula el hash de un token de renovacion para buscarlo en la base de datos.
    /// </summary>
    /// <param name="token">Token en claro recibido del cliente.</param>
    /// <returns>Su hash en hexadecimal.</returns>
    string CalcularHash(string token);
}
