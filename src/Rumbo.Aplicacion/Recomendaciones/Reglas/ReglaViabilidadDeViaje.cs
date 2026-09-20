using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Calculadoras;
using Rumbo.Aplicacion.Comun;
using Rumbo.Dominio.Enums;

namespace Rumbo.Aplicacion.Recomendaciones.Reglas;

/// <summary>
/// Avisa cuando un viaje planificado no va camino de poder pagarse.
/// </summary>
/// <remarks>
/// <para>
/// Es la regla que convierte la pregunta «¿podemos permitirnos este viaje?» en un aviso que
/// llega solo, sin que nadie tenga que acordarse de consultarlo. Vale mucho mas saberlo
/// nueve meses antes que tres semanas antes.
/// </para>
/// <para>
/// <b>No cancela nada ni mueve dinero.</b> Dice cuanto falta y cuanto habria que apartar al
/// mes; lo que se haga con esa informacion es de las personas.
/// </para>
/// </remarks>
/// <param name="datos">Acceso a los datos.</param>
public class ReglaViabilidadDeViaje(IContextoRumbo datos) : IReglaRecomendacion
{
    /// <inheritdoc />
    /// <remarks>
    /// Va antes que el aporte a una meta genérica: un viaje tiene fecha, y una fecha que se
    /// acerca es más urgente que un objetivo sin plazo.
    /// </remarks>
    public int Prioridad => 15;

    /// <inheritdoc />
    public string Nombre => "Viabilidad de un viaje planificado";

    /// <inheritdoc />
    public async Task<IReadOnlyList<SugerenciaGenerada>> EvaluarAsync(
        ContextoRecomendacion contexto,
        CancellationToken cancelacion = default)
    {
        var viajes = await datos.Viajes
            .AsNoTracking()
            .Where(v => v.Estado == EstadoViaje.Planificado
                        && v.FechaInicio >= contexto.Hoy)
            .OrderBy(v => v.FechaInicio)
            .ToListAsync(cancelacion);

        var sugerencias = new List<SugerenciaGenerada>();

        foreach (var viaje in viajes)
        {
            var fondo = viaje.MetaId is { } metaId
                ? await datos.Metas
                    .AsNoTracking()
                    .Where(m => m.Id == metaId)
                    .Select(m => (decimal?)m.MontoActual)
                    .FirstOrDefaultAsync(cancelacion) ?? 0m
                : 0m;

            var proyeccion = CalculadoraViajes.Proyectar(
                viaje.PresupuestoTotal,
                fondo,
                viaje.FechaInicio,
                contexto.Hoy,
                contexto.FlujoCaja.DisponibleParaAhorro,
                contexto.FlujoCaja.ConfianzaBaja,
                viaje.Moneda);

            // Si se llega incluso en el escenario prudente no hay nada que avisar. Una
            // aplicacion que dice algo cada vez que se abre acaba ignorandose.
            if (proyeccion.Veredicto == "Si")
            {
                continue;
            }

            sugerencias.Add(new SugerenciaGenerada(
                TipoRecomendacion.ViabilidadDeViaje,
                proyeccion.Veredicto == "No"
                    ? $"«{viaje.Nombre}» no va camino de poder pagarse"
                    : $"«{viaje.Nombre}» va justo",
                ConstruirCuerpo(viaje.Nombre, proyeccion),
                new Dictionary<string, object?>
                {
                    ["presupuestoDelViaje"] = proyeccion.CostoTotal,
                    ["fondoActual"] = proyeccion.FondoActual,
                    ["faltante"] = proyeccion.Faltante,
                    ["mesesHastaLaSalida"] = proyeccion.MesesHastaLaSalida,
                    ["aporteMensualNecesario"] = proyeccion.AporteMensualNecesario,
                    ["excedenteMensualDelHogar"] = proyeccion.DisponibleMensual,
                    ["esfuerzoRequeridoPorcentaje"] = proyeccion.EsfuerzoRequerido,
                    ["veredicto"] = proyeccion.Veredicto,
                    ["fechaViableMasCercana"] = proyeccion.FechaViableMasCercana,
                    ["formula"] =
                        "(presupuesto - fondo) / meses hasta la salida, comparado con el "
                        + "excedente mensual del hogar en tres escenarios",
                },
                proyeccion.AporteMensualNecesario,
                proyeccion.ConfianzaBaja,
                viaje.MetaId,
                viaje.Id,
                DiasDeVigencia: 14));
        }

        return sugerencias;
    }

    /// <summary>Redacta el cuerpo de la sugerencia.</summary>
    /// <param name="nombre">Nombre del viaje.</param>
    /// <param name="proyeccion">La proyeccion ya calculada.</param>
    /// <returns>El texto, en espanol y sin tono imperativo.</returns>
    private static string ConstruirCuerpo(string nombre, ViabilidadViaje proyeccion)
    {
        var baseTexto =
            $"A «{nombre}» le faltan {proyeccion.Faltante:N2} y quedan "
            + $"{proyeccion.MesesHastaLaSalida} meses: serían "
            + $"{proyeccion.AporteMensualNecesario:N2} al mes. {proyeccion.Explicacion}";

        if (proyeccion.FechaViableMasCercana is { } fecha)
        {
            return baseTexto
                + $" Sin apretar el presupuesto, la fecha alcanzable más cercana sería "
                + $"alrededor de {fecha:MM/yyyy}.";
        }

        return baseTexto;
    }
}
