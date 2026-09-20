using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Invitaciones;
using Rumbo.Dominio.Entidades.Identidad;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;
using Rumbo.Infraestructura.Correo;
using Rumbo.Infraestructura.Correo.Plantillas;
using Rumbo.Infraestructura.Persistencia;

namespace Rumbo.Infraestructura.Identidad;

/// <summary>
/// Emite y gestiona los codigos de invitacion, unica via de alta en Rumbo.
/// </summary>
/// <remarks>
/// <para>
/// Un administrador de plataforma invita a un futuro propietario, y ese propietario invita
/// despues a su pareja o a su familia. No hay superficie de registro abierta al publico.
/// </para>
/// <para>
/// <b>El codigo se muestra una sola vez.</b> En la base de datos solo queda su hash, igual
/// que con una contrasena: quien leyera la tabla no podria usar las invitaciones pendientes.
/// </para>
/// </remarks>
/// <param name="contexto">Contexto de base de datos.</param>
/// <param name="usuarios">Gestor de usuarios de Identity.</param>
/// <param name="tokens">Generador de codigos y hashes.</param>
/// <param name="fechaHora">Proveedor de fecha y hora.</param>
/// <param name="enviadorCorreo">Enviador de correo.</param>
/// <param name="opcionesCorreo">Parametros del correo.</param>
/// <param name="registro">Registro de eventos.</param>
public partial class ServicioInvitaciones(
    ContextoRumbo contexto,
    UserManager<Usuario> usuarios,
    IServicioTokens tokens,
    IProveedorFechaHora fechaHora,
    IEnviadorCorreo enviadorCorreo,
    IOptions<OpcionesCorreo> opcionesCorreo,
    ILogger<ServicioInvitaciones> registro) : IServicioInvitaciones
{
    /// <summary>Dias que un codigo sigue siendo valido.</summary>
    /// <remarks>
    /// Una semana es tiempo de sobra para aceptar, y suficientemente corto para que un codigo
    /// olvidado en un buzon no siga abriendo la puerta meses despues.
    /// </remarks>
    private const int DiasValidez = 7;

    /// <summary>Maximo de invitaciones pendientes por espacio.</summary>
    /// <remarks>
    /// Sin limite, una cuenta comprometida podria usarse para enviar correo masivo desde
    /// nuestro servidor SMTP, arruinando la reputacion del dominio remitente.
    /// </remarks>
    private const int MaximoPendientesPorEspacio = 20;

    private readonly OpcionesCorreo _correo = opcionesCorreo.Value;

    /// <inheritdoc />
    public async Task<InvitacionCreada> InvitarPropietarioAsync(
        SolicitudInvitarPropietario solicitud,
        Guid emitidaPorUsuarioId,
        CancellationToken cancelacion = default)
    {
        var correoNormalizado = NormalizarCorreo(solicitud.Correo);

        await VerificarQueNoTieneCuentaAsync(correoNormalizado);
        await VerificarQueNoHayInvitacionPendienteAsync(correoNormalizado, cancelacion);

        if (!Enum.TryParse<TipoEspacio>(solicitud.TipoEspacio, ignoreCase: true, out var tipoEspacio))
        {
            throw new ExcepcionDominio(
                "El tipo de espacio debe ser Personal, Pareja, Familia o Negocio.");
        }

        var (codigo, hash) = tokens.GenerarTokenRenovacion();

        var invitacion = new Invitacion
        {
            Tipo = TipoInvitacion.Propietario,
            Correo = correoNormalizado,
            HashCodigo = hash,
            NombreEspacioPropuesto = solicitud.NombreEspacioPropuesto.Trim(),
            TipoEspacioPropuesto = tipoEspacio,
            EmitidaPorUsuarioId = emitidaPorUsuarioId,
            FechaExpiracion = fechaHora.AhoraUtc.AddDays(DiasValidez),
            Estado = EstadoInvitacion.Pendiente,
        };

        contexto.Invitaciones.Add(invitacion);
        await contexto.SaveChangesAsync(cancelacion);

        var (asunto, cuerpo) = PlantillasCorreo.InvitacionPropietario(
            invitacion.NombreEspacioPropuesto!, codigo, _correo.UrlBase, DiasValidez);

        // Sin espacio: el hogar todavia no existe, asi que sale por el servidor de
        // plataforma.
        var enviado = await enviadorCorreo.EnviarAsync(
            correoNormalizado, asunto, cuerpo, espacioId: null, cancelacion);

        registro.LogInformation(
            "Invitación de propietario creada para {Correo}. Correo enviado: {Enviado}.",
            correoNormalizado, enviado);

        return new InvitacionCreada(
            invitacion.Id, correoNormalizado, codigo, invitacion.FechaExpiracion, enviado);
    }
}
