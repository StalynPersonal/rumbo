using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Rumbo.Contratos.Invitaciones;
using Rumbo.Dominio.Entidades.Identidad;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;
using Rumbo.Infraestructura.Correo.Plantillas;

namespace Rumbo.Infraestructura.Identidad;

/// <summary>
/// Parte del servicio de invitaciones dedicada a los miembros de un espacio.
/// </summary>
public partial class ServicioInvitaciones
{
    /// <inheritdoc />
    public async Task<InvitacionCreada> InvitarMiembroAsync(
        SolicitudInvitarMiembro solicitud,
        Guid espacioId,
        Guid emitidaPorUsuarioId,
        CancellationToken cancelacion = default)
    {
        var correoNormalizado = NormalizarCorreo(solicitud.Correo);

        if (!Enum.TryParse<RolEspacio>(solicitud.Rol, ignoreCase: true, out var rol))
        {
            throw new ExcepcionDominio("El rol debe ser Administrador o Miembro.");
        }

        // Nadie puede crear otro propietario por invitacion. El propietario es quien creo el
        // espacio, y traspasar esa condicion debe ser una accion aparte y deliberada, no un
        // efecto secundario de invitar a alguien.
        if (rol == RolEspacio.Propietario)
        {
            throw new ExcepcionDominio(
                "No se puede invitar a alguien como Propietario. Invítalo como Administrador.");
        }

        await VerificarQueNoHayInvitacionPendienteAsync(correoNormalizado, cancelacion);
        await VerificarLimiteDePendientesAsync(espacioId, cancelacion);

        var usuarioExistente = await usuarios.FindByEmailAsync(correoNormalizado);

        if (usuarioExistente is not null)
        {
            var yaEsMiembro = await contexto.MembresiasEspacio.AnyAsync(
                m => m.UsuarioId == usuarioExistente.Id && m.EspacioId == espacioId, cancelacion);

            if (yaEsMiembro)
            {
                throw new ExcepcionDominio("Esa persona ya forma parte de este espacio.");
            }
        }

        var nombreEspacio = await contexto.Espacios
            .Where(e => e.Id == espacioId)
            .Select(e => e.Nombre)
            .FirstOrDefaultAsync(cancelacion)
            ?? throw new ExcepcionNoEncontrado("el espacio");

        var quienInvita = await usuarios.FindByIdAsync(emitidaPorUsuarioId.ToString());

        var (codigo, hash) = tokens.GenerarTokenRenovacion();

        var invitacion = new Invitacion
        {
            Tipo = TipoInvitacion.Miembro,
            Correo = correoNormalizado,
            HashCodigo = hash,
            EspacioId = espacioId,
            RolAsignado = rol,
            EmitidaPorUsuarioId = emitidaPorUsuarioId,
            FechaExpiracion = fechaHora.AhoraUtc.AddDays(DiasValidez),
            Estado = EstadoInvitacion.Pendiente,
        };

        contexto.Invitaciones.Add(invitacion);
        await contexto.SaveChangesAsync(cancelacion);

        var (asunto, cuerpo) = PlantillasCorreo.InvitacionMiembro(
            nombreEspacio,
            quienInvita?.NombreCompleto ?? "Alguien",
            codigo,
            _correo.UrlBase,
            DiasValidez);

        // Se indica el espacio para que, si el propietario configuro su propio servidor,
        // la invitacion llegue desde SU direccion y no desde una generica.
        var enviado = await enviadorCorreo.EnviarAsync(
            correoNormalizado, asunto, cuerpo, espacioId, cancelacion);

        registro.LogInformation(
            "Invitación de miembro creada para el espacio {EspacioId}. Correo enviado: {Enviado}.",
            espacioId, enviado);

        return new InvitacionCreada(
            invitacion.Id, correoNormalizado, codigo, invitacion.FechaExpiracion, enviado);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InvitacionResumen>> ListarDelEspacioAsync(
        Guid espacioId,
        CancellationToken cancelacion = default) =>
        await contexto.Invitaciones
            .AsNoTracking()
            .Where(i => i.EspacioId == espacioId)
            .OrderByDescending(i => i.FechaCreacion)
            .Select(i => new InvitacionResumen(
                i.Id, i.Correo, i.Tipo.ToString(), i.Estado.ToString(),
                i.FechaExpiracion, i.FechaCreacion))
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<InvitacionResumen>> ListarDePlataformaAsync(
        CancellationToken cancelacion = default) =>
        await contexto.Invitaciones
            .AsNoTracking()
            .Where(i => i.Tipo == TipoInvitacion.Propietario)
            .OrderByDescending(i => i.FechaCreacion)
            .Select(i => new InvitacionResumen(
                i.Id, i.Correo, i.Tipo.ToString(), i.Estado.ToString(),
                i.FechaExpiracion, i.FechaCreacion))
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task RevocarAsync(
        Guid invitacionId,
        Guid? espacioId,
        CancellationToken cancelacion = default)
    {
        var consulta = contexto.Invitaciones.Where(i => i.Id == invitacionId);

        // Quien anula desde un espacio solo puede anular las de SU espacio. Sin esta
        // condicion, conocer el identificador de una invitacion ajena bastaria para anularla.
        if (espacioId.HasValue)
        {
            consulta = consulta.Where(i => i.EspacioId == espacioId.Value);
        }

        var invitacion = await consulta.FirstOrDefaultAsync(cancelacion)
            ?? throw new ExcepcionNoEncontrado("la invitación");

        if (invitacion.Estado != EstadoInvitacion.Pendiente)
        {
            throw new ExcepcionDominio("Esa invitación ya no está pendiente.");
        }

        invitacion.Estado = EstadoInvitacion.Revocada;
        await contexto.SaveChangesAsync(cancelacion);

        registro.LogInformation("Invitación {InvitacionId} anulada.", invitacionId);
    }

    /// <summary>Deja el correo en minusculas y sin espacios sobrantes.</summary>
    /// <param name="correo">Correo tal como llego.</param>
    /// <returns>El correo normalizado.</returns>
    private static string NormalizarCorreo(string correo) => correo.Trim().ToLowerInvariant();

    /// <summary>Comprueba que no exista ya una cuenta con ese correo.</summary>
    /// <param name="correo">Correo normalizado.</param>
    /// <returns>Tarea que finaliza cuando termina la comprobacion.</returns>
    private async Task VerificarQueNoTieneCuentaAsync(string correo)
    {
        if (await usuarios.FindByEmailAsync(correo) is not null)
        {
            throw new ExcepcionDominio(
                "Esa persona ya tiene una cuenta en Rumbo. Invítala a un espacio existente.");
        }
    }

    /// <summary>Impide acumular invitaciones pendientes para el mismo correo.</summary>
    /// <param name="correo">Correo normalizado.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando termina la comprobacion.</returns>
    private async Task VerificarQueNoHayInvitacionPendienteAsync(
        string correo,
        CancellationToken cancelacion)
    {
        var hayPendiente = await contexto.Invitaciones.AnyAsync(
            i => i.Correo == correo
                 && i.Estado == EstadoInvitacion.Pendiente
                 && i.FechaExpiracion > fechaHora.AhoraUtc,
            cancelacion);

        if (hayPendiente)
        {
            throw new ExcepcionDominio(
                "Ya hay una invitación pendiente para ese correo. Anúlala antes de crear otra.");
        }
    }

    /// <summary>Aplica los limites de invitaciones por espacio.</summary>
    /// <param name="espacioId">Espacio que invita.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando termina la comprobacion.</returns>
    /// <remarks>
    /// Son <b>dos</b> limites y hacen falta los dos. El de pendientes acota cuantas puertas
    /// quedan abiertas a la vez; el de la ultima hora acota cuantos correos salen de nuestro
    /// servidor SMTP. Sin el segundo, bastaria con anular las veinte pendientes y volver a
    /// crearlas en bucle para mandar correo sin tope y arruinar la reputacion del dominio.
    /// </remarks>
    private async Task VerificarLimiteDePendientesAsync(Guid espacioId, CancellationToken cancelacion)
    {
        var pendientes = await contexto.Invitaciones.CountAsync(
            i => i.EspacioId == espacioId && i.Estado == EstadoInvitacion.Pendiente, cancelacion);

        if (pendientes >= MaximoPendientesPorEspacio)
        {
            throw new ExcepcionDominio(
                $"Este espacio ya tiene {MaximoPendientesPorEspacio} invitaciones pendientes.");
        }

        var desde = fechaHora.AhoraUtc.AddHours(-1);

        // Se cuentan TODAS las creadas en la ultima hora, sea cual sea su estado: anular una
        // invitacion no deshace el correo que ya salio.
        var recientes = await contexto.Invitaciones.CountAsync(
            i => i.EspacioId == espacioId && i.FechaCreacion >= desde, cancelacion);

        if (recientes >= MaximoPorHoraPorEspacio)
        {
            throw new ExcepcionDominio(
                $"Este espacio ya envió {MaximoPorHoraPorEspacio} invitaciones en la última "
                + "hora. Espera un poco antes de enviar más.");
        }
    }
}
