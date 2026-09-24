using Rumbo.Movil.Servicios;

namespace Rumbo.Movil;

/// <summary>
/// Contenedor de las pantallas y su navegacion.
/// </summary>
/// <param name="sesion">Sesion activa, para saber si hay que pedir acceso.</param>
public partial class AppShell : Shell
{
    private readonly ServicioSesion _sesion;

    /// <summary>Crea el Shell.</summary>
    /// <param name="sesion">Sesion activa.</param>
    public AppShell(ServicioSesion sesion)
    {
        InitializeComponent();

        _sesion = sesion;
    }

    /// <summary>Decide la primera pantalla segun haya sesion guardada o no.</summary>
    /// <remarks>
    /// Se hace aqui y no en el constructor porque navegar exige que el Shell ya exista. En
    /// el constructor, GoToAsync falla con un error que no explica nada.
    ///
    /// Si hay sesion guardada se va directo al panel: el token de renovacion dura 30 dias, y
    /// pedir la contrasena cada vez que se abre la aplicacion acabaria en que la persona
    /// elige una corta para teclearla rapido.
    /// </remarks>
    protected override async void OnNavigated(ShellNavigatedEventArgs argumentos)
    {
        base.OnNavigated(argumentos);

        // Solo la primera vez. Sin esta guarda, cada navegacion volveria a comprobarlo y la
        // aplicacion se quedaria dando vueltas entre pantallas.
        if (_yaSeDecidio)
        {
            return;
        }

        _yaSeDecidio = true;

        if (await _sesion.HaySesionGuardadaAsync())
        {
            await GoToAsync("//panel");
        }
    }

    private bool _yaSeDecidio;
}
