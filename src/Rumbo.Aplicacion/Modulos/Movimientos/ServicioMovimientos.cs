using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Calculadoras;
using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Comun;
using Rumbo.Contratos.Movimientos;
using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Movimientos;

/// <summary>
/// Registro y consulta del libro mayor.
/// </summary>
/// <remarks>
/// El aislamiento entre espacios lo garantizan los filtros globales del contexto. Este
/// servicio no filtra por espacio a mano y, aun asi, ninguna consulta puede devolver un
/// movimiento ajeno ni ninguna escritura alcanzar otro hogar.
/// </remarks>
/// <param name="contexto">Acceso a los datos.</param>
/// <param name="conversor">Conversion entre monedas.</param>
/// <param name="contextoEspacio">Espacio activo.</param>
/// <param name="usuarioActual">Quien realiza la operacion.</param>
public partial class ServicioMovimientos(
    IContextoRumbo contexto,
    ConversorMonedas conversor,
    IContextoEspacio contextoEspacio,
    IUsuarioActual usuarioActual) : IServicioMovimientos
{
    /// <summary>Tamano maximo de pagina admitido.</summary>
    /// <remarks>
    /// Un hogar con tres anos de historial acumula decenas de miles de filas. Sin tope,
    /// una peticion podria pedirlas todas y agotar la memoria del telefono.
    /// </remarks>
    private const int TamanoMaximoPagina = 200;

    /// <inheritdoc />
    public async Task<ResultadoPaginado<MovimientoResumen>> ListarAsync(
        FiltroMovimientos filtro,
        CancellationToken cancelacion = default)
    {
        var pagina = Math.Max(1, filtro.Pagina);
        var tamano = Math.Clamp(filtro.TamanoPagina, 1, TamanoMaximoPagina);

        var consulta = AplicarFiltros(contexto.Movimientos.AsNoTracking(), filtro);

        var total = await consulta.CountAsync(cancelacion);

        var elementos = await consulta
            .OrderByDescending(m => m.FechaMovimiento)
            .ThenByDescending(m => m.FechaCreacion)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(Proyeccion)
            .ToListAsync(cancelacion);

        return new ResultadoPaginado<MovimientoResumen>(elementos, pagina, tamano, total);
    }

    /// <inheritdoc />
    public async Task<MovimientoResumen> ObtenerAsync(
        Guid movimientoId,
        CancellationToken cancelacion = default) =>
        await contexto.Movimientos
            .AsNoTracking()
            .Where(m => m.Id == movimientoId)
            .Select(Proyeccion)
            .FirstOrDefaultAsync(cancelacion)
        ?? throw new ExcepcionNoEncontrado("el movimiento");

    /// <summary>Aplica los filtros de busqueda a la consulta.</summary>
    /// <param name="consulta">Consulta de partida.</param>
    /// <param name="filtro">Criterios.</param>
    /// <returns>La consulta filtrada.</returns>
    private static IQueryable<Movimiento> AplicarFiltros(
        IQueryable<Movimiento> consulta,
        FiltroMovimientos filtro)
    {
        if (filtro.Desde.HasValue)
        {
            consulta = consulta.Where(m => m.FechaMovimiento >= filtro.Desde.Value);
        }

        if (filtro.Hasta.HasValue)
        {
            consulta = consulta.Where(m => m.FechaMovimiento <= filtro.Hasta.Value);
        }

        if (filtro.CuentaId.HasValue)
        {
            consulta = consulta.Where(m => m.CuentaId == filtro.CuentaId.Value);
        }

        if (filtro.CategoriaId.HasValue)
        {
            consulta = consulta.Where(m => m.CategoriaId == filtro.CategoriaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Tipo)
            && Enum.TryParse<TipoMovimiento>(filtro.Tipo, ignoreCase: true, out var tipo))
        {
            consulta = consulta.Where(m => m.Tipo == tipo);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Reparto)
            && Enum.TryParse<TipoReparto>(filtro.Reparto, ignoreCase: true, out var reparto))
        {
            consulta = consulta.Where(m => m.Reparto == reparto);
        }

        if (filtro.PagadoPorUsuarioId.HasValue)
        {
            consulta = consulta.Where(m => m.PagadoPorUsuarioId == filtro.PagadoPorUsuarioId.Value);
        }

        if (filtro.ViajeId.HasValue)
        {
            consulta = consulta.Where(m => m.ViajeId == filtro.ViajeId.Value);
        }

        if (filtro.MetaId.HasValue)
        {
            consulta = consulta.Where(m => m.MetaId == filtro.MetaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Busqueda))
        {
            var texto = filtro.Busqueda.Trim();

            consulta = consulta.Where(m =>
                EF.Functions.Like(m.Descripcion, $"%{texto}%")
                || (m.Notas != null && EF.Functions.Like(m.Notas, $"%{texto}%")));
        }

        return consulta;
    }

    /// <summary>
    /// Proyeccion de la entidad al DTO, como arbol de expresiones.
    /// </summary>
    /// <remarks>
    /// <b>Tiene que ser una expresion y no un metodo.</b> EF Core no sabe traducir una
    /// llamada a metodo a SQL: la evaluaria en cliente, sobre entidades cuyas propiedades de
    /// navegacion no estan cargadas, y reventaria con una referencia nula al pedir el nombre
    /// de la cuenta. Declarada asi, la conversion ocurre dentro de la consulta y solo se
    /// traen las columnas necesarias.
    /// </remarks>
    private static readonly Expression<Func<Movimiento, MovimientoResumen>> Proyeccion =
        movimiento => new MovimientoResumen(
            movimiento.Id,
            movimiento.Tipo.ToString(),
            movimiento.CuentaId,
            movimiento.Cuenta!.Nombre,
            movimiento.CategoriaId,
            movimiento.Categoria != null ? movimiento.Categoria.Nombre : null,
            movimiento.Monto,
            movimiento.Signo,
            movimiento.Moneda,
            movimiento.MontoEnMonedaBase,
            movimiento.TasaEsAproximada,
            movimiento.FechaMovimiento,
            movimiento.Descripcion,
            movimiento.Notas,
            movimiento.MetodoPago != null ? movimiento.MetodoPago.ToString() : null,
            movimiento.Reparto.ToString(),
            movimiento.PagadoPorUsuarioId,
            movimiento.MetaId,
            movimiento.ViajeId,
            movimiento.TransferenciaId,
            movimiento.Tipo == TipoMovimiento.Ingreso || movimiento.Tipo == TipoMovimiento.Gasto);
}
