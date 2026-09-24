using System.Collections.ObjectModel;

using Rumbo.Movil.Comun;
using Rumbo.Movil.Servicios;

namespace Rumbo.Movil.ModelosVista;

/// <summary>Una partida de presupuesto tal como se ve en la lista.</summary>
/// <param name="Categoria">Categoria que se limita.</param>
/// <param name="Cifras">Gastado de asignado, ya formateado.</param>
/// <param name="Consumido">Porcentaje consumido, de 0 a 1, para la barra.</param>
/// <param name="TextoNivel">Normal, Aviso, Critico o Excedido, con su porcentaje.</param>
/// <param name="Color">Color del nivel.</param>
public record PartidaEnLista(
    string Categoria,
    string Cifras,
    double Consumido,
    string TextoNivel,
    Color Color);

/// <summary>
/// La pantalla de presupuestos del periodo vigente.
/// </summary>
/// <param name="planificacion">Servicio de planificacion.</param>
public class PresupuestosModeloVista(ServicioApiPlanificacion planificacion) : ModeloVistaBase
{
    private string _titulo = "Presupuesto";
    private string _resumen = string.Empty;

    /// <summary>Las partidas del presupuesto vigente.</summary>
    public ObservableCollection<PartidaEnLista> Partidas { get; } = [];

    /// <summary>Nombre y periodo del presupuesto.</summary>
    public string Titulo
    {
        get => _titulo;
        private set => Establecer(ref _titulo, value);
    }

    /// <summary>Totales del presupuesto, en una linea.</summary>
    public string Resumen
    {
        get => _resumen;
        private set => Establecer(ref _resumen, value);
    }

    /// <summary>Recarga el presupuesto.</summary>
    public ComandoSimple CargarComando => _cargarComando ??= new ComandoSimple(CargarAsync);

    private ComandoSimple? _cargarComando;

    /// <summary>Pide el presupuesto vigente al servidor.</summary>
    /// <returns>Tarea que finaliza cuando termina la carga.</returns>
    public async Task CargarAsync() =>
        await EjecutarAsync(async () =>
        {
            var presupuestos = await planificacion.ListarPresupuestosAsync(soloVigente: true);

            Partidas.Clear();

            var vigente = presupuestos.FirstOrDefault();

            if (vigente is null)
            {
                Titulo = "Sin presupuesto este mes";
                Resumen = "Crea uno para empezar a controlar en qué se va el dinero.";

                return;
            }

            Titulo = vigente.Nombre;

            Resumen = $"{Dinero.Formatear(vigente.TotalGastado)} de "
                      + $"{Dinero.Formatear(vigente.TotalAsignado)} · queda "
                      + Dinero.Formatear(vigente.TotalDisponible);

            // Se ordenan por consumo, de mas a menos: lo que esta a punto de desbordarse
            // tiene que verse primero, no perdido a mitad de la lista.
            foreach (var linea in vigente.Lineas.OrderByDescending(l => l.PorcentajeConsumido))
            {
                Partidas.Add(new PartidaEnLista(
                    linea.NombreCategoria,
                    $"{Dinero.Formatear(linea.MontoGastado)} de "
                        + Dinero.Formatear(linea.MontoAsignado),

                    // La barra de progreso va de 0 a 1, y se corta en 1: una partida al
                    // 150 % no puede dibujar una barra vez y media mas larga que la
                    // pantalla. El exceso se ve en el texto y en el color.
                    Math.Min(1d, (double)linea.PorcentajeConsumido / 100d),

                    $"{linea.Nivel} · {Dinero.Porcentaje(linea.PorcentajeConsumido)}",
                    ColorDelNivel(linea.Nivel)));
            }
        });

    /// <summary>Color con el que se pinta cada nivel de alerta.</summary>
    /// <param name="nivel">Normal, Aviso, Critico o Excedido.</param>
    /// <returns>El color.</returns>
    /// <remarks>
    /// El color acompana al texto, nunca lo sustituye: quien no distingue el rojo del verde
    /// sigue leyendo "Excedido" igual de claro.
    /// </remarks>
    private static Color ColorDelNivel(string nivel) => nivel switch
    {
        "Excedido" => Color.FromArgb("#B3261E"),
        "Critico" => Color.FromArgb("#E06C00"),
        "Aviso" => Color.FromArgb("#B58900"),
        _ => Color.FromArgb("#2E7D32"),
    };
}
