using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Aplicacion.Recomendaciones;
using Rumbo.Contratos.Recomendaciones;
using Rumbo.Dominio.Entidades.Soporte;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Recomendaciones;

/// <summary>
/// Lee las sugerencias guardadas y registra la decision de la persona.
/// </summary>
/// <param name="contexto">Acceso a los datos.</param>
/// <param name="motor">Motor que las calcula.</param>
/// <param name="fechaHora">Proveedor de fecha y hora.</param>
public class ServicioRecomendaciones(
    IContextoRumbo contexto,
    MotorRecomendaciones motor,
    IProveedorFechaHora fechaHora) : IServicioRecomendaciones
{
    /// <summary>
    /// Convierte la entidad en su DTO. Es una expresion, no un metodo, para que EF Core la
    /// traduzca a SQL en vez de traerse las filas enteras.
    /// </summary>
    private static readonly Expression<Func<Recomendacion, RecomendacionDto>> Proyectar =
        r => new RecomendacionDto(
            r.Id,
            r.Tipo.ToString(),
            r.Titulo,
            r.Cuerpo,
            r.Insumos,
            r.MontoSugerido,
            r.Moneda,
            r.ConfianzaBaja,
            r.MetaId,
            r.ViajeId,
            r.FechaGeneracion,
            r.FechaExpiracion,
            r.Estado.ToString());

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecomendacionDto>> ListarAsync(
        bool incluirRespondidas = false,
        CancellationToken cancelacion = default)
    {
        var consulta = contexto.Recomendaciones.AsNoTracking();

        if (!incluirRespondidas)
        {
            consulta = consulta.Where(r => r.Estado == EstadoRecomendacion.Pendiente);
        }

        return await consulta
            .OrderByDescending(r => r.FechaGeneracion)
            .Select(Proyectar)
            .ToListAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecomendacionDto>> RecalcularAsync(
        CancellationToken cancelacion = default)
    {
        await motor.RecalcularAsync(cancelacion);

        return await ListarAsync(incluirRespondidas: false, cancelacion);
    }

    /// <inheritdoc />
    public async Task<RecomendacionDto> ResponderAsync(
        Guid recomendacionId,
        SolicitudResponderRecomendacion solicitud,
        CancellationToken cancelacion = default)
    {
        if (!Enum.TryParse<EstadoRecomendacion>(
                solicitud.Estado, ignoreCase: true, out var estado)
            || estado is not (EstadoRecomendacion.Aceptada or EstadoRecomendacion.Descartada))
        {
            throw new ExcepcionDominio("La respuesta debe ser Aceptada o Descartada.");
        }

        var recomendacion = await contexto.Recomendaciones
            .FirstOrDefaultAsync(r => r.Id == recomendacionId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la recomendación");

        if (recomendacion.Estado != EstadoRecomendacion.Pendiente)
        {
            throw new ExcepcionDominio("Esta sugerencia ya fue respondida.");
        }

        // Aceptar solo marca la decision. El dinero no se mueve aqui: el aporte o el ajuste
        // se registra despues por su propia operacion, que la persona confirma. El sistema
        // jamas transfiere por su cuenta.
        recomendacion.Estado = estado;
        recomendacion.FechaRespuesta = fechaHora.AhoraUtc;

        await contexto.SaveChangesAsync(cancelacion);

        return await contexto.Recomendaciones
            .AsNoTracking()
            .Where(r => r.Id == recomendacionId)
            .Select(Proyectar)
            .FirstAsync(cancelacion);
    }
}
