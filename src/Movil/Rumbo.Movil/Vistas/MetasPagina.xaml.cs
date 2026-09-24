using Rumbo.Movil.ModelosVista;

namespace Rumbo.Movil.Vistas;

/// <summary>Pantalla de metas de ahorro.</summary>
public partial class MetasPagina : ContentPage
{
    private readonly MetasModeloVista _modeloVista;

    /// <summary>Crea la pantalla.</summary>
    /// <param name="modeloVista">Logica de esta pantalla.</param>
    public MetasPagina(MetasModeloVista modeloVista)
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
