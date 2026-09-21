// Alias de ensamblado: Azure.Core y Azure.Identity publican ambos DefaultAzureCredential.
// Sin esto, el compilador no sabe cual usar.
extern alias identidadazure;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Rumbo.Infraestructura.Azure;

/// <summary>
/// Configura donde se guardan las claves de proteccion de datos.
/// </summary>
/// <remarks>
/// <para>
/// Data Protection cifra los tokens de «olvidé mi clave» y las contrasenas SMTP de cada
/// espacio. Las claves con las que cifra <b>no</b> son las contrasenas: son el material
/// criptografico de la propia aplicacion.
/// </para>
/// <para>
/// <b>Por defecto se guardan en el sistema de ficheros local.</b> En Azure App Service eso
/// significa dos desastres silenciosos: se pierden en cada reinicio, y no se comparten entre
/// instancias. El sintoma no es un error claro sino algo peor: enlaces de restablecimiento
/// que dejan de funcionar «sin motivo» y contrasenas SMTP guardadas que un dia no se pueden
/// descifrar y hay que volver a introducir.
/// </para>
/// </remarks>
public static class ProteccionDeDatos
{
    /// <summary>Registra la proteccion de datos con el destino que corresponda.</summary>
    /// <param name="servicios">Coleccion de servicios.</param>
    /// <param name="configuracion">Configuracion de la aplicacion.</param>
    /// <returns>La misma coleccion, para encadenar.</returns>
    public static IServiceCollection AgregarProteccionDeDatos(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        var opciones = configuracion.GetSection(OpcionesAzure.Seccion).Get<OpcionesAzure>()
            ?? new OpcionesAzure();

        var constructor = servicios.AddDataProtection()
            .SetApplicationName("Rumbo")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(opciones.DiasVidaDeClave));

        // El nombre de aplicacion se fija a mano y no se deja al valor por defecto, que en
        // App Service depende de la ruta fisica del sitio: si cambiara, la aplicacion
        // dejaria de reconocer sus propias claves.

        if (string.IsNullOrWhiteSpace(opciones.UriContenedorClaves))
        {
            // Sin configurar: sistema de ficheros local. Es lo correcto en desarrollo y en
            // las pruebas, donde no hay cuenta de Azure ni hace falta.
            return servicios;
        }

        // La identidad administrada de App Service. No hay ninguna credencial en la
        // configuracion: Azure concede el acceso al recurso, no una clave que se pueda
        // filtrar en un log o en un volcado de memoria.
        var credencial = new identidadazure::Azure.Identity.DefaultAzureCredential();

        constructor.PersistKeysToAzureBlobStorage(
            new Uri(
                $"{opciones.UriContenedorClaves!.TrimEnd('/')}/{opciones.NombreBlobClaves}"),
            credencial);

        if (!string.IsNullOrWhiteSpace(opciones.UriClaveCifrado))
        {
            // Sin esto, el fichero de claves quedaria en el blob EN CLARO: quien pudiera
            // leer el contenedor podria descifrar todo lo que protege Data Protection.
            constructor.ProtectKeysWithAzureKeyVault(
                new Uri(opciones.UriClaveCifrado!), credencial);
        }

        return servicios;
    }
}
