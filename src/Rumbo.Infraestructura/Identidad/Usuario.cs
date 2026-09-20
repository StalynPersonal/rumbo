using Microsoft.AspNetCore.Identity;

namespace Rumbo.Infraestructura.Identidad;

/// <summary>
/// Persona que puede iniciar sesion en Rumbo.
/// </summary>
/// <remarks>
/// <para>
/// Hereda de <see cref="IdentityUser{TKey}"/> para aprovechar todo lo que ASP.NET Core
/// Identity ya resuelve bien: el hash PBKDF2 de la contrasena, el bloqueo por intentos
/// fallidos, la confirmacion de correo, los tokens de restablecimiento y, mas adelante, el
/// segundo factor. Escribir esa criptografia a mano seria asumir un riesgo sin ninguna ventaja.
/// </para>
/// <para>
/// <b>Por que vive en Infraestructura y no en Dominio.</b> <c>IdentityUser</c> viene de un
/// paquete externo, y la regla del proyecto es que <c>Rumbo.Dominio</c> no dependa de nada.
/// Las entidades del dominio se refieren a las personas por su <c>Guid</c>, sin navegar hasta
/// aqui: el negocio no necesita saber como se autentica alguien.
/// </para>
/// <para>
/// <b>La pertenencia a un espacio NO esta aqui.</b> Vive en <c>MembresiaEspacio</c>, porque una
/// misma persona puede pertenecer a varios espacios con roles distintos. Meter un
/// <c>EspacioId</c> en el usuario habria hecho imposible ese caso.
/// </para>
/// </remarks>
public class Usuario : IdentityUser<Guid>
{
    /// <summary>Nombre completo de la persona.</summary>
    public required string NombreCompleto { get; set; }

    /// <summary>Cultura preferida para formatos y textos, por ejemplo <c>es-DO</c>.</summary>
    public string CulturaPreferida { get; set; } = "es-DO";

    /// <summary>Instante de alta.</summary>
    public DateTimeOffset FechaCreacion { get; set; }

    /// <summary>Instante del ultimo inicio de sesion correcto.</summary>
    public DateTimeOffset? UltimoAcceso { get; set; }

    /// <summary>Indica si la cuenta sigue habilitada.</summary>
    /// <remarks>
    /// Es distinto de <c>LockoutEnd</c> de Identity, que es un bloqueo temporal automatico por
    /// intentos fallidos. Esto es una desactivacion deliberada.
    /// </remarks>
    public bool Activo { get; set; } = true;
}
