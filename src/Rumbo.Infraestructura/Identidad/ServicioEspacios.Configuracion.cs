using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Rumbo.Contratos.Espacios;
using Rumbo.Dominio.Entidades.Identidad;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Infraestructura.Identidad;

/// <summary>
/// Parte del servicio de espacios dedicada a las preferencias.
/// </summary>
public partial class ServicioEspacios
{
    /// <inheritdoc />
    public async Task<ConfiguracionEspacioDto> ObtenerConfiguracionAsync(
        Guid espacioId,
        CancellationToken cancelacion = default)
    {
        var configuracion = await BuscarOCrearConfiguracionAsync(espacioId, cancelacion);

        return Proyectar(configuracion);
    }

    /// <inheritdoc />
    public async Task<ConfiguracionEspacioDto> ActualizarConfiguracionAsync(
        Guid espacioId,
        SolicitudActualizarConfiguracion solicitud,
        CancellationToken cancelacion = default)
    {
        Validar(solicitud);

        var configuracion = await BuscarOCrearConfiguracionAsync(espacioId, cancelacion);

        configuracion.DiaInicioMes = solicitud.DiaInicioMes;
        configuracion.UmbralAvisoPresupuesto = solicitud.UmbralAvisoPresupuesto;
        configuracion.UmbralCriticoPresupuesto = solicitud.UmbralCriticoPresupuesto;
        configuracion.UmbralExcedidoPresupuesto = solicitud.UmbralExcedidoPresupuesto;
        configuracion.MesesHistorialParaAnalisis = solicitud.MesesHistorialParaAnalisis;
        configuracion.RecomendacionesActivas = solicitud.RecomendacionesActivas;
        configuracion.DiasAvisoPagoRecurrente = solicitud.DiasAvisoPagoRecurrente;

        await contexto.SaveChangesAsync(cancelacion);

        registro.LogInformation("Configuración del espacio {EspacioId} actualizada.", espacioId);

        return Proyectar(configuracion);
    }

    /// <summary>
    /// Comprueba las preferencias antes de guardarlas.
    /// </summary>
    /// <param name="solicitud">Preferencias propuestas.</param>
    /// <remarks>
    /// Las mismas reglas existen como restricciones CHECK en la base de datos. Se validan
    /// tambien aqui para poder devolver un mensaje comprensible en lugar de un error de SQL.
    /// </remarks>
    private static void Validar(SolicitudActualizarConfiguracion solicitud)
    {
        // El limite de 28 no es arbitrario: es el ultimo dia que existe en TODOS los meses.
        // Admitir el 31 obligaria a decidir que hacer en febrero.
        if (solicitud.DiaInicioMes is < 1 or > 28)
        {
            throw new ExcepcionDominio(
                "El día de inicio del mes debe estar entre 1 y 28, para que exista en todos "
                + "los meses del año.");
        }

        if (solicitud.UmbralAvisoPresupuesto <= 0
            || solicitud.UmbralAvisoPresupuesto > solicitud.UmbralCriticoPresupuesto
            || solicitud.UmbralCriticoPresupuesto > solicitud.UmbralExcedidoPresupuesto)
        {
            throw new ExcepcionDominio(
                "Los umbrales deben ir en orden: aviso menor o igual que crítico, y crítico "
                + "menor o igual que excedido.");
        }

        if (solicitud.MesesHistorialParaAnalisis is < 1 or > 60)
        {
            throw new ExcepcionDominio(
                "Los meses de historial para el análisis deben estar entre 1 y 60.");
        }

        if (solicitud.DiasAvisoPagoRecurrente is < 0 or > 30)
        {
            throw new ExcepcionDominio(
                "Los días de aviso de un pago deben estar entre 0 y 30.");
        }
    }

    /// <summary>
    /// Devuelve la configuracion del espacio, creandola con los valores por defecto si falta.
    /// </summary>
    /// <param name="espacioId">Espacio activo.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La configuracion.</returns>
    /// <remarks>
    /// Normalmente se crea junto con el espacio. Se contempla su ausencia para que un espacio
    /// creado antes de que existiera esta tabla, o por una migracion de datos, no deje la
    /// aplicacion inutilizable.
    /// </remarks>
    private async Task<ConfiguracionEspacio> BuscarOCrearConfiguracionAsync(
        Guid espacioId,
        CancellationToken cancelacion)
    {
        var configuracion = await contexto.ConfiguracionesEspacio
            .FirstOrDefaultAsync(c => c.EspacioId == espacioId, cancelacion);

        if (configuracion is not null)
        {
            return configuracion;
        }

        configuracion = new ConfiguracionEspacio { EspacioId = espacioId };

        contexto.ConfiguracionesEspacio.Add(configuracion);
        await contexto.SaveChangesAsync(cancelacion);

        return configuracion;
    }

    /// <summary>Convierte la entidad de configuracion en su DTO.</summary>
    private static ConfiguracionEspacioDto Proyectar(ConfiguracionEspacio configuracion) =>
        new(configuracion.DiaInicioMes,
            configuracion.UmbralAvisoPresupuesto,
            configuracion.UmbralCriticoPresupuesto,
            configuracion.UmbralExcedidoPresupuesto,
            configuracion.MesesHistorialParaAnalisis,
            configuracion.RecomendacionesActivas,
            configuracion.DiasAvisoPagoRecurrente);
}
