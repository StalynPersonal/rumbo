using System.Collections.ObjectModel;

using Rumbo.Movil.Comun;
using Rumbo.Movil.Servicios;

namespace Rumbo.Movil.ModelosVista;

/// <summary>Un mes de la serie.</summary>
/// <param name="Etiqueta">Nombre del mes.</param>
/// <param name="Cifras">Ingresos, gastos y balance.</param>
/// <param name="Balance">Balance ya formateado.</param>
/// <param name="EnNumerosRojos">Si se gasto mas de lo que entro.</param>
public record MesEnLista(
    string Etiqueta,
    string Cifras,
    string Balance,
    bool EnNumerosRojos);

/// <summary>Una categoria del desglose de gasto.</summary>
/// <param name="Nombre">Nombre de la categoria.</param>
/// <param name="Total">Importe acumulado.</param>
/// <param name="Fraccion">Parte del total, de 0 a 1.</param>
/// <param name="Porcentaje">Parte del total, en texto.</param>
public record CategoriaEnLista(
    string Nombre,
    string Total,
    double Fraccion,
    string Porcentaje);

/// <summary>
/// La pantalla de informes.
/// </summary>
/// <param name="planificacion">Servicio de planificacion.</param>
public class ReportesModeloVista(ServicioApiPlanificacion planificacion) : ModeloVistaBase
{
    private string _promedios = string.Empty;

    /// <summary>Los meses del periodo.</summary>
    public ObservableCollection<MesEnLista> Meses { get; } = [];

    /// <summary>El gasto desglosado por categoria.</summary>
    public ObservableCollection<CategoriaEnLista> Categorias { get; } = [];

    /// <summary>Medias mensuales, en una linea.</summary>
    public string Promedios
    {
        get => _promedios;
        private set => Establecer(ref _promedios, value);
    }

    /// <summary>Recarga los informes.</summary>
    public ComandoSimple CargarComando => _cargarComando ??= new ComandoSimple(CargarAsync);

    private ComandoSimple? _cargarComando;

    /// <summary>Pide los informes al servidor.</summary>
    /// <returns>Tarea que finaliza cuando termina la carga.</returns>
    public async Task CargarAsync() =>
        await EjecutarAsync(async () =>
        {
            var mensual = await planificacion.ObtenerReporteMensualAsync();

            Promedios = $"De media al mes entran {Dinero.Formatear(mensual.PromedioIngresos)} "
                        + $"y salen {Dinero.Formatear(mensual.PromedioGastos)}.";

            Meses.Clear();

            // Del mes mas reciente al mas antiguo: lo de ahora interesa mas que lo de hace
            // un ano, y en un movil lo primero de la lista es lo unico que mucha gente mira.
            foreach (var mes in mensual.Meses.Reverse())
            {
                Meses.Add(new MesEnLista(
                    mes.Etiqueta,
                    $"Entró {Dinero.Formatear(mes.Ingresos)} · salió "
                        + Dinero.Formatear(mes.Gastos),
                    Dinero.Formatear(mes.Balance),
                    mes.Balance < 0m));
            }

            var porCategoria = await planificacion.ObtenerReporteCategoriasAsync();

            Categorias.Clear();

            foreach (var categoria in porCategoria.Categorias)
            {
                Categorias.Add(new CategoriaEnLista(
                    categoria.Nombre,
                    Dinero.Formatear(categoria.Total),
                    Math.Min(1d, (double)categoria.Porcentaje / 100d),
                    Dinero.Porcentaje(categoria.Porcentaje)));
            }
        });
}
