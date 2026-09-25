using System.Collections.ObjectModel;

using Rumbo.Contratos.Cuentas;
using Rumbo.Movil.Comun;
using Rumbo.Movil.Servicios;

namespace Rumbo.Movil.ModelosVista;

/// <summary>
/// Una cuenta tal como se muestra en la lista.
/// </summary>
/// <param name="Nombre">Nombre de la cuenta.</param>
/// <param name="Detalle">Tipo e institucion, en una linea.</param>
/// <param name="Saldo">Saldo ya formateado.</param>
/// <remarks>
/// Es un DTO de PRESENTACION, distinto del CuentaResumen que llega de la API. Existe para
/// que el XAML no tenga que formatear nada: en XAML no se puede escribir codigo, y meter
/// convertidores para cada cifra complica mucho mas de lo que ahorra.
/// </remarks>
public record CuentaEnLista(string Nombre, string Detalle, string Saldo);

/// <summary>
/// La pantalla de cuentas.
/// </summary>
/// <param name="finanzas">Servicio de cuentas y movimientos.</param>
public class CuentasModeloVista(ServicioApiFinanzas finanzas) : ModeloVistaBase
{
    private string _total = "—";
    private bool _mostrandoFormulario;
    private string _nombreNueva = string.Empty;
    private string _saldoInicial = string.Empty;
    private string _tipoNueva = "Bancaria";

    /// <summary>
    /// Las cuentas que se muestran.
    /// </summary>
    /// <remarks>
    /// ObservableCollection y no List: cuando se añade o se quita un elemento, avisa sola a
    /// la pantalla. Con una List habria que avisar a mano cada vez, y olvidarlo provoca que
    /// la lista no se actualice sin dar ningun error.
    /// </remarks>
    public ObservableCollection<CuentaEnLista> Cuentas { get; } = [];

    /// <summary>Suma de todas las cuentas, ya formateada.</summary>
    public string Total
    {
        get => _total;
        private set => Establecer(ref _total, value);
    }

    /// <summary>Tipos de cuenta entre los que elegir.</summary>
    /// <remarks>
    /// Son los del enum del servidor, escritos igual. Si alguna vez dejan de coincidir, el
    /// servidor rechaza la cuenta con un mensaje claro en vez de guardar algo raro.
    /// </remarks>
    public IReadOnlyList<string> TiposDeCuenta { get; } =
        ["Bancaria", "Ahorro", "Efectivo", "TarjetaCredito", "Inversion", "Otra"];

    /// <summary>Indica si el formulario de alta esta abierto.</summary>
    public bool MostrandoFormulario
    {
        get => _mostrandoFormulario;
        private set => Establecer(ref _mostrandoFormulario, value);
    }

    /// <summary>Nombre de la cuenta que se va a crear.</summary>
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

    /// <summary>Saldo con el que arranca la cuenta.</summary>
    /// <remarks>
    /// Puede quedar vacio y se toma como cero: una cuenta de ahorro recien abierta empieza
    /// sin nada, y obligar a escribir un 0 seria un paso de mas sin motivo.
    /// </remarks>
    public string SaldoInicial
    {
        get => _saldoInicial;
        set => Establecer(ref _saldoInicial, value);
    }

    /// <summary>Tipo elegido.</summary>
    public string TipoNueva
    {
        get => _tipoNueva;
        set => Establecer(ref _tipoNueva, value);
    }

    /// <summary>Recarga las cuentas.</summary>
    public ComandoSimple CargarComando => _cargarComando ??= new ComandoSimple(CargarAsync);

    /// <summary>Abre o cierra el formulario de alta.</summary>
    public ComandoSimple AlternarFormularioComando => _alternarComando ??=
        new ComandoSimple(() =>
        {
            MostrandoFormulario = !MostrandoFormulario;

            return Task.CompletedTask;
        });

    /// <summary>Crea la cuenta del formulario.</summary>
    public ComandoSimple CrearComando => _crearComando ??=
        new ComandoSimple(CrearAsync, () => !string.IsNullOrWhiteSpace(NombreNueva));

    private ComandoSimple? _cargarComando;
    private ComandoSimple? _alternarComando;
    private ComandoSimple? _crearComando;

    /// <summary>Crea la cuenta y recarga la lista.</summary>
    /// <returns>Tarea que finaliza cuando termina.</returns>
    private async Task CrearAsync() =>
        await EjecutarAsync(async () =>
        {
            await finanzas.CrearCuentaAsync(new SolicitudCrearCuenta(
                NombreNueva.Trim(),
                TipoNueva,

                // La moneda base del hogar. Multi-moneda existe en el servidor, pero
                // ofrecerla aqui obligaria a explicar las tasas de cambio en una pantalla de
                // alta rapida; quien la necesite puede cambiarla despues.
                "DOP",

                Dinero.Leer(SaldoInicial),
                null,

                // Compartida por defecto: Rumbo es para un hogar, y lo habitual es que la
                // cuenta la vean los dos. Lo contrario sorprende mas.
                EsCompartida: true,

                null, null, null, null, null, null));

            NombreNueva = string.Empty;
            SaldoInicial = string.Empty;
            MostrandoFormulario = false;

            await CargarAsync();
        });

    /// <summary>Pide las cuentas al servidor.</summary>
    /// <returns>Tarea que finaliza cuando termina la carga.</returns>
    public async Task CargarAsync() =>
        await EjecutarAsync(async () =>
        {
            var cuentas = await finanzas.ListarCuentasAsync();

            // Se limpia y se vuelve a llenar en vez de crear una coleccion nueva: la
            // pantalla esta enlazada a ESTA instancia, y sustituirla romperia el enlace.
            Cuentas.Clear();

            foreach (var cuenta in cuentas.OrderBy(c => c.Orden).ThenBy(c => c.Nombre))
            {
                Cuentas.Add(new CuentaEnLista(
                    cuenta.Nombre,
                    Describir(cuenta),
                    Dinero.Formatear(cuenta.SaldoActual)));
            }

            // El total se suma en moneda BASE, no en la moneda de cada cuenta: sumar pesos
            // y dolares como si fueran lo mismo daria una cifra sin significado.
            Total = Dinero.Formatear(cuentas.Sum(c => c.SaldoEnMonedaBase));
        });

    /// <summary>Arma la linea de detalle de una cuenta.</summary>
    /// <param name="cuenta">Cuenta.</param>
    /// <returns>Tipo, institucion y moneda.</returns>
    private static string Describir(CuentaResumen cuenta)
    {
        var partes = new List<string> { cuenta.Tipo };

        if (!string.IsNullOrWhiteSpace(cuenta.Institucion))
        {
            partes.Add(cuenta.Institucion);
        }

        if (!string.IsNullOrWhiteSpace(cuenta.UltimosDigitos))
        {
            partes.Add($"•••• {cuenta.UltimosDigitos}");
        }

        partes.Add(cuenta.Moneda);

        return string.Join(" · ", partes);
    }
}
