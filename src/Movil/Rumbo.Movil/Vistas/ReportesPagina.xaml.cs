using Rumbo.Movil.ModelosVista;

namespace Rumbo.Movil.Vistas;

/// <summary>Pantalla de informes.</summary>
public partial class ReportesPagina : ContentPage
{
    private readonly ReportesModeloVista _modeloVista;

    /// <summary>Crea la pantalla.</summary>
    /// <param name="modeloVista">Logica de esta pantalla.</param>
    public ReportesPagina(ReportesModeloVista modeloVista)
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
