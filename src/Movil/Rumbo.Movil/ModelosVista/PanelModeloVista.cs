using System.Globalization;

using Rumbo.Contratos.Panel;
using Rumbo.Movil.Comun;
using Rumbo.Movil.Servicios;

namespace Rumbo.Movil.ModelosVista;

/// <summary>
/// La pantalla de inicio: cuanto hay, cuanto entra y cuanto sale este mes.
/// </summary>
/// <param name="servicio">Servicio del panel.</param>
/// <param name="sesion">Sesion activa.</param>
public class PanelModeloVista(ServicioApiPanel servicio, ServicioSesion sesion)
    : ModeloVistaBase
{
    private PanelInicio? _panel;

    /// <summary>Cultura con la que se formatean los importes.</summary>
    /// <remarks>
    /// El formateo ocurre AQUI, en el movil, no en la API. El servidor manda numeros crudos
    /// y cada cliente los presenta como le corresponda. Asi, un importe nunca viaja ya
    /// convertido en texto con el separador equivocado.
    /// </remarks>
    private static readonly CultureInfo Cultura = new("es-DO");

    /// <summary>Datos del panel, o <c>null</c> si todavia no se cargaron.</summary>
    public PanelInicio? Panel
    {
        get => _panel;
        private set
        {
            Establecer(ref _panel, value);

            // Se avisa de TODAS las propiedades calculadas que dependen de esta. Si se
            // olvida una, esa parte de la pantalla se queda en blanco sin dar ningun error,
            // que es el fallo mas dificil de encontrar de MAUI.
            Avisar(nameof(HayDatos));
            Avisar(nameof(NombreEspacio));
            Avisar(nameof(TotalDisponible));
            Avisar(nameof(PatrimonioNeto));
            Avisar(nameof(IngresosDelMes));
            Avisar(nameof(GastosDelMes));
            Avisar(nameof(BalanceDelMes));
            Avisar(nameof(TieneAlertas));
            Avisar(nameof(ResumenAlertas));
        }
    }

    /// <summary>Indica si ya hay datos que mostrar.</summary>
    public bool HayDatos => Panel is not null;

    /// <summary>Nombre del hogar.</summary>
    public string NombreEspacio =>
        Panel?.NombreEspacio ?? sesion.EspacioActivo?.Nombre ?? "Rumbo";

    /// <summary>Saldo sumado de todas las cuentas.</summary>
    public string TotalDisponible => Formatear(Panel?.Patrimonio.TotalDisponible);

    /// <summary>Lo disponible menos las deudas.</summary>
    public string PatrimonioNeto => Formatear(Panel?.Patrimonio.PatrimonioNeto);

    /// <summary>Ingresos del mes en curso.</summary>
    public string IngresosDelMes => Formatear(Panel?.MesEnCurso.TotalIngresos);

    /// <summary>Gastos del mes en curso.</summary>
    public string GastosDelMes => Formatear(Panel?.MesEnCurso.TotalGastos);

    /// <summary>Lo que queda del mes.</summary>
    public string BalanceDelMes => Formatear(Panel?.MesEnCurso.Balance);

    /// <summary>Indica si hay alguna partida de presupuesto pasada de rosca.</summary>
    public bool TieneAlertas => Panel?.AlertasPresupuesto.Count > 0;

    /// <summary>Resumen de las alertas, para mostrarlo en una linea.</summary>
    public string ResumenAlertas => Panel is null || Panel.AlertasPresupuesto.Count == 0
        ? string.Empty
        : string.Join(
            "\n",
            Panel.AlertasPresupuesto.Select(a =>
                $"{a.NombreCategoria}: {a.PorcentajeConsumido:0.#} % ({a.Nivel})"));

    /// <summary>Recarga el panel.</summary>
    public ComandoSimple CargarComando => _cargarComando ??= new ComandoSimple(CargarAsync);

    private ComandoSimple? _cargarComando;

    /// <summary>Pide el panel al servidor.</summary>
    /// <returns>Tarea que finaliza cuando termina la carga.</returns>
    public async Task CargarAsync() =>
        await EjecutarAsync(async () => Panel = await servicio.ObtenerAsync());

    /// <summary>Convierte un importe en texto con formato dominicano.</summary>
    /// <param name="valor">Importe.</param>
    /// <returns>El texto, o un guion si no hay dato.</returns>
    private static string Formatear(decimal? valor) =>
        valor is null ? "—" : valor.Value.ToString("C2", Cultura);
}
