using Rumbo.Movil.Comun;
using Rumbo.Movil.Servicios;
using Rumbo.Movil.Vistas;

namespace Rumbo.Movil.ModelosVista;

/// <summary>
/// La logica de la pantalla de acceso.
/// </summary>
/// <remarks>
/// ESTE FICHERO ES LA PLANTILLA. Esta comentado linea a linea a proposito; las demas
/// pantallas siguen exactamente este mismo patron y ya no repiten estas explicaciones.
///
/// Fijate en lo que NO hay aqui: ni un solo control de MAUI, ni Button, ni Entry, ni
/// Navigation.PushAsync. Esta clase no sabe que existe una pantalla. Por eso se podria
/// probar con una prueba automatica, y por eso el dia que cambie el diseno no hay que tocar
/// nada de esto.
/// </remarks>
/// <param name="sesion">Servicio que habla con el servidor.</param>
public class IniciarSesionModeloVista(ServicioSesion sesion) : ModeloVistaBase
{
    private string _correo = string.Empty;
    private string _clave = string.Empty;

    /// <summary>Correo que escribe la persona.</summary>
    /// <remarks>
    /// El patron de propiedad: campo privado, get que lo devuelve, set que llama a
    /// Establecer. Sin Establecer, la pantalla no se entera de los cambios.
    ///
    /// Ademas se refresca el comando: el boton de entrar esta gris mientras falten datos, y
    /// MAUI no adivina solo que ya se puede habilitar.
    /// </remarks>
    public string Correo
    {
        get => _correo;
        set
        {
            if (Establecer(ref _correo, value))
            {
                EntrarComando.Refrescar();
            }
        }
    }

    /// <summary>Contrasena que escribe la persona.</summary>
    public string Clave
    {
        get => _clave;
        set
        {
            if (Establecer(ref _clave, value))
            {
                EntrarComando.Refrescar();
            }
        }
    }

    /// <summary>Lo que ocurre al pulsar «Entrar».</summary>
    /// <remarks>
    /// Un comando son dos cosas: que hacer (EntrarAsync) y cuando se puede (PuedeEntrar).
    /// MAUI llama a lo segundo solo para habilitar o deshabilitar el boton.
    /// </remarks>
    public ComandoSimple EntrarComando => _entrarComando ??=
        new ComandoSimple(EntrarAsync, PuedeEntrar);

    private ComandoSimple? _entrarComando;

    /// <summary>Indica si ya se puede intentar entrar.</summary>
    /// <returns><c>true</c> si hay correo y contrasena.</returns>
    /// <remarks>
    /// Validacion minima, solo para no mandar peticiones vacias. Quien decide de verdad si
    /// las credenciales valen es el servidor: un movil esta en manos del usuario y cualquiera
    /// puede modificarlo.
    /// </remarks>
    private bool PuedeEntrar() =>
        !string.IsNullOrWhiteSpace(Correo) && !string.IsNullOrWhiteSpace(Clave);

    /// <summary>Intenta iniciar sesion y navega al panel si lo consigue.</summary>
    /// <returns>Tarea que finaliza cuando termina el intento.</returns>
    private async Task EntrarAsync()
    {
        // EjecutarAsync viene de ModeloVistaBase: enciende la ruedita, captura los errores
        // y la apaga pase lo que pase. Es el patron que se repite en toda la aplicacion.
        await EjecutarAsync(async () =>
        {
            await sesion.IniciarSesionAsync(Correo, Clave);

            // La contrasena se borra en cuanto deja de hacer falta, para que no se quede en
            // memoria mas tiempo del necesario.
            Clave = string.Empty;

            // Shell maneja la navegacion. La doble barra significa "ve ahi y olvida de
            // donde venias": asi el boton Atras del telefono no devuelve a la pantalla de
            // acceso despues de haber entrado.
            //
            // La ruta lleva las DOS partes, "principal/panel", porque el panel vive dentro
            // del TabBar que se llama "principal". Con solo "//panel", Shell no encuentra
            // la ruta y lanza una excepcion que no explica nada.
            await Shell.Current.GoToAsync(Rutas.Panel);
        });
    }
}
