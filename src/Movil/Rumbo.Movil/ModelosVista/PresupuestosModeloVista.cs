using System.Collections.ObjectModel;

using Rumbo.Contratos.Categorias;
using Rumbo.Contratos.Presupuestos;
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
/// <param name="finanzas">Servicio de categorias.</param>
public class PresupuestosModeloVista(
    ServicioApiPlanificacion planificacion,
    ServicioApiFinanzas finanzas) : ModeloVistaBase
{
    private string _titulo = "Presupuesto";
    private string _resumen = string.Empty;
    private bool _mostrandoFormulario;
    private CategoriaArbol? _categoriaNueva;
    private string _montoNueva = string.Empty;

    /// <summary>El presupuesto vigente, si lo hay.</summary>
    /// <remarks>
    /// Se conserva entero porque al anadir una partida hay que reenviar TODAS: el servidor
    /// reemplaza el desglose en bloque, no anade una linea suelta.
    /// </remarks>
    private PresupuestoDetalle? _vigente;

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

    /// <summary>Categorias de gasto entre las que elegir.</summary>
    public ObservableCollection<CategoriaArbol> Categorias { get; } = [];

    /// <summary>Indica si el formulario esta abierto.</summary>
    public bool MostrandoFormulario
    {
        get => _mostrandoFormulario;
        private set => Establecer(ref _mostrandoFormulario, value);
    }

    /// <summary>Categoria de la partida que se va a anadir.</summary>
    public CategoriaArbol? CategoriaNueva
    {
        get => _categoriaNueva;
        set
        {
            if (Establecer(ref _categoriaNueva, value))
            {
                AnadirPartidaComando.Refrescar();
            }
        }
    }

    /// <summary>Importe que se asigna a esa categoria.</summary>
    public string MontoNueva
    {
        get => _montoNueva;
        set
        {
            if (Establecer(ref _montoNueva, value))
            {
                AnadirPartidaComando.Refrescar();
            }
        }
    }

    /// <summary>Abre o cierra el formulario.</summary>
    public ComandoSimple AlternarFormularioComando => _alternarComando ??=
        new ComandoSimple(() =>
        {
            MostrandoFormulario = !MostrandoFormulario;

            return Task.CompletedTask;
        });

    /// <summary>Anade una partida, creando el presupuesto del mes si no existe.</summary>
    public ComandoSimple AnadirPartidaComando => _anadirComando ??=
        new ComandoSimple(
            AnadirPartidaAsync,
            () => CategoriaNueva is not null && Dinero.Leer(MontoNueva) > 0m);

    /// <summary>Recarga el presupuesto.</summary>
    public ComandoSimple CargarComando => _cargarComando ??= new ComandoSimple(CargarAsync);

    private ComandoSimple? _cargarComando;
    private ComandoSimple? _alternarComando;
    private ComandoSimple? _anadirComando;

    /// <summary>
    /// Anade una partida al presupuesto del mes, creandolo si todavia no existe.
    /// </summary>
    /// <returns>Tarea que finaliza cuando termina.</returns>
    /// <remarks>
    /// Se resuelven los dos casos en un solo boton a proposito. Pedir primero "crear
    /// presupuesto" y luego "anadir partida" serian dos pasos para algo que la persona vive
    /// como uno: decidir cuanto quiere gastar en comida este mes.
    /// </remarks>
    private async Task AnadirPartidaAsync() =>
        await EjecutarAsync(async () =>
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var inicio = new DateOnly(hoy.Year, hoy.Month, 1);
            var fin = inicio.AddMonths(1).AddDays(-1);

            var lineas = new List<LineaPresupuestoSolicitud>();

            if (_vigente is not null)
            {
                // Las que ya existen se reenvian tal cual: el servidor reemplaza el
                // desglose entero y omitirlas las borraria.
                lineas.AddRange(_vigente.Lineas.Select(l => new LineaPresupuestoSolicitud(
                    l.CategoriaId, l.MontoAsignado, null, null, null)));
            }

            if (lineas.Any(l => l.CategoriaId == CategoriaNueva!.Id))
            {
                throw new InvalidOperationException(
                    $"«{CategoriaNueva!.Nombre}» ya está en el presupuesto.");
            }

            lineas.Add(new LineaPresupuestoSolicitud(
                CategoriaNueva!.Id, Dinero.Leer(MontoNueva), null, null, null));

            var solicitud = new SolicitudGuardarPresupuesto(
                _vigente?.Nombre ?? $"Presupuesto de {inicio:MMMM yyyy}",
                "Mensual",
                _vigente?.InicioPeriodo ?? inicio,
                _vigente?.FinPeriodo ?? fin,
                null,
                null,
                lineas);

            if (_vigente is null)
            {
                await planificacion.CrearPresupuestoAsync(solicitud);
            }
            else
            {
                await planificacion.ActualizarPresupuestoAsync(_vigente.Id, solicitud);
            }

            MontoNueva = string.Empty;

            await CargarAsync();
        });

    /// <summary>Pide el presupuesto vigente al servidor.</summary>
    /// <returns>Tarea que finaliza cuando termina la carga.</returns>
    public async Task CargarAsync() =>
        await EjecutarAsync(async () =>
        {
            var presupuestos = await planificacion.ListarPresupuestosAsync(soloVigente: true);

            Partidas.Clear();

            var vigente = presupuestos.FirstOrDefault();
            _vigente = vigente;

            if (Categorias.Count == 0)
            {
                // Solo las de GASTO: un presupuesto limita lo que sale, no lo que entra. El
                // servidor rechaza una categoria de ingreso, y enterarse despues de
                // escribirlo todo seria molesto.
                foreach (var padre in await finanzas.ListarCategoriasAsync())
                {
                    foreach (var hija in padre.Subcategorias.Count > 0
                                 ? padre.Subcategorias
                                 : [padre])
                    {
                        if (hija.Tipo is "Gasto" or "Ambos")
                        {
                            Categorias.Add(hija);
                        }
                    }
                }

                CategoriaNueva = Categorias.FirstOrDefault();
            }

            if (vigente is null)
            {
                Titulo = "Sin presupuesto este mes";
                Resumen = "Añade una partida para empezar a controlar en qué se va el dinero.";

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
