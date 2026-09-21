extern alias identidadazure;

using identidadazure::Azure.Identity;

using Rumbo.Infraestructura.Azure;

namespace Rumbo.Api.Extensiones;

/// <summary>
/// Conecta la aplicacion con los servicios de Azure cuando estan configurados.
/// </summary>
/// <remarks>
/// Todo lo de aqui es <b>opcional</b>. Si no hay configuracion de Azure, la aplicacion
/// arranca igual y funciona en local. Esa es la razon de que sea opcional y no obligatorio:
/// poder desarrollar y ejecutar las pruebas sin una cuenta de Azure ni conexion a internet.
/// </remarks>
public static class ExtensionesAzure
{
    /// <summary>
    /// Anade Key Vault como fuente de configuracion, si esta configurado.
    /// </summary>
    /// <param name="constructor">Constructor de la aplicacion.</param>
    /// <remarks>
    /// <para>
    /// Se anade como <b>ultima</b> fuente para que gane a <c>appsettings.json</c>: los
    /// secretos de produccion deben tener la ultima palabra.
    /// </para>
    /// <para>
    /// Los nombres de secreto usan dos guiones donde la configuracion usa dos puntos, porque
    /// Key Vault no admite dos puntos: <c>Jwt--ClaveFirma</c> se lee como
    /// <c>Jwt:ClaveFirma</c>.
    /// </para>
    /// </remarks>
    public static void AgregarKeyVault(this WebApplicationBuilder constructor)
    {
        var uri = constructor.Configuration[$"{OpcionesAzure.Seccion}:UriKeyVault"];

        if (string.IsNullOrWhiteSpace(uri))
        {
            return;
        }

        // Identidad administrada: no hay ninguna credencial en la configuracion. Es la
        // diferencia entre «los secretos estan protegidos» y «el secreto que protege los
        // secretos esta en un fichero».
        constructor.Configuration.AddAzureKeyVault(
            new Uri(uri), new DefaultAzureCredential());
    }

    /// <summary>
    /// Anade Application Insights, si hay cadena de conexion.
    /// </summary>
    /// <param name="constructor">Constructor de la aplicacion.</param>
    /// <remarks>
    /// La telemetria <b>no</b> debe llevar datos financieros ni personales. La configuracion
    /// por defecto recoge peticiones, dependencias y excepciones, que es lo que hace falta
    /// para diagnosticar; los importes y los nombres nunca se envian porque el codigo no los
    /// registra (ver <c>docs/SEGURIDAD.md</c>).
    /// </remarks>
    public static void AgregarTelemetria(this WebApplicationBuilder constructor)
    {
        var cadena = constructor.Configuration["ApplicationInsights:ConnectionString"];

        if (string.IsNullOrWhiteSpace(cadena))
        {
            return;
        }

        constructor.Services.AddApplicationInsightsTelemetry(opciones =>
            opciones.ConnectionString = cadena);
    }
}
