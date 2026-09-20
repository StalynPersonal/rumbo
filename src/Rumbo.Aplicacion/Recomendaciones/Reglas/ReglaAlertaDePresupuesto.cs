using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Calculadoras;
using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Modulos.Movimientos;
using Rumbo.Dominio.Enums;

namespace Rumbo.Aplicacion.Recomendaciones.Reglas;

/// <summary>
/// Avisa cuando una partida del presupuesto se acerca a su limite o lo supera.
/// </summary>
/// <remarks>
/// Avisa tambien por PROYECCION, no solo por consumo: si el dia 15 se lleva gastado el 75 %
/// de la partida, el mes cerrara excedido aunque el umbral todavia no se haya alcanzado.
/// Decirlo el dia 15 deja margen de reaccion; decirlo el dia 28 ya no sirve de nada.
/// </remarks>
/// <param name="contexto">Acceso a los datos.</param>
public class ReglaAlertaDePresupuesto(IContextoRumbo contexto) : IReglaRecomendacion
{
    /// <inheritdoc />
    public int Prioridad => 5;

    /// <inheritdoc />
    public string Nombre => "Alerta de presupuesto";

    /// <inheritdoc />
    public async Task<IReadOnlyList<SugerenciaGenerada>> EvaluarAsync(
        ContextoRecomendacion contexto_,
        CancellationToken cancelacion = default)
    {
        var presupuesto = await contexto.Presupuestos
            .AsNoTracking()
            .Include(p => p.Lineas)
            .ThenInclude(l => l.Categoria)
            .Where(p => p.Estado == EstadoPresupuesto.Activo
                        && p.InicioPeriodo <= contexto_.Hoy
                        && p.FinPeriodo >= contexto_.Hoy)
            .FirstOrDefaultAsync(cancelacion);

        if (presupuesto is null || presupuesto.Lineas.Count == 0)
        {
            return [];
        }

        var configuracion = await contexto.ConfiguracionesEspacio
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.EspacioId == contexto_.EspacioId, cancelacion);

        var avisoPorDefecto = configuracion?.UmbralAvisoPresupuesto ?? 80m;
        var criticoPorDefecto = configuracion?.UmbralCriticoPresupuesto ?? 90m;
        var excedidoPorDefecto = configuracion?.UmbralExcedidoPresupuesto ?? 100m;

        var categorias = presupuesto.Lineas.Select(l => l.CategoriaId).ToList();

        // El gasto real sale del libro mayor, filtrado por SoloGastos: si se contaran las
        // transferencias, mover dinero a la cuenta de ahorro consumiria el presupuesto.
        var gastos = await contexto.Movimientos
            .AsNoTracking()
            .SoloGastos()
            .EnRango(presupuesto.InicioPeriodo, presupuesto.FinPeriodo)
            .Where(m => m.CategoriaId != null && categorias.Contains(m.CategoriaId.Value))
            .GroupBy(m => m.CategoriaId!.Value)
            .Select(g => new { CategoriaId = g.Key, Total = g.Sum(m => m.MontoEnMonedaBase) })
            .ToDictionaryAsync(g => g.CategoriaId, g => g.Total, cancelacion);

        var sugerencias = new List<SugerenciaGenerada>();

        foreach (var linea in presupuesto.Lineas)
        {
            var gastado = gastos.TryGetValue(linea.CategoriaId, out var total) ? total : 0m;

            var estado = CalculadoraPresupuestos.Evaluar(
                linea.MontoAsignado,
                gastado,
                linea.UmbralAviso ?? avisoPorDefecto,
                linea.UmbralCritico ?? criticoPorDefecto,
                linea.UmbralExcedido ?? excedidoPorDefecto,
                presupuesto.InicioPeriodo,
                presupuesto.FinPeriodo,
                contexto_.Hoy);

            var nombreCategoria = linea.Categoria?.Nombre ?? "una categoría";

            var proyeccionSupera = estado.ProyeccionAlCierre > linea.MontoAsignado;

            if (estado.Nivel == NivelAlertaPresupuesto.Normal && !proyeccionSupera)
            {
                continue;
            }

            sugerencias.Add(new SugerenciaGenerada(
                estado.Nivel == NivelAlertaPresupuesto.Excedido
                    ? TipoRecomendacion.AlertaDePresupuesto
                    : TipoRecomendacion.AlertaDePresupuesto,
                TituloPara(estado.Nivel, nombreCategoria),
                CuerpoPara(estado, nombreCategoria, contexto_.MonedaBase, proyeccionSupera),
                new Dictionary<string, object?>
                {
                    ["categoria"] = nombreCategoria,
                    ["montoAsignado"] = estado.MontoAsignado,
                    ["montoGastado"] = estado.MontoGastado,
                    ["montoDisponible"] = estado.MontoDisponible,
                    ["porcentajeConsumido"] = estado.PorcentajeConsumido,
                    ["nivel"] = estado.Nivel.ToString(),
                    ["proyeccionAlCierre"] = estado.ProyeccionAlCierre,
                    ["ritmoDiarioNecesario"] = estado.RitmoDiarioNecesario,
                    ["inicioPeriodo"] = presupuesto.InicioPeriodo,
                    ["finPeriodo"] = presupuesto.FinPeriodo,
                },
                null,
                false,
                null,
                null,

                // Un dia: el consumo cambia con cada gasto que se registre.
                DiasDeVigencia: 1));
        }

        return sugerencias;
    }

    /// <summary>Titulo segun la gravedad.</summary>
    private static string TituloPara(NivelAlertaPresupuesto nivel, string categoria) => nivel switch
    {
        NivelAlertaPresupuesto.Excedido => $"Presupuesto excedido en {categoria}",
        NivelAlertaPresupuesto.Critico => $"{categoria} está muy cerca del límite",
        NivelAlertaPresupuesto.Aviso => $"{categoria} se acerca al límite",
        _ => $"Ritmo de gasto alto en {categoria}",
    };

    /// <summary>Explicacion con las cifras que la sostienen.</summary>
    private static string CuerpoPara(
        EstadoPartida estado,
        string categoria,
        string moneda,
        bool proyeccionSupera)
    {
        var gastado = $"{moneda} {estado.MontoGastado:N2}";
        var asignado = $"{moneda} {estado.MontoAsignado:N2}";

        if (estado.Nivel == NivelAlertaPresupuesto.Excedido)
        {
            var exceso = $"{moneda} {Math.Abs(estado.MontoDisponible):N2}";

            return $"En {categoria} llevas {gastado} de los {asignado} presupuestados: "
                   + $"{exceso} por encima ({estado.PorcentajeConsumido:N0} %).";
        }

        var texto = $"En {categoria} llevas {gastado} de {asignado}, "
                    + $"un {estado.PorcentajeConsumido:N0} % del presupuesto.";

        if (estado.RitmoDiarioNecesario is { } ritmo && ritmo > 0m)
        {
            texto += $" Para no pasarte quedan {moneda} {ritmo:N2} al día.";
        }

        if (proyeccionSupera && estado.ProyeccionAlCierre is { } proyeccion)
        {
            texto += $" Al ritmo actual, el periodo cerraría en {moneda} {proyeccion:N2}.";
        }

        return texto;
    }
}
