namespace Rumbo.Infraestructura.Azure;

/// <summary>
/// Parametros de los recursos de Azure que usa la aplicacion.
/// </summary>
/// <remarks>
/// <para>
/// Todos son opcionales. Si no se configuran, la aplicacion funciona igual en local: las
/// claves de proteccion de datos se guardan en el sistema de ficheros y no hay telemetria.
/// Eso permite desarrollar y ejecutar las pruebas sin una cuenta de Azure.
/// </para>
/// <para>
/// <b>Ninguno de estos valores es un secreto.</b> Son direcciones de recursos; el acceso lo
/// concede la identidad administrada de App Service, no una cadena de conexion con clave.
/// </para>
/// </remarks>
public class OpcionesAzure
{
    /// <summary>Nombre de la seccion en la configuracion.</summary>
    public const string Seccion = "Azure";

    /// <summary>
    /// URI del contenedor de Blob Storage donde se guardan las claves de proteccion.
    /// </summary>
    /// <remarks>
    /// Por ejemplo <c>https://rumboclaves.blob.core.windows.net/dataprotection</c>. Sin esto,
    /// en App Service las claves se pierden en cada reinicio y no se comparten entre
    /// instancias: los enlaces de «olvidé mi clave» dejarian de funcionar y las contrasenas
    /// SMTP guardadas no se podrian descifrar.
    /// </remarks>
    public string? UriContenedorClaves { get; set; }

    /// <summary>Nombre del fichero de claves dentro del contenedor.</summary>
    public string NombreBlobClaves { get; set; } = "claves-rumbo.xml";

    /// <summary>
    /// URI de la clave de Key Vault con la que se cifra el propio fichero de claves.
    /// </summary>
    /// <remarks>
    /// Sin esto, el fichero de claves queda en el blob <b>en claro</b>: quien pudiera leer
    /// el contenedor podria descifrar todo lo que protege Data Protection. Con esto, el
    /// fichero va cifrado y hace falta ademas permiso sobre la clave de Key Vault.
    /// </remarks>
    public string? UriClaveCifrado { get; set; }

    /// <summary>URI de Key Vault del que se leen los secretos de configuracion.</summary>
    /// <remarks>
    /// Por ejemplo <c>https://rumbo-secretos.vault.azure.net/</c>.
    /// </remarks>
    public string? UriKeyVault { get; set; }

    /// <summary>Dias que se conserva cada clave de proteccion antes de rotar.</summary>
    /// <remarks>
    /// Noventa dias es el valor por defecto de Data Protection. Las claves antiguas no se
    /// borran: siguen ahi para poder descifrar lo que se cifro con ellas.
    /// </remarks>
    public int DiasVidaDeClave { get; set; } = 90;
}
