using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Rumbo.Aplicacion.Contratos;
using Rumbo.Dominio.Entidades.Soporte;
using Rumbo.Infraestructura.Persistencia;

namespace Rumbo.Infraestructura.Correo;

/// <summary>
/// Decide con que servidor SMTP se envia cada correo.
/// </summary>
/// <remarks>
/// <para>El orden de preferencia es deliberado:</para>
/// <list type="number">
/// <item><description>
/// El servidor del <b>espacio</b>, si el correo pertenece a un hogar y ese hogar tiene uno
/// configurado y activo. Asi la invitacion a la pareja llega desde el correo del propietario.
/// </description></item>
/// <item><description>
/// El servidor de la <b>plataforma</b> guardado en base de datos.
/// </description></item>
/// <item><description>
/// La configuracion de <c>appsettings</c>, que sirve unicamente para el arranque inicial:
/// permite enviar la primera invitacion antes de que exista nada en la base de datos.
/// </description></item>
/// </list>
/// <para>
/// Si nada esta configurado, no se envia. No es un fallo: en desarrollo es lo normal, y el
/// enviador deja el contenido en el registro para poder copiar el enlace.
/// </para>
/// </remarks>
/// <param name="contexto">Contexto de base de datos.</param>
/// <param name="protector">Descifrador de las contrasenas guardadas.</param>
/// <param name="opciones">Configuracion de arranque de <c>appsettings</c>.</param>
/// <param name="registro">Registro de eventos.</param>
public class ResolvedorCorreo(
    ContextoRumbo contexto,
    IProtectorSecretos protector,
    IOptions<OpcionesCorreo> opciones,
    ILogger<ResolvedorCorreo> registro)
{
    private readonly OpcionesCorreo _arranque = opciones.Value;

    /// <summary>
    /// Devuelve las credenciales con las que enviar, o <c>null</c> si no hay ninguna.
    /// </summary>
    /// <param name="espacioId">
    /// Espacio del que parte el correo, o <c>null</c> si es un correo de plataforma.
    /// </param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Las credenciales resueltas y descifradas.</returns>
    public async Task<CredencialesSmtp?> ResolverAsync(
        Guid? espacioId,
        CancellationToken cancelacion = default)
    {
        if (espacioId.HasValue)
        {
            // SIN IgnoreQueryFilters, a proposito. Se penso para un futuro proceso en
            // segundo plano que enviara avisos de varios hogares, pero saltarse el filtro
            // "por si acaso" abre un agujero real hoy a cambio de una comodidad futura.
            //
            // Con el filtro activo, pedir la configuracion de un espacio que no es el
            // activo simplemente no devuelve nada, y el correo sale por el servidor de
            // plataforma. Es el comportamiento seguro. Cuando exista ese proceso, se le
            // dara su propio camino explicito.
            var delEspacio = await contexto.Set<ConfiguracionCorreoEspacio>()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.EspacioId == espacioId.Value, cancelacion);

            if (delEspacio is not null && delEspacio.EstaUtilizable())
            {
                return Construir(delEspacio, $"espacio {espacioId}");
            }
        }

        var dePlataforma = await contexto.Set<ConfiguracionCorreoPlataforma>()
            .AsNoTracking()
            .FirstOrDefaultAsync(cancelacion);

        if (dePlataforma is not null && dePlataforma.EstaUtilizable())
        {
            return Construir(dePlataforma, "plataforma");
        }

        if (_arranque.EstaConfigurado())
        {
            return new CredencialesSmtp(
                _arranque.Host,
                _arranque.Puerto,
                _arranque.UsarSslDirecto,
                string.IsNullOrWhiteSpace(_arranque.Usuario) ? null : _arranque.Usuario,
                string.IsNullOrWhiteSpace(_arranque.Clave) ? null : _arranque.Clave,
                _arranque.RemitenteCorreo,
                _arranque.RemitenteNombre,
                "appsettings");
        }

        return null;
    }

    /// <summary>
    /// Devuelve la direccion base para los enlaces de los correos.
    /// </summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La URL base configurada.</returns>
    /// <remarks>
    /// Vive solo en la configuracion de plataforma: los enlaces apuntan a la aplicacion, que
    /// es la misma para todos los espacios.
    /// </remarks>
    public async Task<string> ObtenerUrlBaseAsync(CancellationToken cancelacion = default)
    {
        var dePlataforma = await contexto.Set<ConfiguracionCorreoPlataforma>()
            .AsNoTracking()
            .Select(c => c.UrlBase)
            .FirstOrDefaultAsync(cancelacion);

        return string.IsNullOrWhiteSpace(dePlataforma) ? _arranque.UrlBase : dePlataforma;
    }

    /// <summary>Descifra la contrasena y arma las credenciales.</summary>
    private CredencialesSmtp Construir(ConfiguracionCorreoBase configuracion, string origen)
    {
        string? clave = null;

        if (!string.IsNullOrWhiteSpace(configuracion.ClaveCifrada))
        {
            clave = protector.Descifrar(configuracion.ClaveCifrada);

            if (clave is null)
            {
                registro.LogWarning(
                    "No se pudo descifrar la contraseña SMTP de {Origen}. Se intentará enviar "
                    + "sin autenticación, lo que probablemente falle.", origen);
            }
        }

        return new CredencialesSmtp(
            configuracion.Host!,
            configuracion.Puerto,
            configuracion.UsarSslDirecto,
            string.IsNullOrWhiteSpace(configuracion.Usuario) ? null : configuracion.Usuario,
            clave,
            configuracion.RemitenteCorreo!,
            string.IsNullOrWhiteSpace(configuracion.RemitenteNombre)
                ? "Rumbo"
                : configuracion.RemitenteNombre,
            origen);
    }
}
