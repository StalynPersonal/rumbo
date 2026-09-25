using System.Collections.ObjectModel;
using System.Globalization;

using Rumbo.Contratos.Categorias;
using Rumbo.Contratos.Cuentas;
using Rumbo.Contratos.Movimientos;
using Rumbo.Movil.Comun;
using Rumbo.Movil.Servicios;

namespace Rumbo.Movil.ModelosVista;

/// <summary>Un movimiento tal como se ve en la lista.</summary>
/// <param name="Descripcion">Descripcion corta.</param>
/// <param name="Detalle">Cuenta, categoria y fecha.</param>
/// <param name="Monto">Importe ya formateado y con signo.</param>
/// <param name="EsGasto">Si es una salida de dinero. Sirve para pintarlo de otro color.</param>
/// <param name="Movimiento">
/// El movimiento original. Hace falta para poder editarlo o borrarlo: sin el solo tendriamos
/// el texto ya formateado, y de ahi no se puede sacar un identificador.
/// </param>
public record MovimientoEnLista(
    string Descripcion,
    string Detalle,
    string Monto,
    bool EsGasto,
    MovimientoResumen Movimiento);

/// <summary>
/// La pantalla de movimientos: los ultimos registrados y el alta rapida.
/// </summary>
/// <param name="finanzas">Servicio de cuentas, categorias y movimientos.</param>
public class MovimientosModeloVista(ServicioApiFinanzas finanzas) : ModeloVistaBase
{
    private static readonly CultureInfo Cultura = new("es-DO");

    private string _monto = string.Empty;
    private string _descripcion = string.Empty;
    private bool _esGasto = true;
    private CuentaResumen? _cuentaSeleccionada;
    private CategoriaArbol? _categoriaSeleccionada;
    private MovimientoEnLista? _seleccionado;
    private string _montoEditado = string.Empty;
    private string _descripcionEditada = string.Empty;
    private bool _confirmandoBorrado;

    /// <summary>Los ultimos movimientos.</summary>
    public ObservableCollection<MovimientoEnLista> Movimientos { get; } = [];

    /// <summary>Cuentas disponibles para elegir.</summary>
    public ObservableCollection<CuentaResumen> Cuentas { get; } = [];

    /// <summary>Categorias disponibles para elegir.</summary>
    public ObservableCollection<CategoriaArbol> Categorias { get; } = [];

    /// <summary>Importe que escribe la persona.</summary>
    public string Monto
    {
        get => _monto;
        set
        {
            if (Establecer(ref _monto, value))
            {
                RegistrarComando.Refrescar();
            }
        }
    }

    /// <summary>Descripcion que escribe la persona.</summary>
    public string Descripcion
    {
        get => _descripcion;
        set
        {
            if (Establecer(ref _descripcion, value))
            {
                RegistrarComando.Refrescar();
            }
        }
    }

    /// <summary>Si lo que se registra es un gasto. Si no, es un ingreso.</summary>
    /// <remarks>
    /// Un interruptor y no una lista desplegable: el 95 % de lo que se registra a diario es
    /// un gasto, y un toque de mas cada vez acaba haciendo que la gente no registre nada.
    /// </remarks>
    public bool EsGasto
    {
        get => _esGasto;
        set
        {
            if (Establecer(ref _esGasto, value))
            {
                Avisar(nameof(TextoTipo));
                CargarCategorias();
            }
        }
    }

    /// <summary>Etiqueta del interruptor de tipo.</summary>
    public string TextoTipo => EsGasto ? "Gasto" : "Ingreso";

    /// <summary>Cuenta elegida.</summary>
    public CuentaResumen? CuentaSeleccionada
    {
        get => _cuentaSeleccionada;
        set
        {
            if (Establecer(ref _cuentaSeleccionada, value))
            {
                RegistrarComando.Refrescar();
            }
        }
    }

    /// <summary>Categoria elegida.</summary>
    public CategoriaArbol? CategoriaSeleccionada
    {
        get => _categoriaSeleccionada;
        set
        {
            if (Establecer(ref _categoriaSeleccionada, value))
            {
                RegistrarComando.Refrescar();
            }
        }
    }

    /// <summary>Movimiento elegido para corregir o borrar.</summary>
    public MovimientoEnLista? Seleccionado
    {
        get => _seleccionado;
        set
        {
            if (!Establecer(ref _seleccionado, value))
            {
                return;
            }

            // Al cambiar de movimiento se rellena el panel con sus datos y se cancela
            // cualquier confirmacion pendiente: nadie quiere que un "si, borra" quede
            // apuntando a otra fila.
            ConfirmandoBorrado = false;

            if (value is not null)
            {
                MontoEditado = value.Movimiento.Monto.ToString(Dinero.Cultura);
                DescripcionEditada = value.Movimiento.Descripcion;
            }

            Avisar(nameof(HaySeleccionado));
            Avisar(nameof(EsTraspaso));
            Avisar(nameof(SePuedeEditar));
            Avisar(nameof(TextoBorrar));
            GuardarComando.Refrescar();
        }
    }

    /// <summary>Indica si hay un movimiento elegido.</summary>
    public bool HaySeleccionado => Seleccionado is not null;

    /// <summary>Indica si el elegido es una de las dos patas de un traspaso.</summary>
    public bool EsTraspaso => Seleccionado?.Movimiento.TransferenciaId is not null;

    /// <summary>
    /// Indica si el movimiento elegido admite correccion.
    /// </summary>
    /// <remarks>
    /// Un traspaso no se corrige por una de sus patas: cambiar el importe de un lado sin el
    /// otro dejaria dinero apareciendo de la nada. Se borra entero y se vuelve a hacer.
    /// </remarks>
    public bool SePuedeEditar => HaySeleccionado && !EsTraspaso;

    /// <summary>Importe corregido.</summary>
    public string MontoEditado
    {
        get => _montoEditado;
        set
        {
            if (Establecer(ref _montoEditado, value))
            {
                GuardarComando.Refrescar();
            }
        }
    }

    /// <summary>Descripcion corregida.</summary>
    public string DescripcionEditada
    {
        get => _descripcionEditada;
        set
        {
            if (Establecer(ref _descripcionEditada, value))
            {
                GuardarComando.Refrescar();
            }
        }
    }

    /// <summary>
    /// Indica si se esta pidiendo confirmacion para borrar.
    /// </summary>
    /// <remarks>
    /// Borrar en dos toques y no en uno. Un movimiento es dinero del hogar y el boton esta
    /// justo debajo de la lista: un toque de mas por accidente no deberia poder alterar un
    /// saldo. Se resuelve aqui, en el modelo de vista, y no con un dialogo del sistema, para
    /// que la logica siga siendo probable sin levantar una pantalla.
    /// </remarks>
    public bool ConfirmandoBorrado
    {
        get => _confirmandoBorrado;
        private set
        {
            Establecer(ref _confirmandoBorrado, value);
            Avisar(nameof(TextoBorrar));
        }
    }

    /// <summary>Texto del boton de borrar, segun el paso en que se este.</summary>
    public string TextoBorrar => ConfirmandoBorrado
        ? (EsTraspaso ? "Sí, borrar los dos asientos" : "Sí, borrar")
        : "Borrar";

    /// <summary>Recarga la lista y los desplegables.</summary>
    public ComandoSimple CargarComando => _cargarComando ??= new ComandoSimple(CargarAsync);

    /// <summary>Guarda la correccion del movimiento elegido.</summary>
    public ComandoSimple GuardarComando => _guardarComando ??=
        new ComandoSimple(GuardarAsync, PuedeGuardar);

    /// <summary>Pide confirmacion y, al segundo toque, borra.</summary>
    public ComandoSimple BorrarComando => _borrarComando ??= new ComandoSimple(BorrarAsync);

    /// <summary>Cierra el panel sin tocar nada.</summary>
    public ComandoSimple CancelarComando => _cancelarComando ??=
        new ComandoSimple(() =>
        {
            Seleccionado = null;

            return Task.CompletedTask;
        });

    /// <summary>Registra el movimiento del formulario.</summary>
    public ComandoSimple RegistrarComando => _registrarComando ??=
        new ComandoSimple(RegistrarAsync, PuedeRegistrar);

    private ComandoSimple? _cargarComando;
    private ComandoSimple? _registrarComando;
    private ComandoSimple? _guardarComando;
    private ComandoSimple? _borrarComando;
    private ComandoSimple? _cancelarComando;

    /// <summary>Indica si la correccion se puede guardar.</summary>
    /// <returns><c>true</c> si hay importe y descripcion.</returns>
    private bool PuedeGuardar() =>
        SePuedeEditar
        && Dinero.Leer(MontoEditado) > 0m
        && !string.IsNullOrWhiteSpace(DescripcionEditada);

    /// <summary>Guarda la correccion.</summary>
    /// <returns>Tarea que finaliza cuando termina.</returns>
    /// <remarks>
    /// Solo se cambian importe, descripcion y categoria. El tipo y la cuenta no: cambiar la
    /// cuenta significaria mover dinero de un sitio a otro, y eso es un traspaso, no una
    /// correccion.
    /// </remarks>
    private async Task GuardarAsync() =>
        await EjecutarAsync(async () =>
        {
            var original = Seleccionado!.Movimiento;

            await finanzas.ActualizarMovimientoAsync(
                original.Id,
                new SolicitudActualizarMovimiento(
                    original.CategoriaId,
                    Dinero.Leer(MontoEditado),
                    original.FechaMovimiento,
                    DescripcionEditada.Trim(),
                    original.Notas,
                    original.MetodoPago,
                    original.Reparto,
                    original.PagadoPorUsuarioId,
                    original.ViajeId));

            Seleccionado = null;

            await CargarAsync();
        });

    /// <summary>Pide confirmacion y, al segundo toque, borra.</summary>
    /// <returns>Tarea que finaliza cuando termina.</returns>
    private async Task BorrarAsync()
    {
        if (!ConfirmandoBorrado)
        {
            ConfirmandoBorrado = true;

            return;
        }

        await EjecutarAsync(async () =>
        {
            var original = Seleccionado!.Movimiento;

            if (original.TransferenciaId is { } transferenciaId)
            {
                // Se borra el traspaso ENTERO. Borrar una pata suelta dejaria dinero
                // apareciendo o desapareciendo en la otra cuenta, y el servidor lo rechaza.
                await finanzas.BorrarTransferenciaAsync(transferenciaId);
            }
            else
            {
                await finanzas.BorrarMovimientoAsync(original.Id);
            }

            Seleccionado = null;

            await CargarAsync();
        });
    }

    /// <summary>Todas las categorias, sin filtrar por tipo.</summary>
    private readonly List<CategoriaArbol> _todasLasCategorias = [];

    /// <summary>Carga movimientos, cuentas y categorias.</summary>
    /// <returns>Tarea que finaliza cuando termina la carga.</returns>
    public async Task CargarAsync() =>
        await EjecutarAsync(async () =>
        {
            var pagina = await finanzas.ListarMovimientosAsync();

            Movimientos.Clear();

            foreach (var movimiento in pagina.Elementos)
            {
                Movimientos.Add(Convertir(movimiento));
            }

            // Solo la primera vez: las cuentas y las categorias no cambian entre recargas de
            // la lista, y volver a pedirlas gasta datos del movil sin motivo.
            if (Cuentas.Count == 0)
            {
                foreach (var cuenta in await finanzas.ListarCuentasAsync())
                {
                    Cuentas.Add(cuenta);
                }

                CuentaSeleccionada = Cuentas.FirstOrDefault();
            }

            if (_todasLasCategorias.Count == 0)
            {
                var arbol = await finanzas.ListarCategoriasAsync();

                // Se aplanan los dos niveles: se registra contra la subcategoria, que es la
                // que de verdad clasifica. "Alimentacion" es demasiado vago para un informe.
                foreach (var padre in arbol)
                {
                    _todasLasCategorias.AddRange(
                        padre.Subcategorias.Count > 0 ? padre.Subcategorias : [padre]);
                }

                CargarCategorias();
            }
        });

    /// <summary>Filtra las categorias segun sea gasto o ingreso.</summary>
    /// <remarks>
    /// Sin esto, se podria registrar un gasto en la categoria "Salario". El servidor lo
    /// rechazaria, pero enterarse despues de escribirlo todo es molesto.
    /// </remarks>
    private void CargarCategorias()
    {
        var tipoBuscado = EsGasto ? "Gasto" : "Ingreso";

        Categorias.Clear();

        foreach (var categoria in _todasLasCategorias
                     .Where(c => c.Tipo == tipoBuscado || c.Tipo == "Ambos")
                     .OrderBy(c => c.Nombre))
        {
            Categorias.Add(categoria);
        }

        CategoriaSeleccionada = Categorias.FirstOrDefault();
    }

    /// <summary>Indica si ya se puede registrar.</summary>
    /// <returns><c>true</c> si el formulario esta completo.</returns>
    private bool PuedeRegistrar() =>
        CuentaSeleccionada is not null
        && CategoriaSeleccionada is not null
        && !string.IsNullOrWhiteSpace(Descripcion)
        && LeerMonto() > 0m;

    /// <summary>Convierte el importe escrito en un numero.</summary>
    /// <returns>El importe, o cero si no se entiende.</returns>
    /// <remarks>
    /// Se prueban la cultura dominicana y la invariante. Alguien puede escribir 1500.50 o
    /// 1500,50 segun como tenga configurado el teclado, y rechazarlo por eso seria
    /// incomprensible para quien lo escribe.
    /// </remarks>
    private decimal LeerMonto()
    {
        var texto = Monto.Trim();

        if (decimal.TryParse(texto, NumberStyles.Number, Cultura, out var conCultura))
        {
            return conCultura;
        }

        return decimal.TryParse(
            texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariante)
            ? invariante
            : 0m;
    }

    /// <summary>Registra el movimiento y limpia el formulario.</summary>
    /// <returns>Tarea que finaliza cuando termina el registro.</returns>
    private async Task RegistrarAsync() =>
        await EjecutarAsync(async () =>
        {
            var registrado = await finanzas.RegistrarAsync(new SolicitudRegistrarMovimiento(
                EsGasto ? "Gasto" : "Ingreso",
                CuentaSeleccionada!.Id,
                CategoriaSeleccionada!.Id,
                LeerMonto(),
                null,

                // La fecha del movil, no la del servidor: quien registra sabe cuando gasto.
                DateOnly.FromDateTime(DateTime.Now),

                Descripcion.Trim(),
                null,
                null,
                "Personal",
                null,
                null));

            // Se añade arriba del todo para que se vea el efecto al momento, sin recargar
            // la lista entera contra el servidor.
            Movimientos.Insert(0, Convertir(registrado));

            Monto = string.Empty;
            Descripcion = string.Empty;
        });

    /// <summary>Convierte un movimiento de la API en su fila de la lista.</summary>
    /// <param name="movimiento">Movimiento de la API.</param>
    /// <returns>La fila.</returns>
    private static MovimientoEnLista Convertir(MovimientoResumen movimiento)
    {
        var esGasto = movimiento.Signo < 0;

        var detalle = string.Join(" · ",
            new[]
            {
                movimiento.FechaMovimiento.ToString("dd/MM", Cultura),
                movimiento.NombreCuenta,
                movimiento.NombreCategoria,
            }.Where(p => !string.IsNullOrWhiteSpace(p)));

        return new MovimientoEnLista(
            movimiento.Descripcion,
            detalle,

            // El signo se muestra siempre, para que un ingreso y un gasto no se confundan
            // de un vistazo.
            (esGasto ? "−" : "+") + movimiento.Monto.ToString("C2", Cultura),
            esGasto,
            movimiento);
    }
}
