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

    /// <summary>Crea una cuenta.</summary>
    /// <param name="solicitud">Datos de la cuenta.</param>
    /// <returns>La cuenta creada.</returns>
    /// <remarks>
    /// Hace falta desde el movil: quien instala el APK y entra por primera vez no tiene
    /// ninguna cuenta, y sin cuenta no puede registrar nada. Sin esto, la aplicacion seria
    /// inutil hasta que alguien creara la primera cuenta por otro medio.
    /// </remarks>
    public Task<CuentaResumen> CrearCuentaAsync(SolicitudCrearCuenta solicitud) =>
        api.EnviarAsync<SolicitudCrearCuenta, CuentaResumen>("api/v1/cuentas", solicitud);

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

    /// <summary>Modifica un movimiento.</summary>
    /// <param name="movimientoId">Movimiento que se modifica.</param>
    /// <param name="solicitud">Datos nuevos.</param>
    /// <returns>El movimiento actualizado.</returns>
    /// <remarks>
    /// El tipo y la cuenta NO se pueden cambiar: cambiar la cuenta significaria mover dinero
    /// de un sitio a otro, y eso es otra operacion. Para corregirlo hay que borrar y volver
    /// a registrar.
    /// </remarks>
    public Task<MovimientoResumen> ActualizarMovimientoAsync(
        Guid movimientoId,
        SolicitudActualizarMovimiento solicitud) =>
        api.ActualizarAsync<SolicitudActualizarMovimiento, MovimientoResumen>(
            $"api/v1/movimientos/{movimientoId}", solicitud);

    /// <summary>Borra un movimiento y revierte su efecto sobre el saldo.</summary>
    /// <param name="movimientoId">Movimiento que se borra.</param>
    /// <returns>Tarea que finaliza cuando queda borrado.</returns>
    /// <remarks>
    /// El borrado es LOGICO: la fila permanece para la auditoria y deja de contar en el
    /// saldo y en los informes. Una pata suelta de transferencia no se puede borrar por su
    /// cuenta, porque dejaria dinero apareciendo en la otra cuenta.
    /// </remarks>
    public Task BorrarMovimientoAsync(Guid movimientoId) =>
        api.BorrarAsync($"api/v1/movimientos/{movimientoId}");

    /// <summary>Borra un traspaso completo y sus DOS asientos.</summary>
    /// <param name="transferenciaId">Traspaso que se borra.</param>
    /// <returns>Tarea que finaliza cuando queda borrado.</returns>
    /// <remarks>
    /// Una pata suelta no se puede borrar: dejaria dinero apareciendo o desapareciendo en la
    /// otra cuenta. Por eso, al borrar un asiento de traspaso hay que borrar el traspaso
    /// entero, y la aplicacion lo avisa antes.
    /// </remarks>
    public Task BorrarTransferenciaAsync(Guid transferenciaId) =>
        api.BorrarAsync($"api/v1/movimientos/transferencias/{transferenciaId}");

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
