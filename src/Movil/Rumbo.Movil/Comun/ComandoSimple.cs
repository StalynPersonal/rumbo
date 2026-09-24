using System.Windows.Input;

namespace Rumbo.Movil.Comun;

/// <summary>
/// Lo que se ejecuta cuando se pulsa un boton.
/// </summary>
/// <remarks>
/// LEE ESTO SEGUNDO, despues de ModeloVistaBase.
///
/// En el XAML no hay ningun evento Click:
///
///     &lt;Button Text="Guardar" Command="{Binding GuardarComando}" /&gt;
///
/// El boton esta enlazado a un COMANDO, que es un objeto con dos cosas: que hacer, y si
/// ahora mismo se puede hacer. MAUI pregunta lo segundo para habilitar o deshabilitar el
/// boton solo, sin que la pantalla tenga que acordarse.
///
/// Esta clase implementa ICommand, que es la interfaz que MAUI espera.
/// </remarks>
/// <param name="accion">Lo que hace el comando.</param>
/// <param name="puedeEjecutarse">
/// Condicion extra para habilitarlo. Si se omite, se considera que siempre puede.
/// </param>
public class ComandoSimple(Func<Task> accion, Func<bool>? puedeEjecutarse = null) : ICommand
{
    private bool _enEjecucion;

    /// <summary>
    /// Evento con el que se le dice a MAUI que vuelva a preguntar si se puede ejecutar.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>Indica si el comando se puede ejecutar ahora mismo.</summary>
    /// <param name="parametro">No se usa en Rumbo.</param>
    /// <returns><c>true</c> si el boton debe estar habilitado.</returns>
    /// <remarks>
    /// MAUI llama a esto solo. Mientras devuelva false, el boton sale gris y no responde.
    /// </remarks>
    public bool CanExecute(object? parametro) =>
        !_enEjecucion && (puedeEjecutarse?.Invoke() ?? true);

    /// <summary>Ejecuta la accion del comando.</summary>
    /// <param name="parametro">No se usa en Rumbo.</param>
    /// <remarks>
    /// Es async void, que en C# es casi siempre un error porque una excepcion dentro de un
    /// async void no se puede capturar desde fuera y tumba la aplicacion. Aqui es
    /// obligatorio: la interfaz ICommand lo define asi. Por eso el cuerpo va envuelto en
    /// try/finally y las acciones capturan sus propias excepciones (lo hace EjecutarAsync
    /// de ModeloVistaBase).
    /// </remarks>
    public async void Execute(object? parametro)
    {
        if (!CanExecute(parametro))
        {
            return;
        }

        // El bloqueo evita el doble toque. En una pantalla de dinero esto NO es cosmetico:
        // dos toques rapidos en "Guardar" registrarian DOS movimientos, y la persona se
        // encontraria el gasto duplicado sin entender por que.
        _enEjecucion = true;
        Refrescar();

        try
        {
            await accion();
        }
        finally
        {
            // En finally: si la accion falla, el boton tiene que volver a habilitarse. Si
            // no, la pantalla se queda muerta tras el primer error y hay que cerrar la
            // aplicacion para recuperarla.
            _enEjecucion = false;
            Refrescar();
        }
    }

    /// <summary>
    /// Le dice a MAUI que vuelva a preguntar si el comando se puede ejecutar.
    /// </summary>
    /// <remarks>
    /// Hay que llamarlo a mano cuando cambia algo de lo que depende <c>puedeEjecutarse</c>.
    /// Por ejemplo, al escribir el correo en la pantalla de acceso: el boton de entrar
    /// estaba gris y ahora debe habilitarse, pero MAUI no lo adivina.
    /// </remarks>
    public void Refrescar() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
