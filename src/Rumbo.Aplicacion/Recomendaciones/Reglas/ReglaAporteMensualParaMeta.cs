using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Calculadoras;
using Rumbo.Aplicacion.Comun;
using Rumbo.Dominio.Enums;

namespace Rumbo.Aplicacion.Recomendaciones.Reglas;

/// <summary>
/// Sugiere cuanto aportar cada mes a cada meta con fecha, y lo contrasta con lo que el hogar
/// puede permitirse de verdad.
/// </summary>
/// <remarks>
/// <para>
/// Es la regla principal del motor y la que produce el mensaje de referencia del proyecto:
/// «Para alcanzar tu meta de RD$180,000 en diciembre de 2027 necesitas ahorrar
/// aproximadamente RD$10,000 mensuales».
/// </para>
/// <para>
/// <b>No se limita a dividir.</b> Compara el aporte necesario con el excedente real del
/// hogar y lo dice cuando no cuadra. Una recomendacion que ignora si el dinero existe no es
/// una recomendacion, es una division.
/// </para>
/// </remarks>
/// <param name="contexto">Acceso a los datos.</param>
public class ReglaAporteMensualParaMeta(IContextoRumbo contexto) : IReglaRecomendacion
{
    /// <inheritdoc />
    public int Prioridad => 10;

    /// <inheritdoc />
    public string Nombre => "Aporte mensual para meta";

    /// <inheritdoc />
    public async Task<IReadOnlyList<SugerenciaGenerada>> EvaluarAsync(
        ContextoRecomendacion contexto_,
        CancellationToken cancelacion = default)
    {
        var metas = await contexto.Metas
            .AsNoTracking()
            .Where(m => m.Estado == EstadoMeta.Activa && m.FechaObjetivo != null)
            .OrderBy(m => m.FechaObjetivo)
            .ToListAsync(cancelacion);

        var sugerencias = new List<SugerenciaGenerada>();

        foreach (var meta in metas)
        {
            var proyeccion = CalculadoraMetas.Proyectar(
                meta.MontoObjetivo, meta.MontoActual, meta.FechaObjetivo, contexto_.Hoy);

            if (proyeccion.EstaAlcanzada || proyeccion.AporteMensualNecesario is not { } aporte)
            {
                continue;
            }

            var excedente = contexto_.FlujoCaja.DisponibleParaAhorro;

            var cuerpo = ConstruirCuerpo(meta.Nombre, proyeccion, excedente, contexto_);

            sugerencias.Add(new SugerenciaGenerada(
                TipoRecomendacion.AporteMensualParaMeta,
                $"Aporte sugerido para «{meta.Nombre}»",
                cuerpo,
                new Dictionary<string, object?>
                {
                    ["metaNombre"] = meta.Nombre,
                    ["montoObjetivo"] = meta.MontoObjetivo,
                    ["montoActual"] = meta.MontoActual,
                    ["montoFaltante"] = proyeccion.MontoFaltante,
                    ["fechaObjetivo"] = meta.FechaObjetivo,
                    ["mesesRestantes"] = proyeccion.MesesRestantes,
                    ["aporteMensualNecesario"] = aporte,
                    ["aporteSemanalNecesario"] = proyeccion.AporteSemanalNecesario,
                    ["excedentePromedioMensual"] = contexto_.FlujoCaja.ExcedentePromedioMensual,
                    ["compromisoMensualRecurrente"] = contexto_.FlujoCaja.CompromisoMensualRecurrente,
                    ["disponibleParaAhorro"] = excedente,
                    ["mesesAnalizados"] = contexto_.FlujoCaja.MesesAnalizados,
                    ["formula"] = "(montoObjetivo - montoActual) / mesesRestantes",
                },
                aporte,
                contexto_.FlujoCaja.ConfianzaBaja,
                meta.Id,
                null,

                // Una semana: el aporte cambia en cuanto se registra un movimiento nuevo,
                // asi que mantenerla mas tiempo la volveria inexacta.
                DiasDeVigencia: 7));
        }

        return sugerencias;
    }

    /// <summary>
    /// Redacta la explicacion, contrastando el aporte con lo que el hogar puede permitirse.
    /// </summary>
    /// <param name="nombreMeta">Nombre de la meta.</param>
    /// <param name="proyeccion">Cifras calculadas.</param>
    /// <param name="disponible">Lo que sobra al mes tras los compromisos.</param>
    /// <param name="contexto_">Contexto de la evaluacion.</param>
    /// <returns>El texto de la recomendacion.</returns>
    /// <remarks>
    /// El tono es deliberadamente descriptivo. Se dice lo que los numeros muestran y se deja
    /// la decision a la persona: son sus finanzas, y puede tener motivos que el sistema no
    /// conoce.
    /// </remarks>
    private static string ConstruirCuerpo(
        string nombreMeta,
        ProyeccionMeta proyeccion,
        decimal disponible,
        ContextoRecomendacion contexto_)
    {
        var moneda = contexto_.MonedaBase;
        var aporte = proyeccion.AporteMensualNecesario ?? 0m;

        var texto = $"Para alcanzar «{nombreMeta}» el "
                    + $"{proyeccion.FechaObjetivo:dd/MM/yyyy} faltan "
                    + $"{Formatear(proyeccion.MontoFaltante, moneda)} y quedan "
                    + $"{proyeccion.MesesRestantes} meses. El aporte necesario es de "
                    + $"{Formatear(aporte, moneda)} al mes.";

        if (proyeccion.PlazoVencido)
        {
            return $"La fecha objetivo de «{nombreMeta}» ya pasó y faltan "
                   + $"{Formatear(proyeccion.MontoFaltante, moneda)}. Puedes ajustar la fecha "
                   + "o el monto para volver a tener un plan realista.";
        }

        if (contexto_.FlujoCaja.ConfianzaBaja)
        {
            return texto + " Todavía hay poco historial registrado "
                   + $"({contexto_.FlujoCaja.MesesAnalizados} "
                   + (contexto_.FlujoCaja.MesesAnalizados == 1 ? "mes" : "meses")
                   + "), así que aún no se puede contrastar con tu excedente habitual.";
        }

        if (disponible <= 0m)
        {
            return texto + " En los últimos "
                   + $"{contexto_.FlujoCaja.MesesAnalizados} meses el hogar no tuvo excedente "
                   + "una vez descontados los pagos recurrentes, así que este aporte exigiría "
                   + "reducir algún gasto.";
        }

        if (aporte > disponible)
        {
            return texto + $" Tu excedente disponible es de {Formatear(disponible, moneda)} al "
                   + "mes, así que este ritmo quedaría por encima de lo que el hogar viene "
                   + "ahorrando. Ampliar la fecha o ajustar el monto lo haría más alcanzable.";
        }

        return texto + $" Durante los últimos {contexto_.FlujoCaja.MesesAnalizados} meses el "
               + $"excedente disponible fue de {Formatear(disponible, moneda)} mensuales, así "
               + "que este aporte es compatible con ese promedio si los gastos se mantienen.";
    }

    /// <summary>Da formato a un importe con su moneda.</summary>
    /// <param name="monto">Importe.</param>
    /// <param name="moneda">Codigo ISO-4217.</param>
    /// <returns>El texto formateado.</returns>
    private static string Formatear(decimal monto, string moneda) =>
        $"{moneda} {monto:N2}";
}
