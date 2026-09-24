using Rumbo.Movil.ModelosVista;

namespace Rumbo.Movil.Vistas;

/// <summary>
/// Pantalla de acceso.
/// </summary>
/// <remarks>
/// Fijate en lo POCO que hay aqui. En Rumbo, el fichero .xaml.cs de una pantalla SIEMPRE
/// tiene exactamente estas dos lineas: InitializeComponent y la asignacion del
/// BindingContext. Si alguna vez ves logica en un fichero como este, es que alguien se salto
/// el patron.
///
/// La linea del BindingContext es la que conecta el XAML con el C#: a partir de ella, todo
/// {Binding X} del XAML busca la propiedad X en el ModeloVista.
///
/// El ModeloVista llega por el constructor. No se crea aqui con "new": de eso se encarga el
/// contenedor de dependencias que se configura en MauiProgram.cs.
/// </remarks>
public partial class IniciarSesionPagina : ContentPage
{
    /// <summary>Crea la pantalla.</summary>
    /// <param name="modeloVista">Logica de esta pantalla.</param>
    public IniciarSesionPagina(IniciarSesionModeloVista modeloVista)
    {
        InitializeComponent();

        BindingContext = modeloVista;
    }
}
