using System.Collections.ObjectModel;

using Rumbo.Contratos.Cuentas;
using Rumbo.Contratos.Metas;
using Rumbo.Movil.Comun;
using Rumbo.Movil.Servicios;

namespace Rumbo.Movil.ModelosVista;

/// <summary>Una meta tal como se ve en la lista.</summary>
/// <param name="Meta">La meta original, para poder aportar sobre ella.</param>
/// <param name="Nombre">Nombre de la meta.</param>
/// <param name="Progreso">Reunido de objetivo, ya formateado.</param>
/// <param name="Fraccion">Avance de 0 a 1, para la barra.</param>
/// <param name="Ritmo">Cuanto habria que aportar al mes, o el estado si no hay fecha.</param>
/// <param name="VaAtrasada">Si al ritmo prometido no llegaria.</param>
public record MetaEnLista(
    MetaDetalle Meta,
    string Nombre,
    string Progreso,
    double Fraccion,
    string Ritmo,
    bool VaAtrasada);

/// <summary>
/// La pantalla de metas de ahorro.
/// </summary>
/// <param name="planificacion">Servicio de planificacion.</param>
/// <param name="finanzas">Servicio de cuentas, para elegir de donde sale el dinero.</param>
public class MetasModeloVista(
    ServicioApiPlanificacion planificacion,
    ServicioApiFinanzas finanzas) : ModeloVistaBase
{
    private MetaEnLista? _metaSeleccionada;
    private CuentaResumen? _cuentaOrigen;
    private string _montoAporte = string.Empty;
    private bool _mostrandoFormulario;
    private string _nombreNueva = string.Empty;
    private string _objetivoNueva = string.Empty;
    private string _prioridadNueva = "Media";
    private bool _conFecha;
    private DateTime _fechaObjetivo = DateTime.Today.AddMonths(12);
    private CuentaResumen? _cuentaVinculada;

    /// <summary>Las metas activas.</summary>
    public ObservableCollection<MetaEnLista> Metas { get; } = [];

    /// <summary>Cuentas desde las que se puede aportar.</summary>
    public ObservableCollection<CuentaResumen> Cuentas { get; } = [];

    /// <summary>Meta a la que se va a aportar.</summary>
    public MetaEnLista? MetaSeleccionada
    {
        get => _metaSeleccionada;
        set
        {
            if (Establecer(ref _metaSeleccionada, value))
            {
                Avisar(nameof(HayMetaSeleccionada));
                AportarComando.Refrescar();
            }
        }
    }

    /// <summary>Indica si hay una meta elegida y el formulario debe verse.</summary>
    public bool HayMetaSeleccionada => MetaSeleccionada is not null;

    /// <summary>Cuenta de la que sale el dinero.</summary>
    public CuentaResumen? CuentaOrigen
    {
        get => _cuentaOrigen;
        set
        {
            if (Establecer(ref _cuentaOrigen, value))
            {
                AportarComando.Refrescar();
            }
        }
    }

    /// <summary>Importe del aporte.</summary>
    public string MontoAporte
    {
        get => _montoAporte;
        set
        {
            if (Establecer(ref _montoAporte, value))
            {
                AportarComando.Refrescar();
            }
        }
    }

    /// <summary>Prioridades entre las que elegir.</summary>
    public IReadOnlyList<string> Prioridades { get; } = ["Baja", "Media", "Alta", "Critica"];

    /// <summary>Indica si el formulario de alta esta abierto.</summary>
    public bool MostrandoFormulario
    {
        get => _mostrandoFormulario;
        private set => Establecer(ref _mostrandoFormulario, value);
    }

    /// <summary>Nombre de la meta que se va a crear.</summary>
    public string NombreNueva
    {
        get => _nombreNueva;
        set
        {
            if (Establecer(ref _nombreNueva, value))
            {
                CrearComando.Refrescar();
            }
        }
    }

    /// <summary>Cuanto se quiere reunir.</summary>
    public string ObjetivoNueva
    {
        get => _objetivoNueva;
        set
        {
            if (Establecer(ref _objetivoNueva, value))
            {
                CrearComando.Refrescar();
            }
        }
    }

    /// <summary>Prioridad elegida.</summary>
    public string PrioridadNueva
    {
        get => _prioridadNueva;
        set => Establecer(ref _prioridadNueva, value);
    }

    /// <summary>Si la meta tiene fecha limite.</summary>
    /// <remarks>
    /// Es opcional a proposito. Sin fecha no hay ritmo que calcular, y el servidor lo dice
    /// asi en vez de inventarse una urgencia que nadie ha pedido. Un fondo de emergencia no
    /// tiene plazo; un viaje si.
    /// </remarks>
    public bool ConFecha
    {
        get => _conFecha;
        set => Establecer(ref _conFecha, value);
    }

    /// <summary>Fecha limite, si la tiene.</summary>
    public DateTime FechaObjetivo
    {
        get => _fechaObjetivo;
        set => Establecer(ref _fechaObjetivo, value);
    }

    /// <summary>Cuenta de ahorro donde se acumulara.</summary>
    /// <remarks>
    /// El servidor la exige para poder aportar: sin cuenta, el acumulado subiria sin que
    /// ningun saldo bajara y el hogar creeria tener ese dinero dos veces.
    /// </remarks>
    public CuentaResumen? CuentaVinculada
    {
        get => _cuentaVinculada;
        set
        {
            if (Establecer(ref _cuentaVinculada, value))
            {
                CrearComando.Refrescar();
            }
        }
    }

    /// <summary>Abre o cierra el formulario de alta.</summary>
    public ComandoSimple AlternarFormularioComando => _alternarComando ??=
        new ComandoSimple(() =>
        {
            MostrandoFormulario = !MostrandoFormulario;

            return Task.CompletedTask;
        });

    /// <summary>Crea la meta del formulario.</summary>
    public ComandoSimple CrearComando => _crearComando ??=
        new ComandoSimple(CrearAsync, PuedeCrear);

    /// <summary>Recarga las metas.</summary>
    public ComandoSimple CargarComando => _cargarComando ??= new ComandoSimple(CargarAsync);

    /// <summary>Aporta a la meta elegida.</summary>
    public ComandoSimple AportarComando => _aportarComando ??=
        new ComandoSimple(AportarAsync, PuedeAportar);

    private ComandoSimple? _cargarComando;
    private ComandoSimple? _aportarComando;
    private ComandoSimple? _alternarComando;
    private ComandoSimple? _crearComando;

    /// <summary>Indica si ya se puede crear la meta.</summary>
    /// <returns><c>true</c> si el formulario esta completo.</returns>
    private bool PuedeCrear() =>
        !string.IsNullOrWhiteSpace(NombreNueva)
        && Dinero.Leer(ObjetivoNueva) > 0m
        && CuentaVinculada is not null;

    /// <summary>Crea la meta y recarga la lista.</summary>
    /// <returns>Tarea que finaliza cuando termina.</returns>
    private async Task CrearAsync() =>
        await EjecutarAsync(async () =>
        {
            await planificacion.CrearMetaAsync(new SolicitudGuardarMeta(
                NombreNueva.Trim(),
                null,
                Dinero.Leer(ObjetivoNueva),
                null,
                ConFecha ? DateOnly.FromDateTime(FechaObjetivo) : null,
                PrioridadNueva,
                null,
                CuentaVinculada!.Id,
                null));

            NombreNueva = string.Empty;
            ObjetivoNueva = string.Empty;
            MostrandoFormulario = false;

            await CargarAsync();
        });

    /// <summary>Pide las metas y las cuentas al servidor.</summary>
    /// <returns>Tarea que finaliza cuando termina la carga.</returns>
    public async Task CargarAsync() =>
        await EjecutarAsync(async () =>
        {
            var metas = await planificacion.ListarMetasAsync();

            Metas.Clear();

            foreach (var meta in metas)
            {
                Metas.Add(Convertir(meta));
            }

            if (Cuentas.Count == 0)
            {
                foreach (var cuenta in await finanzas.ListarCuentasAsync())
                {
                    Cuentas.Add(cuenta);
                }

                CuentaOrigen = Cuentas.FirstOrDefault();

                // Para vincular se propone una cuenta de AHORRO, que es donde tiene sentido
                // acumular. Si no hay ninguna, vale cualquiera: mejor poder crear la meta y
                // corregir la cuenta despues que quedarse bloqueado.
                CuentaVinculada = Cuentas.FirstOrDefault(c => c.Tipo == "Ahorro")
                                  ?? Cuentas.FirstOrDefault();
            }
        });

    /// <summary>Indica si ya se puede aportar.</summary>
    /// <returns><c>true</c> si hay meta, cuenta e importe.</returns>
    private bool PuedeAportar() =>
        MetaSeleccionada is not null
        && CuentaOrigen is not null
        && Dinero.Leer(MontoAporte) > 0m;

    /// <summary>Registra el aporte.</summary>
    /// <returns>Tarea que finaliza cuando termina.</returns>
    /// <remarks>
    /// El aporte es una TRANSFERENCIA hacia la cuenta de ahorro de la meta, no un gasto. Y
    /// lo confirma la persona: Rumbo nunca mueve dinero por su cuenta, ni siquiera cuando el
    /// propio sistema sugirio el importe.
    /// </remarks>
    private async Task AportarAsync() =>
        await EjecutarAsync(async () =>
        {
            var metaId = MetaSeleccionada!.Meta.Id;

            var actualizada = await planificacion.AportarAsync(
                metaId,
                new SolicitudAportarAMeta(
                    CuentaOrigen!.Id,
                    Dinero.Leer(MontoAporte),
                    DateOnly.FromDateTime(DateTime.Now),
                    null,

                    // false: este aporte nacio de que la persona lo escribio, no de aceptar
                    // una sugerencia del motor. Distinguirlo permite medir despues si las
                    // recomendaciones sirven de algo.
                    OrigenRecomendacion: false));

            // Se sustituye la fila en su sitio para que el progreso se vea al momento sin
            // recargar la lista entera contra el servidor.
            var posicion = Metas.IndexOf(MetaSeleccionada!);

            if (posicion >= 0)
            {
                Metas[posicion] = Convertir(actualizada);
                MetaSeleccionada = Metas[posicion];
            }

            MontoAporte = string.Empty;
        });

    /// <summary>Convierte una meta de la API en su fila de la lista.</summary>
    /// <param name="meta">Meta de la API.</param>
    /// <returns>La fila.</returns>
    private static MetaEnLista Convertir(MetaDetalle meta)
    {
        var ritmo = meta.FechaObjetivo is null
            ? "Sin fecha límite"
            : $"{Dinero.Formatear(meta.AporteMensualNecesario)} al mes · "
              + $"{meta.MesesRestantes} meses";

        return new MetaEnLista(
            meta,
            meta.Nombre,
            $"{Dinero.Formatear(meta.MontoActual)} de {Dinero.Formatear(meta.MontoObjetivo)}",
            Math.Min(1d, (double)meta.PorcentajeCompletado / 100d),
            ritmo,
            meta.VaAtrasada);
    }
}
