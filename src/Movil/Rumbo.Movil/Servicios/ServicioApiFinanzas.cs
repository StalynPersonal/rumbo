using Rumbo.Contratos.Categorias;
using Rumbo.Contratos.Comun;
using Rumbo.Contratos.Cuentas;
using Rumbo.Contratos.Movimientos;

namespace Rumbo.Movil.Servicios;

/// <summary>
/// Cuentas, categorias y movimientos.
/// </summary>
/// <remarks>
/// Los tres van juntos porque las pantallas que los usan tambien: para registrar un gasto
/// hacen falta las cuentas y las categorias a la vez. Separarlos en tres clases obligaria a
/// inyectar tres servicios en cada modelo de vista sin ganar nada.
/// </remarks>
/// <param name="api">Cliente HTTP.</param>
public class ServicioApiFinanzas(ClienteApi api)
{
    /// <summary>Lista las cuentas del espacio.</summary>
    /// <returns>Las cuentas activas.</returns>
    public Task<List<CuentaResumen>> ListarCuentasAsync() =>
        api.ObtenerAsync<List<CuentaResumen>>("api/v1/cuentas");

    /// <summary>Devuelve el arbol de categorias.</summary>
    /// <returns>Las categorias de primer nivel con sus hijas.</returns>
    public Task<List<CategoriaArbol>> ListarCategoriasAsync() =>
        api.ObtenerAsync<List<CategoriaArbol>>("api/v1/categorias");

    /// <summary>Lista los ultimos movimientos.</summary>
    /// <param name="pagina">Pagina, empezando en 1.</param>
    /// <param name="tamano">Cuantos por pagina.</param>
    /// <returns>La pagina de movimientos.</returns>
    public Task<ResultadoPaginado<MovimientoResumen>> ListarMovimientosAsync(
        int pagina = 1,
        int tamano = 30) =>
        api.ObtenerAsync<ResultadoPaginado<MovimientoResumen>>(
            $"api/v1/movimientos?Pagina={pagina}&TamanoPagina={tamano}");

    /// <summary>Registra un ingreso, un gasto o un ajuste.</summary>
    /// <param name="solicitud">Datos del movimiento.</param>
    /// <returns>El movimiento registrado.</returns>
    /// <remarks>
    /// Las transferencias NO pasan por aqui: tienen su propio endpoint porque crean dos
    /// asientos, y ninguno cuenta como ingreso ni como gasto.
    /// </remarks>
    public Task<MovimientoResumen> RegistrarAsync(SolicitudRegistrarMovimiento solicitud) =>
        api.EnviarAsync<SolicitudRegistrarMovimiento, MovimientoResumen>(
            "api/v1/movimientos", solicitud);
}
