namespace Rumbo.Dominio.Excepciones;

/// <summary>
/// Las credenciales no son validas.
/// </summary>
/// <remarks>
/// El mensaje es deliberadamente vago y el MISMO tanto si el correo no existe como si la
/// contrasena es incorrecta. Distinguir ambos casos convertiria el formulario de acceso en
/// una herramienta para averiguar quien tiene cuenta en Rumbo.
/// </remarks>
public class ExcepcionCredencialesInvalidas()
    : ExcepcionDominio("El correo o la contraseña no son correctos.");

/// <summary>La cuenta esta bloqueada temporalmente por intentos fallidos.</summary>
/// <param name="hasta">Instante en que se desbloquea.</param>
public class ExcepcionCuentaBloqueada(DateTimeOffset hasta)
    : ExcepcionDominio($"La cuenta está bloqueada temporalmente hasta las "
        + $"{hasta.ToLocalTime():HH:mm} por varios intentos fallidos.")
{
    /// <summary>Instante en que la cuenta vuelve a estar disponible.</summary>
    public DateTimeOffset Hasta { get; } = hasta;
}

/// <summary>El codigo de invitacion no sirve.</summary>
/// <param name="motivo">Explicacion concreta para la persona.</param>
public class ExcepcionInvitacionInvalida(string motivo) : ExcepcionDominio(motivo);

/// <summary>El token de renovacion no sirve.</summary>
/// <param name="motivo">Explicacion del problema.</param>
public class ExcepcionTokenInvalido(string motivo) : ExcepcionDominio(motivo);

/// <summary>
/// La persona no tiene una membresia activa en el espacio que solicita.
/// </summary>
public class ExcepcionSinAccesoAlEspacio()
    : ExcepcionDominio("No tienes acceso a ese espacio.");

/// <summary>El recurso solicitado no existe o no pertenece al espacio activo.</summary>
/// <param name="recurso">Nombre del recurso, para el mensaje.</param>
/// <remarks>
/// Se usa el MISMO error para "no existe" y para "existe pero es de otro espacio". Responder
/// 403 en el segundo caso confirmaria que el registro existe, que ya es informacion que no
/// corresponde dar.
/// </remarks>
public class ExcepcionNoEncontrado(string recurso)
    : ExcepcionDominio($"No se encontró {recurso}.");
