using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Auditoria;
using Rumbo.Contratos.Comun;
using Rumbo.Dominio.Enums;
using Rumbo.Infraestructura.Persistencia;

namespace Rumbo.Infraestructura.Identidad;

/// <summary>
/// Consulta del historial de auditoria.
/// </summary>
/// <remarks>
/// <para>
/// <c>RegistrosAuditoria</c> es una tabla GLOBAL sin filtro de aislamiento: tiene que serlo
/// porque registra tambien acciones sin espacio, como un inicio de sesion fallido. Por eso
/// aqui el aislamiento se aplica <b>de forma explicita</b> con un <c>Where</c> por el
/// espacio activo, y esa condicion no puede faltar.
/// </para>
/// <para>
/// No se devuelve el campo de cambios, que contiene los valores anteriores y nuevos. En un
/// movimiento esos valores son importes: exponerlos convertiria el historial en una segunda
/// via para leer las finanzas del hogar, saltandose los permisos del modulo correspondiente.
/// </para>
/// </remarks>
/// <param name="contexto">Contexto de base de datos.</param>
/// <param name="contextoEspacio">Espacio activo.</param>
public class ServicioAuditoria(
    ContextoRumbo contexto,
    IContextoEspacio contextoEspacio) : IServicioAuditoria
{
    /// <summary>Tamano maximo de pagina admitido.</summary>
    private const int TamanoMaximoPagina = 200;

    /// <inheritdoc />
    public async Task<ResultadoPaginado<EntradaAuditoria>> ConsultarAsync(
        FiltroAuditoria filtro,
        CancellationToken cancelacion = default)
    {
        var espacioId = contextoEspacio.ObtenerEspacioObligatorio();

        var pagina = Math.Max(1, filtro.Pagina);
        var tamano = Math.Clamp(filtro.TamanoPagina, 1, TamanoMaximoPagina);

        // El Where por espacio es OBLIGATORIO: esta tabla no lleva filtro global.
        var consulta = contexto.RegistrosAuditoria
            .AsNoTracking()
            .Where(r => r.EspacioId == espacioId);

        if (filtro.Desde.HasValue)
        {
            consulta = consulta.Where(r => r.FechaHora >= filtro.Desde.Value);
        }

        if (filtro.Hasta.HasValue)
        {
            consulta = consulta.Where(r => r.FechaHora <= filtro.Hasta.Value);
        }

        if (filtro.UsuarioId.HasValue)
        {
            consulta = consulta.Where(r => r.UsuarioId == filtro.UsuarioId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtro.TipoEntidad))
        {
            var tipo = filtro.TipoEntidad.Trim();
            consulta = consulta.Where(r => r.TipoEntidad == tipo);
        }

        if (filtro.EntidadId.HasValue)
        {
            consulta = consulta.Where(r => r.EntidadId == filtro.EntidadId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Accion)
            && Enum.TryParse<AccionAuditoria>(filtro.Accion, ignoreCase: true, out var accion))
        {
            consulta = consulta.Where(r => r.Accion == accion);
        }

        var total = await consulta.CountAsync(cancelacion);

        var elementos = await consulta
            .OrderByDescending(r => r.FechaHora)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(r => new EntradaAuditoria(
                r.Id,
                r.UsuarioId,
                r.CorreoUsuario,
                r.Accion.ToString(),
                r.TipoEntidad,
                r.EntidadId,
                r.FechaHora,
                r.Descripcion,
                r.Exitosa))
            .ToListAsync(cancelacion);

        return new ResultadoPaginado<EntradaAuditoria>(elementos, pagina, tamano, total);
    }
}
