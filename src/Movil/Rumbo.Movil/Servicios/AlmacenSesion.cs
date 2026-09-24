namespace Rumbo.Movil.Servicios;

/// <summary>
/// Guarda y recupera los tokens de la sesion.
/// </summary>
/// <remarks>
/// Usa SecureStorage, que en Android se apoya en el almacen de claves del sistema.
///
/// NUNCA se usa Preferences para esto. Preferences guarda texto plano, y cualquier
/// aplicacion con acceso al almacenamiento podria leer el token. Es la diferencia entre
/// guardar la llave en una caja fuerte o debajo del felpudo.
/// </remarks>
public class AlmacenSesion
{
    private const string ClaveTokenAcceso = "rumbo_token_acceso";
    private const string ClaveTokenRenovacion = "rumbo_token_renovacion";

    /// <summary>Guarda los dos tokens tras iniciar sesion o renovar.</summary>
    /// <param name="tokenAcceso">Token de acceso, que dura 15 minutos.</param>
    /// <param name="tokenRenovacion">Token de renovacion, que dura 30 dias.</param>
    /// <returns>Tarea que finaliza cuando quedan guardados.</returns>
    public async Task GuardarAsync(string tokenAcceso, string tokenRenovacion)
    {
        await SecureStorage.SetAsync(ClaveTokenAcceso, tokenAcceso);
        await SecureStorage.SetAsync(ClaveTokenRenovacion, tokenRenovacion);
    }

    /// <summary>Devuelve el token de acceso guardado.</summary>
    /// <returns>El token, o <c>null</c> si no hay sesion.</returns>
    public async Task<string?> ObtenerTokenAccesoAsync()
    {
        try
        {
            return await SecureStorage.GetAsync(ClaveTokenAcceso);
        }
        catch
        {
            // SecureStorage puede fallar si el almacen de claves del dispositivo esta en
            // mal estado, cosa que pasa de vez en cuando tras restaurar una copia de
            // seguridad. Tratarlo como "no hay sesion" hace que la persona vuelva a la
            // pantalla de acceso, que es molesto pero funciona. Dejar que la excepcion suba
            // haria que la aplicacion no arrancara nunca mas.
            return null;
        }
    }

    /// <summary>Devuelve el token de renovacion guardado.</summary>
    /// <returns>El token, o <c>null</c> si no hay sesion.</returns>
    public async Task<string?> ObtenerTokenRenovacionAsync()
    {
        try
        {
            return await SecureStorage.GetAsync(ClaveTokenRenovacion);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Borra la sesion guardada.</summary>
    public void Limpiar()
    {
        SecureStorage.Remove(ClaveTokenAcceso);
        SecureStorage.Remove(ClaveTokenRenovacion);
    }

    /// <summary>Indica si hay una sesion guardada.</summary>
    /// <returns><c>true</c> si hay token de renovacion.</returns>
    /// <remarks>
    /// Se mira el de RENOVACION y no el de acceso: el de acceso caduca a los 15 minutos, asi
    /// que casi siempre estara vencido al abrir la aplicacion. El de renovacion dura 30
    /// dias y es el que de verdad dice si la persona sigue dentro.
    /// </remarks>
    public async Task<bool> HaySesionAsync() =>
        !string.IsNullOrWhiteSpace(await ObtenerTokenRenovacionAsync());
}
