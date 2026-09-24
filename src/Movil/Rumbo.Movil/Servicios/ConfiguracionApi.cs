namespace Rumbo.Movil.Servicios;

/// <summary>
/// Donde esta el servidor de Rumbo.
/// </summary>
/// <remarks>
/// La direccion vive AQUI y solo aqui. Repartida por los servicios, cambiar de entorno
/// significaria buscar y reemplazar en quince ficheros y olvidarse de uno.
/// </remarks>
public static class ConfiguracionApi
{
    /// <summary>Direccion base de la API.</summary>
    /// <remarks>
    /// EL ERROR QUE TE VAS A ENCONTRAR SEGURO: el movil NO puede llegar a "localhost".
    /// Para el telefono, localhost es el propio telefono, no tu PC.
    ///
    /// Opciones, de mas simple a menos:
    ///
    ///   - Emulador de Android: la direccion 10.0.2.2 apunta al PC anfitrion.
    ///   - Movil real en la misma wifi: pon la IP de tu PC (ipconfig, algo como
    ///     192.168.1.40). Tendras que permitirlo en el cortafuegos de Windows.
    ///   - Ya desplegado en Azure: la direccion de produccion funciona sin mas.
    ///
    /// En DEBUG se usa HTTP y no HTTPS a proposito: el certificado de desarrollo del PC no
    /// vale para el telefono, y pelearse con eso mientras se aprende no aporta nada. En
    /// RELEASE es HTTPS obligatorio, que es lo unico aceptable con datos financieros.
    /// </remarks>
#if DEBUG
    public const string DireccionBase = "http://10.0.2.2:5114/";
#else
    public const string DireccionBase = "https://rumbo-api.azurewebsites.net/";
#endif

    /// <summary>Version de esta aplicacion, tal como la ve el servidor.</summary>
    /// <remarks>
    /// Se manda a /api/v1/app/version para saber si hay una version nueva. El APK se
    /// instala a mano, sin tienda, asi que nadie avisa si no avisa la propia aplicacion.
    /// </remarks>
    public const string VersionInstalada = "1.0.0";
}
