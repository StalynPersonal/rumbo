using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Notificaciones;
using Rumbo.Dominio.Entidades.Soporte;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Notificaciones;

/// <summary>
/// Avisos del hogar: pagos que vencen, presupuestos al limite y metas alcanzadas.
/// </summary>
/// <remarks>
/// <para>
/// Los avisos se <b>persisten</b> aunque todavia no haya notificaciones push. Cuando se
/// enchufe el transporte, el historial ya estara ahi y no habra que inventarlo.
/// </para>
/// <para>
/// Un aviso no se duplica: antes de crearlo se comprueba que no exista ya otro del mismo tipo
/// sobre lo mismo, leido o no. Recibir cinco veces «el recibo de la luz vence pronto» hace que
/// se dejen de leer todos.
/// </para>
/// </remarks>
/// <param name="contexto">Acceso a los datos.</param>
/// <param name="contextoEspacio">Espacio activo.</param>
/// <param name="fechaHora">Proveedor de fecha y hora.</param>
public class ServicioNotificaciones(
    IContextoRumbo contexto,
    IContextoEspacio contextoEspacio,
    IProveedorFechaHora fechaHora) : IServicioNotificaciones
{
    /// <summary>Dias de antelacion con que se avisa de un pago.</summary>
    private const int DiasDeAvisoPorDefecto = 5;

    /// <inheritdoc />
    public async Task<IReadOnlyList<NotificacionDto>> ListarAsync(
        bool soloSinLeer = false,
        CancellationToken cancelacion = default)
    {
        var consulta = contexto.Notificaciones.AsNoTracking();

        if (soloSinLeer)
        {
            consulta = consulta.Where(n => n.FechaLectura == null
                                           && n.Estado != EstadoNotificacion.Descartada);
        }

        return await consulta
            .OrderByDescending(n => n.ProgramadaPara)
            .Select(n => new NotificacionDto(
                n.Id, n.Tipo.ToString(), n.Titulo, n.Cuerpo, n.Datos,
                n.ProgramadaPara, n.FechaLectura, n.Estado.ToString()))
            .ToListAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<NotificacionDto> MarcarComoLeidaAsync(
        Guid notificacionId,
        CancellationToken cancelacion = default)
    {
        var notificacion = await contexto.Notificaciones
            .FirstOrDefaultAsync(n => n.Id == notificacionId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la notificación");

        if (notificacion.FechaLectura is null)
        {
            notificacion.FechaLectura = fechaHora.AhoraUtc;
            notificacion.Estado = EstadoNotificacion.Leida;

            await contexto.SaveChangesAsync(cancelacion);
        }

        return new NotificacionDto(
            notificacion.Id, notificacion.Tipo.ToString(), notificacion.Titulo,
            notificacion.Cuerpo, notificacion.Datos, notificacion.ProgramadaPara,
            notificacion.FechaLectura, notificacion.Estado.ToString());
    }

    /// <inheritdoc />
    public async Task<int> MarcarTodasComoLeidasAsync(CancellationToken cancelacion = default)
    {
        var pendientes = await contexto.Notificaciones
            .Where(n => n.FechaLectura == null
                        && n.Estado != EstadoNotificacion.Descartada)
            .ToListAsync(cancelacion);

        var ahora = fechaHora.AhoraUtc;

        foreach (var notificacion in pendientes)
        {
            notificacion.FechaLectura = ahora;
            notificacion.Estado = EstadoNotificacion.Leida;
        }

        await contexto.SaveChangesAsync(cancelacion);

        return pendientes.Count;
    }

    /// <inheritdoc />
    public async Task DescartarAsync(
        Guid notificacionId,
        CancellationToken cancelacion = default)
    {
        var notificacion = await contexto.Notificaciones
            .FirstOrDefaultAsync(n => n.Id == notificacionId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la notificación");

        // Se descarta, no se borra: saber que se avisó y que la persona lo apartó es
        // información, y borrarla haría imposible medir si los avisos sirven de algo.
        notificacion.Estado = EstadoNotificacion.Descartada;
        notificacion.FechaLectura ??= fechaHora.AhoraUtc;

        await contexto.SaveChangesAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<ResultadoGeneracionAvisos> GenerarAsync(
        CancellationToken cancelacion = default)
    {
        var espacioId = contextoEspacio.ObtenerEspacioObligatorio();

        var zona = await contexto.Espacios
            .AsNoTracking()
            .Where(e => e.Id == espacioId)
            .Select(e => e.ZonaHoraria)
            .FirstOrDefaultAsync(cancelacion);

        var hoy = fechaHora.HoyEn(zona ?? "America/Santo_Domingo");
        var ahora = fechaHora.AhoraUtc;

        var nuevas = new List<Notificacion>();

        nuevas.AddRange(await AvisosDePagosProximosAsync(hoy, ahora, cancelacion));
        nuevas.AddRange(await AvisosDeMetasAlcanzadasAsync(ahora, cancelacion));

        // Se comparan contra TODAS las existentes, leidas y descartadas incluidas. Si solo
        // se miraran las pendientes, un aviso ya leido volveria a aparecer en el siguiente
        // recalculo, y algo que ya se atendio no deberia reclamar atencion otra vez.
        //
        // Los avisos que si deben repetirse llevan la fecha dentro de sus datos, asi que el
        // del mes que viene es un aviso distinto y se genera sin problema.
        var yaAvisado = await contexto.Notificaciones
            .AsNoTracking()
            .Select(n => new { n.Tipo, n.Datos })
            .ToListAsync(cancelacion);

        var aGuardar = nuevas
            .Where(n => !yaAvisado.Any(y => y.Tipo == n.Tipo && y.Datos == n.Datos))
            .ToList();

        if (aGuardar.Count > 0)
        {
            contexto.Notificaciones.AddRange(aGuardar);
            await contexto.SaveChangesAsync(cancelacion);
        }

        var sinLeer = await contexto.Notificaciones
            .CountAsync(n => n.FechaLectura == null
                             && n.Estado != EstadoNotificacion.Descartada, cancelacion);

        return new ResultadoGeneracionAvisos(aGuardar.Count, sinLeer);
    }

    /// <summary>Avisos de recibos y cuotas que vencen pronto.</summary>
    /// <param name="hoy">Fecha actual.</param>
    /// <param name="ahora">Instante actual.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Los avisos propuestos.</returns>
    private async Task<List<Notificacion>> AvisosDePagosProximosAsync(
        DateOnly hoy,
        DateTimeOffset ahora,
        CancellationToken cancelacion)
    {
        var recurrentes = await contexto.GastosRecurrentes
            .AsNoTracking()
            .Where(g => g.Estado == EstadoRecurrencia.Activa && g.ProximaFechaPago >= hoy)
            .Select(g => new
            {
                g.Id,
                g.Nombre,
                g.MontoEstimado,
                g.ProximaFechaPago,
                g.DiasAvisoPrevio,
            })
            .ToListAsync(cancelacion);

        return
        [
            .. recurrentes
                .Where(g => g.ProximaFechaPago.DayNumber - hoy.DayNumber
                            <= (g.DiasAvisoPrevio > 0 ? g.DiasAvisoPrevio : DiasDeAvisoPorDefecto))
                .Select(g => new Notificacion
                {
                    Tipo = TipoNotificacion.ProximoPago,
                    Titulo = $"«{g.Nombre}» vence pronto",
                    Cuerpo = $"El {g.ProximaFechaPago:dd/MM} toca pagar «{g.Nombre}», "
                             + $"unos {g.MontoEstimado:N2}.",
                    Datos = $"{{\"gastoRecurrenteId\":\"{g.Id}\","
                            + $"\"fecha\":\"{g.ProximaFechaPago:yyyy-MM-dd}\"}}",
                    ProgramadaPara = ahora,
                    Estado = EstadoNotificacion.Pendiente,
                }),
        ];
    }

    /// <summary>Avisos de metas que ya llegaron a su objetivo.</summary>
    /// <param name="ahora">Instante actual.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Los avisos propuestos.</returns>
    /// <remarks>
    /// <para>
    /// Llegar a una meta de ahorro cuesta meses y suele pasar sin que nadie se entere. Es el
    /// unico aviso del sistema que da una buena noticia.
    /// </para>
    /// <para>
    /// Se buscan las que ya estan en estado <c>Alcanzada</c>: el propio aporte las cierra en
    /// cuanto el acumulado llega al objetivo, asi que buscar metas activas con el objetivo
    /// cubierto no encontraria nunca ninguna.
    /// </para>
    /// </remarks>
    private async Task<List<Notificacion>> AvisosDeMetasAlcanzadasAsync(
        DateTimeOffset ahora,
        CancellationToken cancelacion)
    {
        var alcanzadas = await contexto.Metas
            .AsNoTracking()
            .Where(m => m.Estado == EstadoMeta.Alcanzada)
            .Select(m => new { m.Id, m.Nombre, m.MontoObjetivo })
            .ToListAsync(cancelacion);

        return
        [
            .. alcanzadas.Select(m => new Notificacion
            {
                Tipo = TipoNotificacion.MetaAlcanzada,
                Titulo = $"¡«{m.Nombre}» está completa!",
                Cuerpo = $"Ya reuniste los {m.MontoObjetivo:N2} de «{m.Nombre}». "
                         + "Puedes marcarla como alcanzada cuando quieras.",
                Datos = $"{{\"metaId\":\"{m.Id}\"}}",
                ProgramadaPara = ahora,
                Estado = EstadoNotificacion.Pendiente,
            }),
        ];
    }
}
