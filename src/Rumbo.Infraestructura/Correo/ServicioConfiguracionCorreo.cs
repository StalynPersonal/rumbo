using MailKit.Net.Smtp;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Correo;
using Rumbo.Dominio.Entidades.Soporte;
using Rumbo.Dominio.Excepciones;
using Rumbo.Infraestructura.Persistencia;

namespace Rumbo.Infraestructura.Correo;

/// <summary>
/// Guarda y comprueba los servidores SMTP de la plataforma y de cada espacio.
/// </summary>
/// <param name="contexto">Contexto de base de datos.</param>
/// <param name="protector">Cifrador de las contrasenas.</param>
/// <param name="resolvedor">Resuelve que servidor usar.</param>
/// <param name="registro">Registro de eventos.</param>
public partial class ServicioConfiguracionCorreo(
    ContextoRumbo contexto,
    IProtectorSecretos protector,
    ResolvedorCorreo resolvedor,
    ILogger<ServicioConfiguracionCorreo> registro) : IServicioConfiguracionCorreo
{
    /// <inheritdoc />
    public async Task<ConfiguracionCorreoDto> ObtenerPlataformaAsync(
        CancellationToken cancelacion = default) =>
        Proyectar(await BuscarOCrearPlataformaAsync(cancelacion));

    /// <inheritdoc />
    public async Task<ConfiguracionCorreoDto> GuardarPlataformaAsync(
        SolicitudGuardarCorreo solicitud,
        CancellationToken cancelacion = default)
    {
        Validar(solicitud);

        var configuracion = await BuscarOCrearPlataformaAsync(cancelacion);

        Aplicar(configuracion, solicitud);

        await contexto.SaveChangesAsync(cancelacion);

        registro.LogInformation(
            "Configuración de correo de la plataforma actualizada. Servidor: {Host}.",
            configuracion.Host);

        return Proyectar(configuracion);
    }

    /// <inheritdoc />
    public async Task<ConfiguracionCorreoDto> ObtenerDelEspacioAsync(
        Guid espacioId,
        CancellationToken cancelacion = default) =>
        Proyectar(await BuscarOCrearDelEspacioAsync(espacioId, cancelacion));

    /// <inheritdoc />
    public async Task<ConfiguracionCorreoDto> GuardarDelEspacioAsync(
        Guid espacioId,
        SolicitudGuardarCorreo solicitud,
        CancellationToken cancelacion = default)
    {
        Validar(solicitud);

        var configuracion = await BuscarOCrearDelEspacioAsync(espacioId, cancelacion);

        Aplicar(configuracion, solicitud);

        await contexto.SaveChangesAsync(cancelacion);

        registro.LogInformation(
            "Configuración de correo del espacio {EspacioId} actualizada.", espacioId);

        return Proyectar(configuracion);
    }

    /// <summary>
    /// Comprueba los datos antes de guardarlos.
    /// </summary>
    /// <param name="solicitud">Datos propuestos.</param>
    /// <remarks>
    /// Solo se exige servidor y remitente cuando la configuracion se marca como activa. Asi
    /// se puede guardar a medias, desactivada, e ir completandola.
    /// </remarks>
    private static void Validar(SolicitudGuardarCorreo solicitud)
    {
        if (solicitud.Puerto is < 1 or > 65535)
        {
            throw new ExcepcionDominio("El puerto debe estar entre 1 y 65535.");
        }

        if (!solicitud.Activa)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(solicitud.Host))
        {
            throw new ExcepcionDominio(
                "Para activar el envío hay que indicar el servidor SMTP.");
        }

        if (string.IsNullOrWhiteSpace(solicitud.RemitenteCorreo))
        {
            throw new ExcepcionDominio(
                "Para activar el envío hay que indicar la dirección del remitente.");
        }

        if (!solicitud.RemitenteCorreo.Contains('@', StringComparison.Ordinal))
        {
            throw new ExcepcionDominio("La dirección del remitente no tiene un formato válido.");
        }
    }

    /// <summary>Vuelca la solicitud sobre la entidad, cifrando la contrasena.</summary>
    /// <param name="configuracion">Entidad que se actualiza.</param>
    /// <param name="solicitud">Datos nuevos.</param>
    private void Aplicar(ConfiguracionCorreoBase configuracion, SolicitudGuardarCorreo solicitud)
    {
        configuracion.Host = solicitud.Host?.Trim();
        configuracion.Puerto = solicitud.Puerto;
        configuracion.UsarSslDirecto = solicitud.UsarSslDirecto;
        configuracion.Usuario = solicitud.Usuario?.Trim();
        configuracion.RemitenteCorreo = solicitud.RemitenteCorreo?.Trim();
        configuracion.RemitenteNombre = solicitud.RemitenteNombre?.Trim();
        configuracion.Activa = solicitud.Activa;

        // Una contraseña vacía CONSERVA la que hubiera. De lo contrario, cambiar el puerto
        // obligaría a volver a escribirla, y la API nunca la devuelve para poder reenviarla.
        if (!string.IsNullOrWhiteSpace(solicitud.Clave))
        {
            configuracion.ClaveCifrada = protector.Cifrar(solicitud.Clave);

            // Los datos cambiaron: la prueba anterior ya no dice nada sobre esta.
            configuracion.FechaUltimaPrueba = null;
            configuracion.UltimaPruebaCorrecta = null;
            configuracion.UltimoErrorPrueba = null;
        }
    }

    /// <summary>Convierte la entidad en su DTO, SIN la contrasena.</summary>
    /// <param name="configuracion">Entidad guardada.</param>
    /// <returns>El DTO para la API.</returns>
    private static ConfiguracionCorreoDto Proyectar(ConfiguracionCorreoBase configuracion) =>
        new(configuracion.Host,
            configuracion.Puerto,
            configuracion.UsarSslDirecto,
            configuracion.Usuario,
            !string.IsNullOrWhiteSpace(configuracion.ClaveCifrada),
            configuracion.RemitenteCorreo,
            configuracion.RemitenteNombre,
            configuracion.Activa,
            configuracion.FechaUltimaPrueba,
            configuracion.UltimaPruebaCorrecta,
            configuracion.UltimoErrorPrueba);

    /// <summary>Devuelve la fila de plataforma, creandola vacia si no existe.</summary>
    private async Task<ConfiguracionCorreoPlataforma> BuscarOCrearPlataformaAsync(
        CancellationToken cancelacion)
    {
        var configuracion = await contexto.Set<ConfiguracionCorreoPlataforma>()
            .FirstOrDefaultAsync(cancelacion);

        if (configuracion is not null)
        {
            return configuracion;
        }

        configuracion = new ConfiguracionCorreoPlataforma();

        contexto.Set<ConfiguracionCorreoPlataforma>().Add(configuracion);
        await contexto.SaveChangesAsync(cancelacion);

        return configuracion;
    }

    /// <summary>Devuelve la configuracion del espacio, creandola vacia si no existe.</summary>
    private async Task<ConfiguracionCorreoEspacio> BuscarOCrearDelEspacioAsync(
        Guid espacioId,
        CancellationToken cancelacion)
    {
        var configuracion = await contexto.Set<ConfiguracionCorreoEspacio>()
            .FirstOrDefaultAsync(c => c.EspacioId == espacioId, cancelacion);

        if (configuracion is not null)
        {
            return configuracion;
        }

        configuracion = new ConfiguracionCorreoEspacio { EspacioId = espacioId };

        contexto.Set<ConfiguracionCorreoEspacio>().Add(configuracion);
        await contexto.SaveChangesAsync(cancelacion);

        return configuracion;
    }
}
