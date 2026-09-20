namespace Rumbo.Contratos.Correo;

/// <summary>
/// Configuracion de correo tal como la devuelve la API.
/// </summary>
/// <param name="Host">Servidor SMTP.</param>
/// <param name="Puerto">Puerto de conexion.</param>
/// <param name="UsarSslDirecto">Si usa SSL directo en lugar de STARTTLS.</param>
/// <param name="Usuario">Usuario de autenticacion.</param>
/// <param name="ClaveConfigurada">
/// Indica si hay contrasena guardada. <b>La contrasena en si NUNCA se devuelve</b>: se guarda
/// cifrada y no existe ningun camino por el que la API la exponga, ni siquiera a quien la
/// puso.
/// </param>
/// <param name="RemitenteCorreo">Direccion que figura como remitente.</param>
/// <param name="RemitenteNombre">Nombre que figura como remitente.</param>
/// <param name="Activa">Si esta configuracion se usa para enviar.</param>
/// <param name="FechaUltimaPrueba">Cuando se comprobo la conexion por ultima vez.</param>
/// <param name="UltimaPruebaCorrecta">Si esa comprobacion funciono.</param>
/// <param name="UltimoErrorPrueba">Mensaje del error, si fallo.</param>
public record ConfiguracionCorreoDto(
    string? Host,
    int Puerto,
    bool UsarSslDirecto,
    string? Usuario,
    bool ClaveConfigurada,
    string? RemitenteCorreo,
    string? RemitenteNombre,
    bool Activa,
    DateTimeOffset? FechaUltimaPrueba,
    bool? UltimaPruebaCorrecta,
    string? UltimoErrorPrueba);

/// <summary>
/// Datos para guardar una configuracion de correo.
/// </summary>
/// <param name="Host">Servidor SMTP, por ejemplo <c>smtp.gmail.com</c>.</param>
/// <param name="Puerto">Puerto. Normalmente 587 (STARTTLS) o 465 (SSL directo).</param>
/// <param name="UsarSslDirecto">Si conecta con SSL desde el principio.</param>
/// <param name="Usuario">Usuario de autenticacion.</param>
/// <param name="Clave">
/// Contrasena. Se recomienda una <b>contrasena de aplicacion</b> dedicada y no la principal
/// de la cuenta: si se filtrara, se revoca sin tocar nada mas.
/// Dejar este campo en <c>null</c> CONSERVA la contrasena que ya estuviera guardada, para
/// poder cambiar el puerto o el remitente sin tener que volver a escribirla.
/// </param>
/// <param name="RemitenteCorreo">Direccion que figura como remitente.</param>
/// <param name="RemitenteNombre">Nombre que figura como remitente.</param>
/// <param name="Activa">Si debe usarse para enviar.</param>
public record SolicitudGuardarCorreo(
    string? Host,
    int Puerto,
    bool UsarSslDirecto,
    string? Usuario,
    string? Clave,
    string? RemitenteCorreo,
    string? RemitenteNombre,
    bool Activa);

/// <summary>Datos para probar la conexion con el servidor.</summary>
/// <param name="CorreoDestinoPrueba">
/// Direccion a la que enviar el mensaje de prueba. Si es <c>null</c>, solo se comprueba que
/// la conexion y la autenticacion funcionen, sin enviar nada.
/// </param>
public record SolicitudProbarCorreo(string? CorreoDestinoPrueba);

/// <summary>Resultado de probar la conexion.</summary>
/// <param name="Correcta">Si la conexion y la autenticacion funcionaron.</param>
/// <param name="Mensaje">Explicacion del resultado, en español.</param>
/// <param name="CorreoEnviado">Si ademas se envio el mensaje de prueba.</param>
public record ResultadoPruebaCorreo(bool Correcta, string Mensaje, bool CorreoEnviado);
