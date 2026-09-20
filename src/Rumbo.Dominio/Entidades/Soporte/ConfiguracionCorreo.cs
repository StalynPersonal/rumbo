using Rumbo.Dominio.Comun;

namespace Rumbo.Dominio.Entidades.Soporte;

/// <summary>
/// Datos de conexion a un servidor SMTP, comunes a la plataforma y a cada espacio.
/// </summary>
/// <remarks>
/// <para>
/// <b>La contrasena se guarda CIFRADA</b> en <see cref="ClaveCifrada"/>, nunca en claro.
/// No es un dato del sistema: es la contrasena del correo real de una persona. En texto
/// plano, cualquiera con acceso de lectura a la base de datos se llevaria una cuenta de
/// correo ajena, no solo informacion financiera.
/// </para>
/// <para>
/// La API jamas devuelve este valor. Solo informa de si hay contrasena guardada.
/// </para>
/// </remarks>
public abstract class ConfiguracionCorreoBase : EntidadAuditable
{
    /// <summary>Servidor SMTP, por ejemplo <c>smtp.gmail.com</c>.</summary>
    public string? Host { get; set; }

    /// <summary>Puerto. Normalmente 587 con STARTTLS o 465 con SSL directo.</summary>
    public int Puerto { get; set; } = 587;

    /// <summary>Indica si se conecta con SSL directo en lugar de STARTTLS.</summary>
    public bool UsarSslDirecto { get; set; }

    /// <summary>Usuario de autenticacion, habitualmente la propia direccion de correo.</summary>
    public string? Usuario { get; set; }

    /// <summary>
    /// Contrasena cifrada con Data Protection. Nunca se almacena ni se devuelve en claro.
    /// </summary>
    public string? ClaveCifrada { get; set; }

    /// <summary>Direccion que figura como remitente.</summary>
    public string? RemitenteCorreo { get; set; }

    /// <summary>Nombre que figura como remitente.</summary>
    public string? RemitenteNombre { get; set; }

    /// <summary>
    /// Indica si esta configuracion debe usarse. Permite desactivarla sin borrar los datos.
    /// </summary>
    public bool Activa { get; set; }

    /// <summary>Instante en que se comprobo por ultima vez que la conexion funcionaba.</summary>
    public DateTimeOffset? FechaUltimaPrueba { get; set; }

    /// <summary>Resultado de la ultima prueba, para poder mostrarlo sin repetirla.</summary>
    public bool? UltimaPruebaCorrecta { get; set; }

    /// <summary>Mensaje de error de la ultima prueba, si fallo.</summary>
    public string? UltimoErrorPrueba { get; set; }

    /// <summary>Indica si hay datos suficientes para intentar un envio.</summary>
    /// <returns><c>true</c> si esta activa y tiene servidor y remitente.</returns>
    public bool EstaUtilizable() =>
        Activa
        && !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(RemitenteCorreo);
}

/// <summary>
/// Servidor SMTP de la plataforma.
/// </summary>
/// <remarks>
/// <para>
/// Es una tabla GLOBAL con una unica fila. La usa el administrador de plataforma.
/// </para>
/// <para>
/// <b>Por que hace falta uno de plataforma ademas del de cada espacio.</b> Hay dos correos
/// que no tienen espacio del que sacar credenciales: la invitacion a un futuro propietario,
/// que se envia ANTES de que su espacio exista, y el restablecimiento de contrasena, que
/// pertenece a la persona y no a un hogar (alguien puede estar en varios).
/// </para>
/// </remarks>
public class ConfiguracionCorreoPlataforma : ConfiguracionCorreoBase
{
    /// <summary>
    /// Direccion base de la aplicacion, para construir los enlaces de los correos.
    /// </summary>
    public string UrlBase { get; set; } = "https://localhost:7299";
}

/// <summary>
/// Servidor SMTP propio de un espacio.
/// </summary>
/// <remarks>
/// <para>
/// Lo configura el propietario del hogar con su propia cuenta de correo, de modo que las
/// invitaciones a su pareja o a su familia salgan desde su direccion y no desde una
/// genérica.
/// </para>
/// <para>
/// Si un espacio no tiene configuracion utilizable, el envio recae en la de plataforma. Es
/// preferible que el correo salga desde la direccion general a que no salga.
/// </para>
/// </remarks>
public class ConfiguracionCorreoEspacio : ConfiguracionCorreoBase, IEntidadDeEspacio
{
    /// <inheritdoc />
    public Guid EspacioId { get; set; }
}
