using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Rumbo.Movil.Comun;

/// <summary>
/// Clase base de la que hereda TODO modelo de vista de Rumbo.
/// </summary>
/// <remarks>
/// LEE ESTO PRIMERO. Es la pieza que explica por que el resto del codigo movil se escribe
/// como se escribe.
///
/// El problema que resuelve:
///
///     public string Nombre { get; set; }     // Esto NO funciona en una pantalla.
///
/// Si asignas Nombre = "Hogar Garcia", el valor cambia en memoria... y la pantalla sigue
/// mostrando lo de antes. Nadie le aviso de que algo cambio.
///
/// MAUI necesita que el objeto GRITE "esta propiedad cambio". Eso se hace disparando el
/// evento PropertyChanged, que viene de la interfaz INotifyPropertyChanged. Como escribir
/// eso en cada propiedad de cada pantalla seria insoportable, se escribe UNA vez aqui.
///
/// A partir de ahora, cada propiedad de una pantalla se escribe asi:
///
///     private string _nombre = string.Empty;
///
///     public string Nombre
///     {
///         get =&gt; _nombre;
///         set =&gt; Establecer(ref _nombre, value);
///     }
///
/// Es mas largo que { get; set; }, si. Pero VES lo que pasa, y eso es justo el objetivo.
/// </remarks>
public class ModeloVistaBase : INotifyPropertyChanged
{
    private bool _estaCargando;
    private string? _mensajeError;

    /// <summary>
    /// El evento al que MAUI se suscribe para enterarse de los cambios.
    /// </summary>
    /// <remarks>
    /// No hay que dispararlo a mano: de eso se encarga <see cref="Establecer"/>.
    /// </remarks>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Indica si hay una operacion en curso. La pantalla lo usa para mostrar la ruedita.
    /// </summary>
    public bool EstaCargando
    {
        get => _estaCargando;
        set => Establecer(ref _estaCargando, value);
    }

    /// <summary>
    /// Mensaje de error visible para la persona, o <c>null</c> si no hay ninguno.
    /// </summary>
    /// <remarks>
    /// Todas las pantallas lo muestran. Un error que no se ve convierte un fallo en una
    /// pantalla en blanco sin explicacion, que es mucho peor que el fallo.
    /// </remarks>
    public string? MensajeError
    {
        get => _mensajeError;
        set
        {
            Establecer(ref _mensajeError, value);

            // Se avisa tambien de HayError, que es una propiedad calculada a partir de esta.
            // Sin esta linea, el aviso rojo de la pantalla no aparece ni desaparece nunca.
            Avisar(nameof(HayError));
        }
    }

    /// <summary>Indica si hay un error que mostrar.</summary>
    /// <remarks>
    /// Existe porque en XAML no se puede escribir una condicion: es mas simple exponer un
    /// booleano y enlazar IsVisible a el que enredarse con convertidores.
    /// </remarks>
    public bool HayError => !string.IsNullOrWhiteSpace(MensajeError);

    /// <summary>
    /// Asigna un valor a un campo y avisa a la pantalla, pero SOLO si de verdad cambio.
    /// </summary>
    /// <typeparam name="T">Tipo del valor.</typeparam>
    /// <param name="campo">Campo privado donde se guarda, pasado por referencia.</param>
    /// <param name="valor">Valor nuevo.</param>
    /// <param name="nombrePropiedad">
    /// Se rellena SOLO. El atributo CallerMemberName es magia del compilador de C#, no de
    /// MAUI: pone aqui el nombre de la propiedad desde la que se llamo. Por eso no hay que
    /// escribir Establecer(ref _nombre, value, "Nombre").
    /// </param>
    /// <returns><c>true</c> si el valor cambio.</returns>
    protected bool Establecer<T>(
        ref T campo,
        T valor,
        [CallerMemberName] string? nombrePropiedad = null)
    {
        // Si el valor es el mismo, no se avisa. Avisar de cambios que no existen hace que
        // la pantalla se redibuje sin motivo, y en una lista larga eso se nota.
        if (EqualityComparer<T>.Default.Equals(campo, valor))
        {
            return false;
        }

        campo = valor;
        Avisar(nombrePropiedad);

        return true;
    }

    /// <summary>Avisa a la pantalla de que una propiedad cambio.</summary>
    /// <param name="nombrePropiedad">Nombre de la propiedad.</param>
    /// <remarks>
    /// Solo hace falta llamarlo a mano para propiedades CALCULADAS, que no tienen campo
    /// propio. Por ejemplo <see cref="HayError"/>, que depende de MensajeError.
    /// </remarks>
    protected void Avisar([CallerMemberName] string? nombrePropiedad = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombrePropiedad));

    /// <summary>
    /// Ejecuta una operacion mostrando la ruedita y capturando los errores.
    /// </summary>
    /// <param name="operacion">Lo que hay que hacer.</param>
    /// <returns>Tarea que finaliza cuando termina la operacion.</returns>
    /// <remarks>
    /// Este metodo existe porque el patron "activa la ruedita, intenta, captura, apaga la
    /// ruedita" se repite en TODAS las pantallas. Escrito una vez, no se puede olvidar el
    /// finally en ninguna.
    /// </remarks>
    protected async Task EjecutarAsync(Func<Task> operacion)
    {
        EstaCargando = true;
        MensajeError = null;

        try
        {
            await operacion();
        }
        catch (Exception excepcion)
        {
            // La persona tiene que ENTERARSE. Un catch vacio es la peor linea de codigo que
            // se puede escribir en una aplicacion que maneja dinero.
            MensajeError = excepcion.Message;
        }
        finally
        {
            // En finally: si la operacion falla, la ruedita tiene que apagarse igual. Si no,
            // la pantalla se queda girando para siempre tras el primer error.
            EstaCargando = false;
        }
    }
}
