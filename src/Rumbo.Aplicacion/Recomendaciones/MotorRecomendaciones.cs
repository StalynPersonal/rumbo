using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Rumbo.Aplicacion.Calculadoras;
using Rumbo.Aplicacion.Comun;
using Rumbo.Dominio.Entidades.Soporte;
using Rumbo.Dominio.Enums;

namespace Rumbo.Aplicacion.Recomendaciones;

/// <summary>
/// Ejecuta las reglas y persiste las sugerencias que producen.
/// </summary>
/// <remarks>
/// <para>
/// El motor es <b>deterministico</b>: reglas y aritmetica, sin inteligencia artificial. Dos
/// ejecuciones con los mismos datos dan el mismo resultado, y cada recomendacion guarda los
/// insumos con los que se calculo para poder explicarse.
/// </para>
/// <para>
/// <b>Ninguna recomendacion mueve dinero.</b> Nacen en estado Pendiente y solo se convierten
/// en un movimiento cuando la persona lo confirma. Es el principio innegociable del
/// proyecto: que el sistema transfiriera por su cuenta, aunque fuera con buena intencion,
/// destruiria la confianza en la aplicacion.
/// </para>
/// <para>
/// <b>Las pendientes se sustituyen, no se acumulan.</b> Si se guardaran todas, a la semana
/// habria veinte sugerencias contradictorias calculadas con datos distintos y ninguna seria
/// de fiar.
/// </para>
/// </remarks>
/// <param name="contexto">Acceso a los datos.</param>
/// <param name="reglas">Reglas registradas.</param>
/// <param name="analizador">Analizador del flujo de caja.</param>
/// <param name="contextoEspacio">Espacio activo.</param>
/// <param name="fechaHora">Proveedor de fecha y hora.</param>
/// <param name="registro">Registro de eventos.</param>
public class MotorRecomendaciones(
    IContextoRumbo contexto,
    IEnumerable<IReglaRecomendacion> reglas,
    AnalizadorFlujoCaja analizador,
    IContextoEspacio contextoEspacio,
    IProveedorFechaHora fechaHora,
    ILogger<MotorRecomendaciones> registro)
{
    /// <summary>
    /// Recalcula las recomendaciones del espacio activo.
    /// </summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Cuantas sugerencias quedaron vigentes.</returns>
    public async Task<int> RecalcularAsync(CancellationToken cancelacion = default)
    {
        var espacioId = contextoEspacio.ObtenerEspacioObligatorio();

        var espacio = await contexto.Espacios
            .AsNoTracking()
            .Where(e => e.Id == espacioId)
            .Select(e => new { e.MonedaBase, e.ZonaHoraria })
            .FirstOrDefaultAsync(cancelacion);

        if (espacio is null)
        {
            return 0;
        }

        var configuracion = await contexto.ConfiguracionesEspacio
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.EspacioId == espacioId, cancelacion);

        // Respetar la preferencia del hogar: quien no quiere sugerencias no debe recibirlas.
        if (configuracion is not null && !configuracion.RecomendacionesActivas)
        {
            return 0;
        }

        var hoy = fechaHora.HoyEn(espacio.ZonaHoraria);

        var flujo = await analizador.AnalizarAsync(
            hoy,
            configuracion?.MesesHistorialParaAnalisis ?? 6,
            espacio.MonedaBase,
            cancelacion);

        var contextoRegla = new ContextoRecomendacion(
            espacioId, hoy, espacio.MonedaBase, flujo);

        var generadas = new List<SugerenciaGenerada>();

        foreach (var regla in reglas.OrderBy(r => r.Prioridad))
        {
            try
            {
                generadas.AddRange(await regla.EvaluarAsync(contextoRegla, cancelacion));
            }
            catch (Exception excepcion) when (excepcion is not OperationCanceledException)
            {
                // Una regla que falle no debe tumbar al resto: es preferible mostrar cuatro
                // recomendaciones de cinco que ninguna.
                registro.LogError(excepcion,
                    "La regla «{Regla}» falló al evaluar el espacio {EspacioId}. "
                    + "Se omite y se continúa con las demás.",
                    regla.Nombre, espacioId);
            }
        }

        await ReemplazarPendientesAsync(generadas, hoy, cancelacion);

        registro.LogInformation(
            "Se recalcularon las recomendaciones del espacio {EspacioId}: {Cantidad} vigentes.",
            espacioId, generadas.Count);

        return generadas.Count;
    }

    /// <summary>
    /// Sustituye las recomendaciones pendientes por las recien generadas.
    /// </summary>
    /// <param name="generadas">Sugerencias nuevas.</param>
    /// <param name="hoy">Fecha actual.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando se guardan.</returns>
    /// <remarks>
    /// Solo se tocan las PENDIENTES. Las que el usuario ya acepto o descarto se conservan:
    /// son su historial de decisiones, y volver a proponerle algo que ya rechazo seria
    /// molesto.
    /// </remarks>
    private async Task ReemplazarPendientesAsync(
        List<SugerenciaGenerada> generadas,
        DateOnly hoy,
        CancellationToken cancelacion)
    {
        var pendientes = await contexto.Recomendaciones
            .Where(r => r.Estado == EstadoRecomendacion.Pendiente)
            .ToListAsync(cancelacion);

        contexto.Recomendaciones.RemoveRange(pendientes);

        var ahora = fechaHora.AhoraUtc;

        foreach (var sugerencia in generadas)
        {
            contexto.Recomendaciones.Add(new Recomendacion
            {
                Tipo = sugerencia.Tipo,
                Titulo = sugerencia.Titulo,
                Cuerpo = sugerencia.Cuerpo,
                Insumos = System.Text.Json.JsonSerializer.Serialize(sugerencia.Insumos),
                MontoSugerido = sugerencia.MontoSugerido,
                ConfianzaBaja = sugerencia.ConfianzaBaja,
                MetaId = sugerencia.MetaId,
                ViajeId = sugerencia.ViajeId,
                FechaGeneracion = ahora,
                FechaExpiracion = ahora.AddDays(sugerencia.DiasDeVigencia),
                Estado = EstadoRecomendacion.Pendiente,
            });
        }

        await contexto.SaveChangesAsync(cancelacion);
    }
}
