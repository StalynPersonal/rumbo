using Rumbo.Contratos.Autenticacion;

namespace Rumbo.Movil.Servicios;

/// <summary>
/// Entrar, salir y saber quien esta dentro.
/// </summary>
/// <remarks>
/// Se registra como Singleton: hay UNA sesion mientras la aplicacion viva, y todas las
/// pantallas consultan la misma.
/// </remarks>
/// <param name="api">Cliente para hablar con el servidor.</param>
/// <param name="almacen">Donde se guardan los tokens.</param>
public class ServicioSesion(ClienteApi api, AlmacenSesion almacen)
{
    /// <summary>Datos de la persona que ha entrado, o <c>null</c> si no hay nadie.</summary>
    public UsuarioAutenticado? Usuario { get; private set; }

    /// <summary>Espacio sobre el que se esta operando.</summary>
    public EspacioResumen? EspacioActivo { get; private set; }

    /// <summary>Espacios a los que puede cambiar sin volver a autenticarse.</summary>
    public IReadOnlyList<EspacioResumen> EspaciosDisponibles { get; private set; } = [];

    /// <summary>Inicia sesion con correo y contrasena.</summary>
    /// <param name="correo">Correo de acceso.</param>
    /// <param name="clave">Contrasena.</param>
    /// <returns>Tarea que finaliza cuando la sesion esta abierta.</returns>
    public async Task IniciarSesionAsync(string correo, string clave)
    {
        var respuesta = await api.EnviarAsync<SolicitudIniciarSesion, RespuestaAutenticacion>(
            "api/v1/autenticacion/iniciar-sesion",
            new SolicitudIniciarSesion(correo.Trim(), clave));

        await GuardarAsync(respuesta);
    }

    /// <summary>Crea una cuenta canjeando un codigo de invitacion.</summary>
    /// <param name="solicitud">Codigo, correo, nombre y contrasena.</param>
    /// <returns>Tarea que finaliza cuando la sesion esta abierta.</returns>
    /// <remarks>
    /// Rumbo no tiene registro publico: sin un codigo de invitacion valido no se entra.
    /// </remarks>
    public async Task RegistrarAsync(SolicitudRegistrar solicitud)
    {
        var respuesta = await api.EnviarAsync<SolicitudRegistrar, RespuestaAutenticacion>(
            "api/v1/autenticacion/registrar", solicitud);

        await GuardarAsync(respuesta);
    }

    /// <summary>Cambia el espacio sobre el que se opera.</summary>
    /// <param name="espacioId">Espacio al que se cambia.</param>
    /// <returns>Tarea que finaliza cuando el cambio esta hecho.</returns>
    /// <remarks>
    /// Devuelve un token NUEVO, porque el espacio activo viaja dentro del token firmado por
    /// el servidor. No es un ajuste del movil: si lo fuera, bastaria con cambiarlo en el
    /// telefono para leer las finanzas de otro hogar.
    /// </remarks>
    public async Task CambiarEspacioAsync(Guid espacioId)
    {
        var respuesta = await api.EnviarAsync<SolicitudCambiarEspacio, RespuestaAutenticacion>(
            "api/v1/autenticacion/cambiar-espacio",
            new SolicitudCambiarEspacio(espacioId));

        await GuardarAsync(respuesta);
    }

    /// <summary>Cierra la sesion.</summary>
    /// <returns>Tarea que finaliza cuando queda cerrada.</returns>
    public async Task CerrarSesionAsync()
    {
        var tokenRenovacion = await almacen.ObtenerTokenRenovacionAsync();

        if (!string.IsNullOrWhiteSpace(tokenRenovacion))
        {
            try
            {
                await api.EnviarSinRespuestaAsync(
                    "api/v1/autenticacion/cerrar-sesion",
                    new SolicitudCerrarSesion(tokenRenovacion));
            }
            catch
            {
                // Si el servidor no responde, se cierra igual en el movil. Dejar a alguien
                // atrapado dentro de la aplicacion porque no hay red seria absurdo; el token
                // caducara solo en el servidor.
            }
        }

        almacen.Limpiar();

        Usuario = null;
        EspacioActivo = null;
        EspaciosDisponibles = [];
    }

    /// <summary>Indica si hay una sesion guardada de una vez anterior.</summary>
    /// <returns><c>true</c> si la hay.</returns>
    public Task<bool> HaySesionGuardadaAsync() => almacen.HaySesionAsync();

    /// <summary>Guarda los datos de una respuesta de autenticacion.</summary>
    /// <param name="respuesta">Respuesta del servidor.</param>
    /// <returns>Tarea que finaliza cuando queda guardada.</returns>
    private async Task GuardarAsync(RespuestaAutenticacion respuesta)
    {
        await almacen.GuardarAsync(respuesta.TokenAcceso, respuesta.TokenRenovacion);

        Usuario = respuesta.Usuario;
        EspacioActivo = respuesta.EspacioActivo;
        EspaciosDisponibles = respuesta.EspaciosDisponibles;
    }
}
