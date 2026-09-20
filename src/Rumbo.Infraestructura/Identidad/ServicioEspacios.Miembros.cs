using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Rumbo.Contratos.Espacios;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Infraestructura.Identidad;

/// <summary>
/// Parte del servicio de espacios dedicada a los miembros.
/// </summary>
/// <remarks>
/// Las reglas de esta clase existen para evitar tres situaciones sin salida: que un hogar se
/// quede sin propietario, que alguien se degrade a si mismo por error y quede bloqueado, y
/// que un administrador pueda echar a quien creo el espacio.
/// </remarks>
public partial class ServicioEspacios
{
    /// <inheritdoc />
    public async Task<MiembroEspacio> CambiarRolAsync(
        Guid espacioId,
        Guid usuarioObjetivoId,
        SolicitudCambiarRol solicitud,
        Guid usuarioQueEjecutaId,
        CancellationToken cancelacion = default)
    {
        if (!Enum.TryParse<RolEspacio>(solicitud.Rol, ignoreCase: true, out var rolNuevo))
        {
            throw new ExcepcionDominio("El rol debe ser Propietario, Administrador o Miembro.");
        }

        var objetivo = await BuscarMembresiaAsync(espacioId, usuarioObjetivoId, cancelacion);
        var ejecutor = await BuscarMembresiaAsync(espacioId, usuarioQueEjecutaId, cancelacion);

        // Nadie cambia su propio rol. Sin esta regla, el unico propietario de un hogar podria
        // degradarse a Miembro y quedarse sin nadie que pueda devolverle el control.
        if (usuarioObjetivoId == usuarioQueEjecutaId)
        {
            throw new ExcepcionDominio(
                "No puedes cambiar tu propio rol. Pídeselo a otra persona del espacio.");
        }

        // Solo un propietario puede tocar a otro propietario, o crear uno nuevo. Un
        // administrador que pudiera degradar al dueño se apoderaria del hogar.
        if ((objetivo.Rol == RolEspacio.Propietario || rolNuevo == RolEspacio.Propietario)
            && ejecutor.Rol != RolEspacio.Propietario)
        {
            throw new ExcepcionDominio(
                "Solo un propietario puede cambiar el rol de un propietario.");
        }

        if (objetivo.Rol == RolEspacio.Propietario && rolNuevo != RolEspacio.Propietario)
        {
            await VerificarQueQuedaAlgunPropietarioAsync(espacioId, usuarioObjetivoId, cancelacion);
        }

        objetivo.Rol = rolNuevo;
        await contexto.SaveChangesAsync(cancelacion);

        // Sin esto, el cambio tardaria hasta 30 segundos en notarse, lo que dura la caché
        // de comprobaciones de membresía.
        cacheMembresias.Invalidar(usuarioObjetivoId, espacioId);

        registro.LogInformation(
            "El usuario {Ejecutor} cambió el rol de {Objetivo} a {Rol} en el espacio {EspacioId}.",
            usuarioQueEjecutaId, usuarioObjetivoId, rolNuevo, espacioId);

        return await ObtenerMiembroAsync(espacioId, usuarioObjetivoId, cancelacion);
    }

    /// <inheritdoc />
    public async Task<MiembroEspacio> CambiarEstadoMiembroAsync(
        Guid espacioId,
        Guid usuarioObjetivoId,
        SolicitudCambiarEstadoMiembro solicitud,
        Guid usuarioQueEjecutaId,
        CancellationToken cancelacion = default)
    {
        if (!Enum.TryParse<EstadoMembresia>(solicitud.Estado, ignoreCase: true, out var estadoNuevo))
        {
            throw new ExcepcionDominio("El estado debe ser Activa, Suspendida o Revocada.");
        }

        var objetivo = await BuscarMembresiaAsync(espacioId, usuarioObjetivoId, cancelacion);
        var ejecutor = await BuscarMembresiaAsync(espacioId, usuarioQueEjecutaId, cancelacion);

        // Nadie se expulsa a si mismo. Salir de un espacio es otra operacion distinta, y
        // permitirlo aqui dejaria al hogar sin dueño de un clic.
        if (usuarioObjetivoId == usuarioQueEjecutaId)
        {
            throw new ExcepcionDominio("No puedes cambiar tu propio estado en el espacio.");
        }

        if (objetivo.Rol == RolEspacio.Propietario && ejecutor.Rol != RolEspacio.Propietario)
        {
            throw new ExcepcionDominio(
                "Solo un propietario puede suspender o expulsar a otro propietario.");
        }

        if (objetivo.Rol == RolEspacio.Propietario && estadoNuevo != EstadoMembresia.Activa)
        {
            await VerificarQueQuedaAlgunPropietarioAsync(espacioId, usuarioObjetivoId, cancelacion);
        }

        objetivo.Estado = estadoNuevo;

        // La membresia NO se borra, solo cambia de estado. Los movimientos que esa persona
        // registro siguen referenciandola; borrarla dejaria el historial sin autor y
        // rompería la auditoría.
        await contexto.SaveChangesAsync(cancelacion);

        // Retirar un acceso es justo la operación que no debe hacerse esperar: se olvida lo
        // cacheado para que la siguiente petición de esa persona ya encuentre la puerta
        // cerrada.
        cacheMembresias.Invalidar(usuarioObjetivoId, espacioId);

        registro.LogInformation(
            "El usuario {Ejecutor} dejó a {Objetivo} en estado {Estado} en el espacio {EspacioId}.",
            usuarioQueEjecutaId, usuarioObjetivoId, estadoNuevo, espacioId);

        return await ObtenerMiembroAsync(espacioId, usuarioObjetivoId, cancelacion);
    }

    /// <summary>Busca una membresia del espacio o falla con 404.</summary>
    private async Task<Dominio.Entidades.Identidad.MembresiaEspacio> BuscarMembresiaAsync(
        Guid espacioId,
        Guid usuarioId,
        CancellationToken cancelacion) =>
        await contexto.MembresiasEspacio
            .FirstOrDefaultAsync(m => m.EspacioId == espacioId && m.UsuarioId == usuarioId, cancelacion)
        ?? throw new ExcepcionNoEncontrado("esa persona en este espacio");

    /// <summary>Impide dejar el espacio sin ningun propietario activo.</summary>
    /// <param name="espacioId">Espacio afectado.</param>
    /// <param name="usuarioQueSeVaId">Propietario que perderia el rol o el acceso.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando termina la comprobacion.</returns>
    /// <remarks>
    /// Un espacio sin propietario no tendria a nadie que pueda invitar, gestionar miembros ni
    /// eliminarlo: quedaria bloqueado para siempre sin intervencion manual en la base de datos.
    /// </remarks>
    private async Task VerificarQueQuedaAlgunPropietarioAsync(
        Guid espacioId,
        Guid usuarioQueSeVaId,
        CancellationToken cancelacion)
    {
        var otrosPropietarios = await contexto.MembresiasEspacio.CountAsync(
            m => m.EspacioId == espacioId
                 && m.UsuarioId != usuarioQueSeVaId
                 && m.Rol == RolEspacio.Propietario
                 && m.Estado == EstadoMembresia.Activa,
            cancelacion);

        if (otrosPropietarios == 0)
        {
            throw new ExcepcionDominio(
                "Este espacio se quedaría sin propietario. Nombra antes a otra persona "
                + "propietaria.");
        }
    }

    /// <summary>Devuelve un miembro concreto ya proyectado al DTO.</summary>
    private async Task<MiembroEspacio> ObtenerMiembroAsync(
        Guid espacioId,
        Guid usuarioId,
        CancellationToken cancelacion) =>
        (await ConsultarMiembrosAsync(espacioId, usuarioId, cancelacion)).FirstOrDefault()
        ?? throw new ExcepcionNoEncontrado("esa persona en este espacio");
}
