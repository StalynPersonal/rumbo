using Rumbo.Movil.ModelosVista;

namespace Rumbo.Movil.Vistas;

/// <summary>Pantalla de ajustes.</summary>
public partial class AjustesPagina : ContentPage
{
    private readonly AjustesModeloVista _modeloVista;

    /// <summary>Crea la pantalla.</summary>
    /// <param name="modeloVista">Logica de esta pantalla.</param>
    public AjustesPagina(AjustesModeloVista modeloVista)
    {
        InitializeComponent();

        _modeloVista = modeloVista;
        BindingContext = modeloVista;
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _modeloVista.CargarAsync();
    }
}
