using Rumbo.Movil.ModelosVista;

namespace Rumbo.Movil.Vistas;

/// <summary>Menu de las pantallas que no son del dia a dia.</summary>
public partial class MasPagina : ContentPage
{
    /// <summary>Crea la pantalla.</summary>
    /// <param name="modeloVista">Logica de esta pantalla.</param>
    public MasPagina(MasModeloVista modeloVista)
    {
        InitializeComponent();

        BindingContext = modeloVista;
    }
}
