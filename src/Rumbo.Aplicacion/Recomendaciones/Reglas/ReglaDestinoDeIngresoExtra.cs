using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Modulos.Movimientos;
using Rumbo.Dominio.Enums;

namespace Rumbo.Aplicacion.Recomendaciones.Reglas;

/// <summary>
/// Cuando entra un ingreso claramente fuera de lo normal, propone a dónde destinarlo.
/// </summary>
/// <remarks>
/// <para>
/// Un bono, un doble sueldo o un trabajo extra suelen disolverse en el gasto corriente sin
/// que nadie decida nada. Esta regla lo detecta y pone la decisión encima de la mesa
/// mientras el dinero todavía está.
/// </para>
/// <para>
/// El destino se propone <b>por prioridad de las metas</b>, no repartido a partes iguales:
/// media docena de aportes simbólicos no acercan ninguna meta, y un solo aporte a la más
/// urgente sí.
/// </para>
/// <para>
/// <b>No mueve el dinero.</b> Propone, y la persona confirma el aporte por su operación.
/// </para>
/// </remarks>
/// <param name="datos">Acceso a los datos.</param>
public class ReglaDestinoDeIngresoExtra(IContextoRumbo datos) : IReglaRecomendacion
{
    /// <summary>Cuánto debe superar un ingreso a la media mensual para ser extraordinario.</summary>
    /// <remarks>
    /// Un 40 % por encima del ingreso medio del hogar. Un umbral más bajo marcaría como
    /// extraordinario cualquier mes con horas extra, y el aviso perdería sentido.
    /// </remarks>
    private const decimal FactorParaSerExtraordinario = 0.40m;

    /// <summary>Días hacia atrás en los que se busca el ingreso.</summary>
    /// <remarks>
    /// Más allá de un mes el dinero ya se gastó o ya se decidió, y sugerir qué hacer con él
    /// sería llegar tarde.
    /// </remarks>
    private const int DiasDeVentana = 30;

    /// <inheritdoc />
    /// <remarks>
    /// Es lo más urgente del motor: el dinero disponible ahora mismo es una oportunidad con
    /// fecha de caducidad.
    /// </remarks>
    public int Prioridad => 10;

    /// <inheritdoc />
    public string Nombre => "Destino de un ingreso extraordinario";

    /// <inheritdoc />
    public async Task<IReadOnlyList<SugerenciaGenerada>> EvaluarAsync(
        ContextoRecomendacion contexto,
        CancellationToken cancelacion = default)
    {
        // Sin historial no hay "normal" contra el que comparar, y cualquier ingreso
        // parecería extraordinario.
        if (contexto.FlujoCaja.IngresoPromedioMensual <= 0m)
        {
            return [];
        }

        var umbral = contexto.FlujoCaja.IngresoPromedioMensual * FactorParaSerExtraordinario;
        var desde = contexto.Hoy.AddDays(-DiasDeVentana);

        var extraordinario = await datos.Movimientos
            .AsNoTracking()
            .SoloIngresos()
            .EnRango(desde, contexto.Hoy)
            .Where(m => m.MontoEnMonedaBase >= umbral)
            .OrderByDescending(m => m.MontoEnMonedaBase)
            .Select(m => new { m.MontoEnMonedaBase, m.Descripcion, m.FechaMovimiento })
            .FirstOrDefaultAsync(cancelacion);

        if (extraordinario is null)
        {
            return [];
        }

        var meta = await datos.Metas
            .AsNoTracking()
            .Where(m => m.Estado == EstadoMeta.Activa && m.MontoActual < m.MontoObjetivo)
            .OrderByDescending(m => m.Prioridad)
            .ThenBy(m => m.FechaObjetivo)
            .Select(m => new { m.Id, m.Nombre, m.MontoObjetivo, m.MontoActual })
            .FirstOrDefaultAsync(cancelacion);

        if (meta is null)
        {
            // Sin ninguna meta activa no hay destino que proponer, y decir "ahorra" sin
            // decir para qué no ayuda a nadie.
            return [];
        }

        var faltaEnLaMeta = meta.MontoObjetivo - meta.MontoActual;
        var sugerido = Math.Min(extraordinario.MontoEnMonedaBase, faltaEnLaMeta);

        return
        [
            new SugerenciaGenerada(
                TipoRecomendacion.DestinoDeIngresoExtra,
                "Entró un ingreso fuera de lo habitual",
                $"El {extraordinario.FechaMovimiento:dd/MM} entraron "
                + $"{extraordinario.MontoEnMonedaBase:N2}, muy por encima de los "
                + $"{contexto.FlujoCaja.IngresoPromedioMensual:N2} que suele ingresar el "
                + $"hogar al mes. Destinar {sugerido:N2} a «{meta.Nombre}» la dejaría "
                + $"{(sugerido >= faltaEnLaMeta ? "completada" : "mucho más cerca")}. "
                + "Si prefieres otro destino, o ninguno, no pasa nada: esto es solo una "
                + "sugerencia.",
                new Dictionary<string, object?>
                {
                    ["montoDelIngreso"] = extraordinario.MontoEnMonedaBase,
                    ["descripcion"] = extraordinario.Descripcion,
                    ["fecha"] = extraordinario.FechaMovimiento,
                    ["ingresoPromedioMensual"] = contexto.FlujoCaja.IngresoPromedioMensual,
                    ["umbralParaSerExtraordinario"] = Math.Round(umbral, 2),
                    ["metaPropuesta"] = meta.Nombre,
                    ["faltanteDeLaMeta"] = faltaEnLaMeta,
                    ["formula"] =
                        "ingreso >= promedio mensual x 0,40 dentro de los ultimos 30 dias; "
                        + "el destino es la meta activa de mayor prioridad y fecha mas "
                        + "cercana, y el importe es el menor entre el ingreso y lo que le "
                        + "falta a esa meta",
                },
                sugerido,
                contexto.FlujoCaja.ConfianzaBaja,
                meta.Id,
                null,
                DiasDeVigencia: 15),
        ];
    }
}
