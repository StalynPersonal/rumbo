using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Autenticacion;
using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Entidades.Identidad;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;
using Rumbo.Infraestructura.Correo;
using Rumbo.Infraestructura.Correo.Plantillas;
using Rumbo.Infraestructura.Persistencia;
using Rumbo.Infraestructura.Persistencia.Semilla;

namespace Rumbo.Infraestructura.Identidad;

/// <summary>
/// Implementa el ciclo de vida de la sesion: alta, acceso, renovacion y contrasenas.
/// </summary>
/// <remarks>
/// <para>
/// Vive en infraestructura porque depende de <see cref="UserManager{TUser}"/> de ASP.NET Core
/// Identity. La interfaz si esta en la capa de aplicacion, de modo que los controladores
/// dependen de la abstraccion y no de esta clase.
/// </para>
/// <para>
/// Todas sus operaciones se ejecutan SIN espacio activo: o todavia no se conoce (se esta
/// iniciando sesion) o se esta cambiando. Por eso trabaja con las tablas globales, que no
/// llevan filtro de aislamiento.
/// </para>
/// </remarks>
/// <param name="usuarios">Gestor de usuarios de Identity.</param>
/// <param name="contexto">Contexto de base de datos.</param>
/// <param name="tokens">Emisor de tokens.</param>
/// <param name="fechaHora">Proveedor de fecha y hora.</param>
/// <param name="enviadorCorreo">Enviador de correo.</param>
/// <param name="opcionesJwt">Parametros de los tokens.</param>
/// <param name="opcionesCorreo">Parametros del correo.</param>
/// <param name="registro">Registro de eventos.</param>
public partial class ServicioAutenticacion(
    UserManager<Usuario> usuarios,
    ContextoRumbo contexto,
    IServicioTokens tokens,
    IProveedorFechaHora fechaHora,
    IEnviadorCorreo enviadorCorreo,
    IOptions<OpcionesJwt> opcionesJwt,
    IOptions<OpcionesCorreo> opcionesCorreo,
    ILogger<ServicioAutenticacion> registro) : IServicioAutenticacion
{
    private readonly OpcionesJwt _jwt = opcionesJwt.Value;
    private readonly OpcionesCorreo _correo = opcionesCorreo.Value;

    /// <inheritdoc />
    public async Task<RespuestaAutenticacion> RegistrarAsync(
        SolicitudRegistrar solicitud,
        CancellationToken cancelacion = default)
    {
        var correoNormalizado = solicitud.Correo.Trim().ToLowerInvariant();
        var hashCodigo = tokens.CalcularHash(solicitud.CodigoInvitacion.Trim());

        var invitacion = await contexto.Invitaciones
            .FirstOrDefaultAsync(i => i.HashCodigo == hashCodigo, cancelacion)
            ?? throw new ExcepcionInvitacionInvalida("El código de invitación no es válido.");

        if (invitacion.Estado != EstadoInvitacion.Pendiente)
        {
            throw new ExcepcionInvitacionInvalida("Ese código de invitación ya se usó o fue anulado.");
        }

        if (invitacion.FechaExpiracion <= fechaHora.AhoraUtc)
        {
            invitacion.Estado = EstadoInvitacion.Expirada;
            await contexto.SaveChangesAsync(cancelacion);

            throw new ExcepcionInvitacionInvalida("El código de invitación ha caducado.");
        }

        // La invitacion esta ligada a un correo concreto: si no coincide, el codigo llego a
        // otras manos y no debe servir.
        if (!string.Equals(invitacion.Correo, correoNormalizado, StringComparison.OrdinalIgnoreCase))
        {
            throw new ExcepcionInvitacionInvalida(
                "El código de invitación fue emitido para otra dirección de correo.");
        }

        if (await usuarios.FindByEmailAsync(correoNormalizado) is not null)
        {
            throw new ExcepcionInvitacionInvalida("Ya existe una cuenta con ese correo.");
        }

        var usuario = new Usuario
        {
            Id = Guid.CreateVersion7(),
            UserName = correoNormalizado,
            Email = correoNormalizado,
            NombreCompleto = solicitud.NombreCompleto.Trim(),
            FechaCreacion = fechaHora.AhoraUtc,
            EmailConfirmed = true,
        };

        var resultado = await usuarios.CreateAsync(usuario, solicitud.Clave);

        if (!resultado.Succeeded)
        {
            throw new ExcepcionDominio(TraducirErroresDeClave(resultado.Errors));
        }

        var membresia = invitacion.Tipo == TipoInvitacion.Propietario
            ? CrearEspacioParaPropietario(usuario, invitacion)
            : await UnirAEspacioExistenteAsync(usuario, invitacion, cancelacion);

        invitacion.Estado = EstadoInvitacion.Aceptada;
        invitacion.FechaUso = fechaHora.AhoraUtc;
        invitacion.AceptadaPorUsuarioId = usuario.Id;

        await contexto.SaveChangesAsync(cancelacion);

        registro.LogInformation(
            "Alta completada para el usuario {UsuarioId} en el espacio {EspacioId}.",
            usuario.Id, membresia.EspacioId);

        return await ConstruirRespuestaAsync(usuario, membresia.EspacioId, null, cancelacion);
    }
}
