using System.Collections.ObjectModel;
using System.Globalization;

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
    private static readonly CultureInfo Cultura = new("es-DO");

    private string _total = "—";

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

    /// <summary>Recarga las cuentas.</summary>
    public ComandoSimple CargarComando => _cargarComando ??= new ComandoSimple(CargarAsync);

    private ComandoSimple? _cargarComando;

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
                    cuenta.SaldoActual.ToString("C2", Cultura)));
            }

            // El total se suma en moneda BASE, no en la moneda de cada cuenta: sumar pesos
            // y dolares como si fueran lo mismo daria una cifra sin significado.
            Total = cuentas.Sum(c => c.SaldoEnMonedaBase).ToString("C2", Cultura);
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
