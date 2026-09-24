using Rumbo.Movil.ModelosVista;

namespace Rumbo.Movil.Vistas;

/// <summary>Pantalla de viajes y su viabilidad.</summary>
public partial class ViajesPagina : ContentPage
{
    private readonly ViajesModeloVista _modeloVista;

    /// <summary>Crea la pantalla.</summary>
    /// <param name="modeloVista">Logica de esta pantalla.</param>
    public ViajesPagina(ViajesModeloVista modeloVista)
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
