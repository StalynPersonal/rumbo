using System.Collections.ObjectModel;

using Rumbo.Contratos.Categorias;
using Rumbo.Contratos.Cuentas;
using Rumbo.Contratos.Deudas;
using Rumbo.Movil.Comun;
using Rumbo.Movil.Servicios;

namespace Rumbo.Movil.ModelosVista;

/// <summary>Una deuda tal como se ve en la lista.</summary>
/// <param name="Deuda">La deuda original, para poder pagarla.</param>
/// <param name="Nombre">Nombre y acreedor.</param>
/// <param name="Saldo">Lo que queda por pagar, ya formateado.</param>
/// <param name="Fraccion">Parte del capital ya devuelta, de 0 a 1.</param>
/// <param name="Avance">Pagado de original, y el plazo estimado.</param>
public record DeudaEnLista(
    DeudaDetalle Deuda,
    string Nombre,
    string Saldo,
    double Fraccion,
    string Avance);

/// <summary>
/// La pantalla de deudas.
/// </summary>
/// <param name="deudas">Servicio de deudas.</param>
/// <param name="finanzas">Servicio de cuentas y categorias.</param>
public class DeudasModeloVista(ServicioApiDeudas deudas, ServicioApiFinanzas finanzas)
    : ModeloVistaBase
{
    private DeudaEnLista? _deudaSeleccionada;
    private CuentaResumen? _cuentaOrigen;
    private CategoriaArbol? _categoriaPago;
    private string _capital = string.Empty;
    private string _interes = string.Empty;
    private bool _mostrandoFormulario;
    private string _nombreNueva = string.Empty;
    private string _tipoNueva = "Prestamo";
    private string _originalNueva = string.Empty;
    private string _pendienteNueva = string.Empty;
    private string _cuotaNueva = string.Empty;

    /// <summary>Las deudas activas.</summary>
    public ObservableCollection<DeudaEnLista> Deudas { get; } = [];

    /// <summary>Cuentas desde las que se puede pagar.</summary>
    public ObservableCollection<CuentaResumen> Cuentas { get; } = [];

    /// <summary>Categorias de gasto con las que clasificar el pago.</summary>
    public ObservableCollection<CategoriaArbol> Categorias { get; } = [];

    /// <summary>Tipos de deuda entre los que elegir.</summary>
    public IReadOnlyList<string> TiposDeDeuda { get; } =
        ["Prestamo", "TarjetaCredito", "PrestamoPersonal", "Vehiculo", "Hipoteca", "Otra"];

    /// <summary>Deuda a la que se va a pagar.</summary>
    public DeudaEnLista? DeudaSeleccionada
    {
        get => _deudaSeleccionada;
        set
        {
            if (Establecer(ref _deudaSeleccionada, value))
            {
                Avisar(nameof(HayDeudaSeleccionada));
                PagarComando.Refrescar();
            }
        }
    }

    /// <summary>Indica si hay una deuda elegida y el formulario de pago debe verse.</summary>
    public bool HayDeudaSeleccionada => DeudaSeleccionada is not null;

    /// <summary>Cuenta de la que sale el dinero.</summary>
    public CuentaResumen? CuentaOrigen
    {
        get => _cuentaOrigen;
        set
        {
            if (Establecer(ref _cuentaOrigen, value))
            {
                PagarComando.Refrescar();
            }
        }
    }

    /// <summary>Categoria con la que se clasifica el gasto del pago.</summary>
    public CategoriaArbol? CategoriaPago
    {
        get => _categoriaPago;
        set
        {
            if (Establecer(ref _categoriaPago, value))
            {
                PagarComando.Refrescar();
            }
        }
    }

    /// <summary>Parte del pago que reduce la deuda.</summary>
    public string Capital
    {
        get => _capital;
        set
        {
            if (Establecer(ref _capital, value))
            {
                PagarComando.Refrescar();
            }
        }
    }

    /// <summary>
    /// Parte del pago que se lleva el banco.
    /// </summary>
    /// <remarks>
    /// Se pide aparte y no se calcula: solo el recibo del banco sabe cuanto de la cuota fue
    /// interes. Repartirlo por nuestra cuenta daria una cifra inventada, y el interes pagado
    /// es justo el numero que hace ver lo que cuesta de verdad una deuda.
    /// </remarks>
    public string Interes
    {
        get => _interes;
        set => Establecer(ref _interes, value);
    }

    /// <summary>Indica si el formulario de alta esta abierto.</summary>
    public bool MostrandoFormulario
    {
        get => _mostrandoFormulario;
        private set => Establecer(ref _mostrandoFormulario, value);
    }

    /// <summary>Nombre de la deuda que se va a crear.</summary>
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

    /// <summary>Tipo elegido.</summary>
    public string TipoNueva
    {
        get => _tipoNueva;
        set => Establecer(ref _tipoNueva, value);
    }

    /// <summary>Lo que se pidio prestado.</summary>
    public string OriginalNueva
    {
        get => _originalNueva;
        set
        {
            if (Establecer(ref _originalNueva, value))
            {
                CrearComando.Refrescar();
            }
        }
    }

    /// <summary>Lo que queda por pagar hoy.</summary>
    public string PendienteNueva
    {
        get => _pendienteNueva;
        set
        {
            if (Establecer(ref _pendienteNueva, value))
            {
                CrearComando.Refrescar();
            }
        }
    }

    /// <summary>Cuota habitual, con la que se estima el plazo restante.</summary>
    public string CuotaNueva
    {
        get => _cuotaNueva;
        set => Establecer(ref _cuotaNueva, value);
    }

    /// <summary>Recarga las deudas.</summary>
    public ComandoSimple CargarComando => _cargarComando ??= new ComandoSimple(CargarAsync);

    /// <summary>Abre o cierra el formulario de alta.</summary>
    public ComandoSimple AlternarFormularioComando => _alternarComando ??=
        new ComandoSimple(() =>
        {
            MostrandoFormulario = !MostrandoFormulario;

            return Task.CompletedTask;
        });

    /// <summary>Crea la deuda del formulario.</summary>
    public ComandoSimple CrearComando => _crearComando ??=
        new ComandoSimple(CrearAsync, PuedeCrear);

    /// <summary>Registra el pago.</summary>
    public ComandoSimple PagarComando => _pagarComando ??=
        new ComandoSimple(PagarAsync, PuedePagar);

    private ComandoSimple? _cargarComando;
    private ComandoSimple? _alternarComando;
    private ComandoSimple? _crearComando;
    private ComandoSimple? _pagarComando;

    /// <summary>Pide las deudas, las cuentas y las categorias.</summary>
    /// <returns>Tarea que finaliza cuando termina la carga.</returns>
    public async Task CargarAsync() =>
        await EjecutarAsync(async () =>
        {
            var lista = await deudas.ListarAsync();

            Deudas.Clear();

            foreach (var deuda in lista)
            {
                Deudas.Add(Convertir(deuda));
            }

            if (Cuentas.Count == 0)
            {
                foreach (var cuenta in await finanzas.ListarCuentasAsync())
                {
                    Cuentas.Add(cuenta);
                }

                CuentaOrigen = Cuentas.FirstOrDefault();
            }

            if (Categorias.Count == 0)
            {
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

                CategoriaPago = Categorias.FirstOrDefault();
            }
        });

    /// <summary>Indica si ya se puede crear la deuda.</summary>
    /// <returns><c>true</c> si el formulario esta completo.</returns>
    private bool PuedeCrear() =>
        !string.IsNullOrWhiteSpace(NombreNueva)
        && Dinero.Leer(OriginalNueva) > 0m
        && Dinero.Leer(PendienteNueva) >= 0m;

    /// <summary>Crea la deuda y recarga la lista.</summary>
    /// <returns>Tarea que finaliza cuando termina.</returns>
    private async Task CrearAsync() =>
        await EjecutarAsync(async () =>
        {
            var original = Dinero.Leer(OriginalNueva);
            var pendiente = Dinero.Leer(PendienteNueva);

            // Se avisa aqui en vez de dejar que el servidor lo rechace: es un error de
            // tecleo muy facil y el mensaje llega antes.
            if (pendiente > original)
            {
                throw new InvalidOperationException(
                    "Lo que queda por pagar no puede superar lo que se pidió prestado.");
            }

            var cuota = Dinero.Leer(CuotaNueva);

            await deudas.CrearAsync(new SolicitudGuardarDeuda(
                NombreNueva.Trim(),
                TipoNueva,
                null,
                original,
                pendiente,
                null,
                null,
                null,
                cuota > 0m ? cuota : null,
                null,
                DateOnly.FromDateTime(DateTime.Today),
                null,
                null,
                null));

            NombreNueva = string.Empty;
            OriginalNueva = string.Empty;
            PendienteNueva = string.Empty;
            CuotaNueva = string.Empty;
            MostrandoFormulario = false;

            await CargarAsync();
        });

    /// <summary>Indica si ya se puede pagar.</summary>
    /// <returns><c>true</c> si el formulario esta completo.</returns>
    private bool PuedePagar() =>
        DeudaSeleccionada is not null
        && CuentaOrigen is not null
        && CategoriaPago is not null
        && Dinero.Leer(Capital) + Dinero.Leer(Interes) > 0m;

    /// <summary>Registra el pago y recarga la lista.</summary>
    /// <returns>Tarea que finaliza cuando termina.</returns>
    private async Task PagarAsync() =>
        await EjecutarAsync(async () =>
        {
            await deudas.PagarAsync(
                DeudaSeleccionada!.Deuda.Id,
                new SolicitudPagarDeuda(
                    CuentaOrigen!.Id,
                    CategoriaPago!.Id,
                    Dinero.Leer(Capital),
                    Dinero.Leer(Interes),

                    // Sin cargos: las comisiones son raras y pedirlas siempre seria una
                    // casilla de mas en el 95 % de los pagos.
                    0m,

                    DateOnly.FromDateTime(DateTime.Today),
                    null,
                    null));

            Capital = string.Empty;
            Interes = string.Empty;
            DeudaSeleccionada = null;

            await CargarAsync();
        });

    /// <summary>Convierte una deuda de la API en su fila de la lista.</summary>
    /// <param name="deuda">Deuda de la API.</param>
    /// <returns>La fila.</returns>
    private static DeudaEnLista Convertir(DeudaDetalle deuda)
    {
        var plazo = deuda.MesesEstimadosRestantes is { } meses
            ? $" · quedan unos {meses} meses"
            : string.Empty;

        return new DeudaEnLista(
            deuda,
            string.IsNullOrWhiteSpace(deuda.Acreedor)
                ? deuda.Nombre
                : $"{deuda.Nombre} · {deuda.Acreedor}",
            Dinero.Formatear(deuda.SaldoActual),
            Math.Min(1d, (double)deuda.PorcentajePagado / 100d),
            $"Pagado {Dinero.Formatear(deuda.MontoPagado)} de "
                + Dinero.Formatear(deuda.MontoOriginal) + plazo);
    }
}
