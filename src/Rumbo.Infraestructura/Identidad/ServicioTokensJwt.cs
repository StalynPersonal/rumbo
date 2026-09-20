using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Dominio.Autorizacion;
using Rumbo.Dominio.Enums;

namespace Rumbo.Infraestructura.Identidad;

/// <summary>
/// Emite los tokens de acceso (JWT) y los de renovacion.
/// </summary>
/// <param name="opciones">Parametros de firma y caducidad.</param>
/// <param name="fechaHora">Proveedor de la fecha y hora actuales.</param>
public class ServicioTokensJwt(
    IOptions<OpcionesJwt> opciones,
    IProveedorFechaHora fechaHora) : IServicioTokens
{
    private readonly OpcionesJwt _opciones = opciones.Value;

    /// <inheritdoc />
    public (string Token, DateTimeOffset Expiracion) GenerarTokenAcceso(
        Guid usuarioId,
        string correo,
        bool esAdministradorPlataforma,
        Guid? espacioId,
        RolEspacio? rolEnEspacio)
    {
        var expiracion = fechaHora.AhoraUtc.AddMinutes(_opciones.MinutosTokenAcceso);

        var reclamaciones = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
            new(JwtRegisteredClaimNames.Email, correo),

            // Identificador unico del token. Permite rastrear una sesion concreta en la
            // auditoria sin tener que guardar el token entero.
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
        };

        if (esAdministradorPlataforma)
        {
            reclamaciones.Add(new Claim(ClaimTypes.Role, RolesPlataforma.AdministradorPlataforma));
        }

        if (espacioId.HasValue && rolEnEspacio.HasValue)
        {
            reclamaciones.Add(new Claim(ClaimsRumbo.Espacio, espacioId.Value.ToString()));
            reclamaciones.Add(new Claim(ClaimsRumbo.RolEspacio, rolEnEspacio.Value.ToString()));

            // Los permisos se incrustan en el token para no consultar la base de datos en
            // cada comprobacion de autorizacion. El contrapeso es que un cambio de rol no
            // surte efecto hasta que el token caduca; con 15 minutos de vida, es asumible.
            // La membresia SI se verifica contra la base en cada peticion, de modo que
            // revocar un acceso por completo es inmediato.
            foreach (var permiso in MapaPermisos.ParaRol(rolEnEspacio.Value))
            {
                reclamaciones.Add(new Claim(ClaimsRumbo.Permiso, permiso));
            }
        }

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.ClaveFirma));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opciones.Emisor,
            audience: _opciones.Audiencia,
            claims: reclamaciones,
            notBefore: fechaHora.AhoraUtc.UtcDateTime,
            expires: expiracion.UtcDateTime,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiracion);
    }

    /// <inheritdoc />
    public (string Token, string Hash) GenerarTokenRenovacion()
    {
        // 256 bits de aleatoriedad criptografica. No se usa Guid: los Guid no estan pensados
        // para ser impredecibles y adivinar uno daria acceso a una sesion ajena.
        var bytes = RandomNumberGenerator.GetBytes(32);

        // Base64 con alfabeto seguro para URL, para que el token pueda viajar en un enlace
        // sin necesidad de escaparlo.
        var token = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        return (token, CalcularHash(token));
    }

    /// <inheritdoc />
    public string CalcularHash(string token)
    {
        // SHA-256 sin sal y sin estiramiento, a diferencia de una contrasena. Es correcto
        // aqui: el token ya tiene 256 bits de entropia aleatoria, asi que no hay diccionario
        // ni fuerza bruta que valga. Estirarlo solo anadiria coste a cada renovacion.
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        return Convert.ToHexStringLower(bytes);
    }
}
