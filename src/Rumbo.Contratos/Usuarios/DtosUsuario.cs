namespace Rumbo.Contratos.Usuarios;

/// <summary>Perfil de la persona autenticada.</summary>
/// <param name="Id">Identificador del usuario.</param>
/// <param name="Correo">Correo de acceso.</param>
/// <param name="NombreCompleto">Nombre para mostrar.</param>
/// <param name="CulturaPreferida">Cultura para formatos y textos, por ejemplo <c>es-DO</c>.</param>
/// <param name="FechaCreacion">Cuando se dio de alta.</param>
/// <param name="UltimoAcceso">Ultimo inicio de sesion correcto.</param>
/// <param name="EsAdministradorPlataforma">Si gestiona la plataforma.</param>
/// <param name="CantidadEspacios">A cuantos espacios pertenece.</param>
public record PerfilUsuario(
    Guid Id,
    string Correo,
    string NombreCompleto,
    string CulturaPreferida,
    DateTimeOffset FechaCreacion,
    DateTimeOffset? UltimoAcceso,
    bool EsAdministradorPlataforma,
    int CantidadEspacios);

/// <summary>Datos que una persona puede cambiar de su propio perfil.</summary>
/// <param name="NombreCompleto">Nombre para mostrar.</param>
/// <param name="CulturaPreferida">Cultura para formatos, por ejemplo <c>es-DO</c>.</param>
/// <remarks>
/// El correo NO se puede cambiar desde aqui: es la credencial de acceso y cambiarlo sin
/// verificar la direccion nueva permitiria secuestrar una cuenta. Esa operacion necesita su
/// propio flujo con confirmacion por correo.
/// </remarks>
public record SolicitudActualizarPerfil(string NombreCompleto, string CulturaPreferida);
