using Microsoft.EntityFrameworkCore;

using Rumbo.Contratos.Autenticacion;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Infraestructura.Identidad;

/// <summary>
/// Parte del servicio de autenticacion dedicada al acceso y a la renovacion de la sesion.
/// </summary>
public partial class ServicioAutenticacion
{
    /// <inheritdoc />
    public async Task<RespuestaAutenticacion> IniciarSesionAsync(
        SolicitudIniciarSesion solicitud,
        string? direccionIp,
        CancellationToken cancelacion = default)
    {
        var correoNormalizado = solicitud.Correo.Trim().ToLowerInvariant();
        var usuario = await usuarios.FindByEmailAsync(correoNormalizado);

        // MISMO error si el usuario no existe y si la clave es incorrecta. Distinguirlos
        // convertiria el formulario de acceso en una herramienta para averiguar quien tiene
        // cuenta en Rumbo probando direcciones.
        if (usuario is null || !usuario.Activo)
        {
            registro.LogWarning(
                "Intento de acceso fallido para un correo no registrado o inactivo desde {Ip}.",
                direccionIp);

            throw new ExcepcionCredencialesInvalidas();
        }

        if (await usuarios.IsLockedOutAsync(usuario))
        {
            throw new ExcepcionCuentaBloqueada(
                usuario.LockoutEnd ?? fechaHora.AhoraUtc.AddMinutes(15));
        }

        if (!await usuarios.CheckPasswordAsync(usuario, solicitud.Clave))
        {
            // Cuenta el intento fallido: tras cinco, Identity bloquea la cuenta 15 minutos.
            // Es lo que hace inviable probar contrasenas por fuerza bruta.
            await usuarios.AccessFailedAsync(usuario);

            registro.LogWarning(
                "Contraseña incorrecta para el usuario {UsuarioId} desde {Ip}.",
                usuario.Id, direccionIp);

            throw new ExcepcionCredencialesInvalidas();
        }

        await usuarios.ResetAccessFailedCountAsync(usuario);

        usuario.UltimoAcceso = fechaHora.AhoraUtc;
        await usuarios.UpdateAsync(usuario);

        var espacioActivo = await PrimerEspacioActivoAsync(usuario.Id, cancelacion);

        registro.LogInformation(
            "Inicio de sesión correcto del usuario {UsuarioId} desde {Ip}.",
            usuario.Id, direccionIp);

        return await ConstruirRespuestaAsync(usuario, espacioActivo, direccionIp, cancelacion);
    }

    /// <inheritdoc />
    public async Task<RespuestaAutenticacion> RenovarAsync(
        SolicitudRenovar solicitud,
        string? direccionIp,
        CancellationToken cancelacion = default)
    {
        var hash = tokens.CalcularHash(solicitud.TokenRenovacion);

        var token = await contexto.TokensRenovacion
            .FirstOrDefaultAsync(t => t.Hash == hash, cancelacion)
            ?? throw new ExcepcionTokenInvalido("La sesión no es válida. Vuelve a iniciar sesión.");

        // DETECCION DE ROBO DE TOKEN.
        // Los tokens rotan: cada uno se usa una sola vez. Que llegue uno ya revocado
        // significa que existen dos copias circulando, es decir, que alguien lo intercepto.
        // No hay forma de saber cual de las dos partes es la legitima, asi que se revoca la
        // cadena entera y ambas tendran que volver a iniciar sesion. Es molesto, y es lo
        // correcto: la alternativa es dejar al atacante dentro.
        if (token.FechaRevocacion is not null)
        {
            registro.LogWarning(
                "Se reutilizó un token de renovación ya revocado del usuario {UsuarioId} desde "
                + "{Ip}. Se revocan todas sus sesiones por sospecha de robo.",
                token.UsuarioId, direccionIp);

            await RevocarTodosLosTokensAsync(token.UsuarioId, cancelacion);

            throw new ExcepcionTokenInvalido(
                "Tu sesión se cerró por seguridad. Vuelve a iniciar sesión.");
        }

        if (token.FechaExpiracion <= fechaHora.AhoraUtc)
        {
            throw new ExcepcionTokenInvalido("La sesión ha caducado. Vuelve a iniciar sesión.");
        }

        var usuario = await usuarios.FindByIdAsync(token.UsuarioId.ToString());

        if (usuario is null || !usuario.Activo)
        {
            throw new ExcepcionTokenInvalido("La cuenta ya no está disponible.");
        }

        var espacioActivo = await PrimerEspacioActivoAsync(usuario.Id, cancelacion);

        // El token usado queda revocado en la misma operacion que emite el nuevo.
        token.FechaRevocacion = fechaHora.AhoraUtc;

        var respuesta = await ConstruirRespuestaAsync(usuario, espacioActivo, direccionIp, cancelacion);

        return respuesta;
    }

    /// <inheritdoc />
    public async Task CerrarSesionAsync(
        SolicitudCerrarSesion solicitud,
        CancellationToken cancelacion = default)
    {
        var hash = tokens.CalcularHash(solicitud.TokenRenovacion);

        var token = await contexto.TokensRenovacion
            .FirstOrDefaultAsync(t => t.Hash == hash && t.FechaRevocacion == null, cancelacion);

        // Si el token no existe o ya estaba revocado no se avisa de nada: cerrar sesion debe
        // funcionar siempre desde el punto de vista del cliente.
        if (token is null)
        {
            return;
        }

        token.FechaRevocacion = fechaHora.AhoraUtc;
        await contexto.SaveChangesAsync(cancelacion);

        registro.LogInformation("Sesión cerrada para el usuario {UsuarioId}.", token.UsuarioId);
    }

    /// <inheritdoc />
    public async Task<RespuestaAutenticacion> CambiarEspacioAsync(
        Guid usuarioId,
        SolicitudCambiarEspacio solicitud,
        string? direccionIp,
        CancellationToken cancelacion = default)
    {
        // Se comprueba CONTRA LA BASE DE DATOS que la membresia existe y esta activa. Que el
        // cliente diga que pertenece a ese espacio no prueba nada.
        var tieneAcceso = await contexto.MembresiasEspacio.AnyAsync(
            m => m.UsuarioId == usuarioId
                 && m.EspacioId == solicitud.EspacioId
                 && m.Estado == EstadoMembresia.Activa,
            cancelacion);

        if (!tieneAcceso)
        {
            throw new ExcepcionSinAccesoAlEspacio();
        }

        var usuario = await usuarios.FindByIdAsync(usuarioId.ToString())
            ?? throw new ExcepcionCredencialesInvalidas();

        return await ConstruirRespuestaAsync(usuario, solicitud.EspacioId, direccionIp, cancelacion);
    }

    /// <summary>Devuelve el primer espacio activo de un usuario, o <c>null</c> si no tiene.</summary>
    /// <param name="usuarioId">Usuario del que se buscan las membresias.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Identificador del espacio o <c>null</c>.</returns>
    private async Task<Guid?> PrimerEspacioActivoAsync(Guid usuarioId, CancellationToken cancelacion) =>
        await contexto.MembresiasEspacio
            .Where(m => m.UsuarioId == usuarioId && m.Estado == EstadoMembresia.Activa)
            .OrderBy(m => m.FechaIngreso)
            .Select(m => (Guid?)m.EspacioId)
            .FirstOrDefaultAsync(cancelacion);

    /// <summary>Revoca todos los tokens de renovacion vigentes de un usuario.</summary>
    /// <param name="usuarioId">Usuario afectado.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando se revocan.</returns>
    private async Task RevocarTodosLosTokensAsync(Guid usuarioId, CancellationToken cancelacion)
    {
        var ahora = fechaHora.AhoraUtc;

        await contexto.TokensRenovacion
            .Where(t => t.UsuarioId == usuarioId && t.FechaRevocacion == null)
            .ExecuteUpdateAsync(
                actualizacion => actualizacion.SetProperty(t => t.FechaRevocacion, ahora),
                cancelacion);
    }
}
