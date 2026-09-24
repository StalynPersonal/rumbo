namespace Rumbo.Movil;

/// <summary>
/// La aplicacion.
/// </summary>
/// <param name="shell">Contenedor de las pantallas.</param>
public partial class App(AppShell shell) : Application
{
    /// <summary>Crea la ventana principal.</summary>
    /// <param name="estado">Estado de activacion.</param>
    /// <returns>La ventana.</returns>
    /// <remarks>
    /// El AppShell llega por el constructor, no se crea aqui con "new": necesita la sesion
    /// para decidir si la primera pantalla es la de acceso o el panel, y eso lo resuelve el
    /// contenedor de dependencias.
    /// </remarks>
    protected override Window CreateWindow(IActivationState? estado) => new(shell);
}
