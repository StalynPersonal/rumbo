using Rumbo.Movil.ModelosVista;

namespace Rumbo.Movil.Vistas;

/// <summary>
/// Pantalla de inicio.
/// </summary>
public partial class PanelPagina : ContentPage
{
    private readonly PanelModeloVista _modeloVista;

    /// <summary>Crea la pantalla.</summary>
    /// <param name="modeloVista">Logica de esta pantalla.</param>
    public PanelPagina(PanelModeloVista modeloVista)
    {
        InitializeComponent();

        _modeloVista = modeloVista;
        BindingContext = modeloVista;
    }

    /// <summary>Se ejecuta cada vez que la pantalla aparece.</summary>
    /// <remarks>
    /// OnAppearing y no el constructor: el constructor se ejecuta UNA vez, y esto tiene que
    /// pasar cada vez que se vuelve a la pantalla. Si el panel se cargara solo al construir,
    /// al registrar un gasto y volver aqui seguirian viendose las cifras de antes.
    ///
    /// Es la unica excepcion a la regla de "el .xaml.cs va vacio", y existe porque el ciclo
    /// de vida de la pantalla es cosa de la pantalla, no del ModeloVista.
    /// </remarks>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _modeloVista.CargarAsync();
    }
}
