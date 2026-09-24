using System.Collections.ObjectModel;

using Rumbo.Contratos.Viajes;
using Rumbo.Movil.Comun;
using Rumbo.Movil.Servicios;

namespace Rumbo.Movil.ModelosVista;

/// <summary>Un viaje tal como se ve en la lista.</summary>
/// <param name="Viaje">El viaje original, para poder consultar su viabilidad.</param>
/// <param name="Nombre">Nombre y destino.</param>
/// <param name="Fechas">Cuando es y cuanto falta.</param>
/// <param name="Presupuesto">Coste y fondo reunido.</param>
/// <param name="Fraccion">Parte financiada, de 0 a 1.</param>
public record ViajeEnLista(
    ViajeDetalle Viaje,
    string Nombre,
    string Fechas,
    string Presupuesto,
    double Fraccion);

/// <summary>Un escenario de viabilidad, ya formateado.</summary>
/// <param name="Nombre">Conservador, Esperado u Optimista.</param>
/// <param name="Descripcion">Que supone y a que lleva.</param>
/// <param name="Alcanza">Si con ese ritmo se llega.</param>
public record EscenarioEnLista(string Nombre, string Descripcion, bool Alcanza);

/// <summary>
/// La pantalla de viajes y la respuesta a «¿podemos permitirnos este viaje?».
/// </summary>
/// <param name="planificacion">Servicio de planificacion.</param>
public class ViajesModeloVista(ServicioApiPlanificacion planificacion) : ModeloVistaBase
{
    private ViajeEnLista? _viajeSeleccionado;
    private ViabilidadViajeDto? _viabilidad;

    /// <summary>Los viajes planificados.</summary>
    public ObservableCollection<ViajeEnLista> Viajes { get; } = [];

    /// <summary>Los tres escenarios del viaje elegido.</summary>
    public ObservableCollection<EscenarioEnLista> Escenarios { get; } = [];

    /// <summary>Viaje del que se muestra la viabilidad.</summary>
    public ViajeEnLista? ViajeSeleccionado
    {
        get => _viajeSeleccionado;
        set
        {
            if (Establecer(ref _viajeSeleccionado, value) && value is not null)
            {
                // Al elegir un viaje se consulta su viabilidad al momento. Es la pregunta
                // que la persona tiene en la cabeza al tocar el viaje; hacerle pulsar otro
                // boton para verla seria poner una puerta donde no hace falta.
                _ = ConsultarViabilidadAsync(value.Viaje.Id);
            }
        }
    }

    /// <summary>Indica si hay una viabilidad calculada que mostrar.</summary>
    public bool HayViabilidad => _viabilidad is not null;

    /// <summary>Si, Ajustado o No.</summary>
    public string Veredicto => _viabilidad?.Veredicto ?? string.Empty;

    /// <summary>Color del veredicto.</summary>
    public Color ColorVeredicto => _viabilidad?.Veredicto switch
    {
        "Si" => Color.FromArgb("#2E7D32"),
        "Ajustado" => Color.FromArgb("#B58900"),
        "No" => Color.FromArgb("#B3261E"),
        _ => Colors.Gray,
    };

    /// <summary>El porque, en una frase.</summary>
    public string Explicacion => _viabilidad?.Explicacion ?? string.Empty;

    /// <summary>Cuanto habria que apartar al mes.</summary>
    public string AporteNecesario => _viabilidad is null
        ? string.Empty
        : $"Harían falta {Dinero.Formatear(_viabilidad.AporteMensualNecesario)} al mes, "
          + $"y al hogar le sobran {Dinero.Formatear(_viabilidad.DisponibleMensual)}.";

    /// <summary>Aviso de que la proyeccion se apoya en poco historial.</summary>
    public bool ConfianzaBaja => _viabilidad?.ConfianzaBaja ?? false;

    /// <summary>Recarga los viajes.</summary>
    public ComandoSimple CargarComando => _cargarComando ??= new ComandoSimple(CargarAsync);

    private ComandoSimple? _cargarComando;

    /// <summary>Pide los viajes al servidor.</summary>
    /// <returns>Tarea que finaliza cuando termina la carga.</returns>
    public async Task CargarAsync() =>
        await EjecutarAsync(async () =>
        {
            var viajes = await planificacion.ListarViajesAsync();

            Viajes.Clear();

            foreach (var viaje in viajes)
            {
                var falta = viaje.DiasHastaLaSalida;

                var fechas = falta >= 0
                    ? $"{viaje.FechaInicio:dd/MM/yyyy} · faltan {falta} días"
                    : $"{viaje.FechaInicio:dd/MM/yyyy} · ya pasó";

                Viajes.Add(new ViajeEnLista(
                    viaje,
                    string.IsNullOrWhiteSpace(viaje.Destino)
                        ? viaje.Nombre
                        : $"{viaje.Nombre} · {viaje.Destino}",
                    fechas,
                    $"{Dinero.Formatear(viaje.FondoActual)} reunidos de "
                        + Dinero.Formatear(viaje.PresupuestoTotal),
                    Math.Min(1d, (double)viaje.PorcentajeFinanciado / 100d)));
            }
        });

    /// <summary>Consulta la viabilidad de un viaje.</summary>
    /// <param name="viajeId">Viaje analizado.</param>
    /// <returns>Tarea que finaliza cuando termina la consulta.</returns>
    /// <remarks>
    /// Es una PROYECCION para decidir: no mueve dinero, no crea aportes y no reserva nada.
    /// </remarks>
    private async Task ConsultarViabilidadAsync(Guid viajeId) =>
        await EjecutarAsync(async () =>
        {
            _viabilidad = await planificacion.ConsultarViabilidadAsync(viajeId);

            Escenarios.Clear();

            foreach (var escenario in _viabilidad.Escenarios)
            {
                var descripcion =
                    $"Apartando el {Dinero.Porcentaje(escenario.PorcentajeDelDisponible)} "
                    + $"({Dinero.Formatear(escenario.AporteMensualSupuesto)} al mes): "
                    + (escenario.Alcanza
                        ? "se llega."
                        : $"faltarían {Dinero.Formatear(escenario.Faltante)}.");

                Escenarios.Add(new EscenarioEnLista(
                    escenario.Nombre, descripcion, escenario.Alcanza));
            }

            Avisar(nameof(HayViabilidad));
            Avisar(nameof(Veredicto));
            Avisar(nameof(ColorVeredicto));
            Avisar(nameof(Explicacion));
            Avisar(nameof(AporteNecesario));
            Avisar(nameof(ConfianzaBaja));
        });
}
