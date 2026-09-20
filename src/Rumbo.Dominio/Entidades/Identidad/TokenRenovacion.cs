using Rumbo.Dominio.Comun;

namespace Rumbo.Dominio.Entidades.Identidad;

/// <summary>
/// Token de larga duracion que permite obtener un token de acceso nuevo sin volver a pedir la
/// contrasena.
/// </summary>
/// <remarks>
/// <para>
/// El token de acceso (JWT) dura 15 minutos a proposito: si lo roban, la ventana de dano es
/// corta. Para que el usuario no tenga que iniciar sesion cada 15 minutos existe este token,
/// que vive 30 dias.
/// </para>
/// <para>
/// <b>Rotacion.</b> Cada vez que se usa, se revoca y se emite uno nuevo encadenado mediante
/// <see cref="ReemplazadoPorTokenId"/>. Si alguna vez llega un token ya revocado, significa que
/// dos partes tienen el mismo token, es decir, que hubo un robo: en ese caso se revoca toda la
/// cadena y se obliga a iniciar sesion de nuevo.
/// </para>
/// <para>
/// Como en las invitaciones, en la base de datos se guarda solo el hash del token.
/// </para>
/// </remarks>
public class TokenRenovacion : EntidadBase
{
    /// <summary>Usuario al que pertenece el token.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Hash SHA-256 del token. El valor en claro solo lo tiene el cliente.</summary>
    public required string Hash { get; set; }

    /// <summary>Instante de emision.</summary>
    public DateTimeOffset FechaCreacion { get; set; }

    /// <summary>Instante a partir del cual deja de servir.</summary>
    public DateTimeOffset FechaExpiracion { get; set; }

    /// <summary>Instante en que se revoco, o <c>null</c> si sigue vigente.</summary>
    public DateTimeOffset? FechaRevocacion { get; set; }

    /// <summary>
    /// Token que sustituyo a este al rotarlo. Encadenar los tokens permite revocar toda la
    /// familia de golpe cuando se detecta una reutilizacion sospechosa.
    /// </summary>
    public Guid? ReemplazadoPorTokenId { get; set; }

    /// <summary>Direccion IP desde la que se emitio, para poder auditar un acceso extrano.</summary>
    public string? DireccionIp { get; set; }

    /// <summary>Descripcion del dispositivo o navegador que lo solicito.</summary>
    public string? Dispositivo { get; set; }

    /// <summary>Indica si el token sigue siendo utilizable en el instante indicado.</summary>
    /// <param name="ahora">Instante con el que comparar.</param>
    /// <returns><c>true</c> si no esta revocado ni expirado.</returns>
    public bool EstaVigente(DateTimeOffset ahora) =>
        FechaRevocacion is null && FechaExpiracion > ahora;
}
