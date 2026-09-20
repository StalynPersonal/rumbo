using Rumbo.Contratos.Auditoria;
using Rumbo.Contratos.Comun;

namespace Rumbo.Aplicacion.Contratos;

/// <summary>
/// Consulta del historial de auditoria del espacio activo.
/// </summary>
/// <remarks>
/// Permite responder "quién tocó este movimiento y cuándo" sin salir de la aplicación. En un
/// hogar compartido es una función de convivencia tanto como de seguridad.
/// </remarks>
public interface IServicioAuditoria
{
    /// <summary>Consulta el historial del espacio activo.</summary>
    /// <param name="filtro">Criterios de busqueda.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Una pagina del historial, de lo mas reciente a lo mas antiguo.</returns>
    Task<ResultadoPaginado<EntradaAuditoria>> ConsultarAsync(
        FiltroAuditoria filtro,
        CancellationToken cancelacion = default);
}
