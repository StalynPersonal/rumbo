using Rumbo.Contratos.Correo;

namespace Rumbo.Aplicacion.Contratos;

/// <summary>
/// Gestiona los servidores SMTP: el de la plataforma y el de cada espacio.
/// </summary>
/// <remarks>
/// <para>
/// Hay dos niveles a proposito. El de <b>plataforma</b> envia lo que no pertenece a ningun
/// hogar: la invitacion a un futuro propietario (su espacio todavia no existe) y el
/// restablecimiento de contrasena (pertenece a la persona, que puede estar en varios
/// espacios). El de <b>espacio</b> envia lo del hogar, para que las invitaciones a la pareja
/// o a la familia salgan desde el correo del propietario y no desde una direccion generica.
/// </para>
/// <para>
/// Si un espacio no tiene configuracion utilizable, se recurre a la de plataforma: es
/// preferible que el correo salga desde la direccion general a que no salga.
/// </para>
/// </remarks>
public interface IServicioConfiguracionCorreo
{
    /// <summary>Devuelve la configuracion de la plataforma.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La configuracion, sin la contrasena.</returns>
    Task<ConfiguracionCorreoDto> ObtenerPlataformaAsync(CancellationToken cancelacion = default);

    /// <summary>Guarda la configuracion de la plataforma.</summary>
    /// <param name="solicitud">Datos del servidor.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La configuracion guardada, sin la contrasena.</returns>
    Task<ConfiguracionCorreoDto> GuardarPlataformaAsync(
        SolicitudGuardarCorreo solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Prueba la conexion con el servidor de la plataforma.</summary>
    /// <param name="solicitud">Direccion de prueba, opcional.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El resultado de la prueba.</returns>
    Task<ResultadoPruebaCorreo> ProbarPlataformaAsync(
        SolicitudProbarCorreo solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Devuelve la configuracion de un espacio.</summary>
    /// <param name="espacioId">Espacio activo.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La configuracion, sin la contrasena.</returns>
    Task<ConfiguracionCorreoDto> ObtenerDelEspacioAsync(
        Guid espacioId,
        CancellationToken cancelacion = default);

    /// <summary>Guarda la configuracion de un espacio.</summary>
    /// <param name="espacioId">Espacio activo.</param>
    /// <param name="solicitud">Datos del servidor.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La configuracion guardada, sin la contrasena.</returns>
    Task<ConfiguracionCorreoDto> GuardarDelEspacioAsync(
        Guid espacioId,
        SolicitudGuardarCorreo solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Prueba la conexion con el servidor de un espacio.</summary>
    /// <param name="espacioId">Espacio activo.</param>
    /// <param name="solicitud">Direccion de prueba, opcional.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El resultado de la prueba.</returns>
    Task<ResultadoPruebaCorreo> ProbarDelEspacioAsync(
        Guid espacioId,
        SolicitudProbarCorreo solicitud,
        CancellationToken cancelacion = default);
}
