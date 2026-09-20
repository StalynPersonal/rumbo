using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Rumbo.Contratos.Autenticacion;
using Rumbo.Dominio.Excepciones;
using Rumbo.Infraestructura.Correo.Plantillas;

namespace Rumbo.Infraestructura.Identidad;

/// <summary>
/// Parte del servicio de autenticacion dedicada a las contrasenas.
/// </summary>
public partial class ServicioAutenticacion
{
    /// <inheritdoc />
    public async Task CambiarClaveAsync(
        Guid usuarioId,
        SolicitudCambiarClave solicitud,
        CancellationToken cancelacion = default)
    {
        var usuario = await usuarios.FindByIdAsync(usuarioId.ToString())
            ?? throw new ExcepcionCredencialesInvalidas();

        var resultado = await usuarios.ChangePasswordAsync(
            usuario, solicitud.ClaveActual, solicitud.ClaveNueva);

        if (!resultado.Succeeded)
        {
            throw new ExcepcionDominio(TraducirErroresDeClave(resultado.Errors));
        }

        // Se revocan TODAS las sesiones abiertas. Muchas veces alguien cambia su contraseña
        // precisamente porque sospecha que otra persona la conoce: dejar vivas las demas
        // sesiones anularia el motivo del cambio.
        await RevocarTodosLosTokensAsync(usuarioId, cancelacion);

        registro.LogInformation(
            "El usuario {UsuarioId} cambió su contraseña. Se revocaron sus sesiones.", usuarioId);
    }

    /// <inheritdoc />
    public async Task SolicitarRestablecerClaveAsync(
        SolicitudOlvideClave solicitud,
        CancellationToken cancelacion = default)
    {
        var correoNormalizado = solicitud.Correo.Trim().ToLowerInvariant();
        var usuario = await usuarios.FindByEmailAsync(correoNormalizado);

        // Si el correo no existe, se termina SIN avisar y sin enviar nada. El controlador
        // responde lo mismo en ambos casos. Decir "ese correo no está registrado" permitiria
        // a cualquiera averiguar quien usa Rumbo probando direcciones.
        if (usuario is null || !usuario.Activo)
        {
            registro.LogInformation(
                "Se pidió restablecer la contraseña de un correo no registrado o inactivo.");

            return;
        }

        var codigo = await usuarios.GeneratePasswordResetTokenAsync(usuario);

        var (asunto, cuerpo) = PlantillasCorreo.RestablecerClave(
            usuario.NombreCompleto, codigo, correoNormalizado, _correo.UrlBase);

        // Sin espacio: restablecer la contrasena pertenece a la persona, que puede estar en
        // varios hogares. Sale siempre por el servidor de plataforma.
        await enviadorCorreo.EnviarAsync(correoNormalizado, asunto, cuerpo, espacioId: null, cancelacion);

        registro.LogInformation(
            "Se envió el enlace de restablecimiento al usuario {UsuarioId}.", usuario.Id);
    }

    /// <inheritdoc />
    public async Task RestablecerClaveAsync(
        SolicitudRestablecerClave solicitud,
        CancellationToken cancelacion = default)
    {
        var correoNormalizado = solicitud.Correo.Trim().ToLowerInvariant();
        var usuario = await usuarios.FindByEmailAsync(correoNormalizado);

        if (usuario is null || !usuario.Activo)
        {
            // Mismo mensaje que si el codigo fuera incorrecto, por la misma razon.
            throw new ExcepcionTokenInvalido(
                "El enlace no es válido o ha caducado. Solicita uno nuevo.");
        }

        var resultado = await usuarios.ResetPasswordAsync(
            usuario, solicitud.Codigo, solicitud.ClaveNueva);

        if (!resultado.Succeeded)
        {
            var esCodigoInvalido = resultado.Errors.Any(e => e.Code == "InvalidToken");

            throw esCodigoInvalido
                ? new ExcepcionTokenInvalido("El enlace no es válido o ha caducado. Solicita uno nuevo.")
                : new ExcepcionDominio(TraducirErroresDeClave(resultado.Errors));
        }

        // Igual que al cambiar la contraseña: restablecerla cierra todas las sesiones.
        await RevocarTodosLosTokensAsync(usuario.Id, cancelacion);

        registro.LogInformation(
            "El usuario {UsuarioId} restableció su contraseña. Se revocaron sus sesiones.",
            usuario.Id);
    }

    /// <summary>Convierte los errores de contrasena de Identity a mensajes en espanol.</summary>
    /// <param name="errores">Errores devueltos por Identity.</param>
    /// <returns>Un unico mensaje comprensible para la persona.</returns>
    /// <remarks>
    /// Identity devuelve sus mensajes en ingles. Decirle a alguien
    /// "Passwords must have at least one digit" en una aplicacion en espanol es descuidado.
    /// </remarks>
    private static string TraducirErroresDeClave(IEnumerable<IdentityError> errores)
    {
        var mensajes = errores.Select(error => error.Code switch
        {
            "PasswordTooShort" => "La contraseña debe tener al menos 12 caracteres.",
            "PasswordRequiresDigit" => "La contraseña debe incluir al menos un número.",
            "PasswordRequiresLower" => "La contraseña debe incluir al menos una letra minúscula.",
            "PasswordRequiresUpper" => "La contraseña debe incluir al menos una letra mayúscula.",
            "PasswordRequiresNonAlphanumeric" => "La contraseña debe incluir un símbolo.",
            "PasswordRequiresUniqueChars" => "La contraseña repite demasiado los mismos caracteres.",
            "PasswordMismatch" => "La contraseña actual no es correcta.",
            "DuplicateUserName" or "DuplicateEmail" => "Ya existe una cuenta con ese correo.",
            "InvalidEmail" => "El correo no tiene un formato válido.",
            _ => error.Description,
        });

        return string.Join(" ", mensajes.Distinct());
    }
}
