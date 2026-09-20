using System.Net.Http.Json;

using Rumbo.Contratos.Categorias;
using Rumbo.Contratos.Cuentas;
using Rumbo.Contratos.Movimientos;
using Rumbo.PruebasIntegracion.Autenticacion;

namespace Rumbo.PruebasIntegracion.Financiero;

/// <summary>
/// Atajos para montar escenarios financieros en las pruebas.
/// </summary>
public static class AyudanteFinanciero
{
    /// <summary>Crea una cuenta y devuelve sus datos.</summary>
    /// <param name="cliente">Cliente ya autenticado.</param>
    /// <param name="nombre">Nombre de la cuenta.</param>
    /// <param name="saldoInicial">Saldo de partida.</param>
    /// <param name="tipo">Tipo de cuenta.</param>
    /// <param name="moneda">Codigo ISO-4217.</param>
    /// <returns>La cuenta creada.</returns>
    public static async Task<CuentaResumen> CrearCuentaAsync(
        HttpClient cliente,
        string nombre,
        decimal saldoInicial = 0m,
        string tipo = "Bancaria",
        string moneda = "DOP")
    {
        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/cuentas",
            new SolicitudCrearCuenta(
                nombre, tipo, moneda, saldoInicial, null, false, null, null, null, null, null, null));

        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<CuentaResumen>())!;
    }

    /// <summary>
    /// Devuelve una categoria de gasto cualquiera de las que trae el espacio de serie.
    /// </summary>
    /// <param name="cliente">Cliente ya autenticado.</param>
    /// <returns>Una categoria de gasto.</returns>
    /// <remarks>
    /// Todo espacio nuevo nace con su arbol de categorias, asi que no hace falta crearlas.
    /// </remarks>
    public static async Task<CategoriaArbol> ObtenerCategoriaDeGastoAsync(HttpClient cliente)
    {
        var arbol = await cliente.GetFromJsonAsync<List<CategoriaArbol>>("/api/v1/categorias");

        var padre = arbol!.First(c => c.Tipo == "Gasto" && c.Subcategorias.Count > 0);

        return padre.Subcategorias[0];
    }

    /// <summary>Devuelve una categoria de ingreso de las predeterminadas.</summary>
    /// <param name="cliente">Cliente ya autenticado.</param>
    /// <returns>Una categoria de ingreso.</returns>
    public static async Task<CategoriaArbol> ObtenerCategoriaDeIngresoAsync(HttpClient cliente)
    {
        var arbol = await cliente.GetFromJsonAsync<List<CategoriaArbol>>("/api/v1/categorias");

        var padre = arbol!.First(c => c.Tipo == "Ingreso" && c.Subcategorias.Count > 0);

        return padre.Subcategorias[0];
    }

    /// <summary>Registra un movimiento y devuelve su resumen.</summary>
    /// <param name="cliente">Cliente ya autenticado.</param>
    /// <param name="tipo">Ingreso, Gasto o Ajuste.</param>
    /// <param name="cuentaId">Cuenta afectada.</param>
    /// <param name="categoriaId">Categoria del movimiento.</param>
    /// <param name="monto">Importe, siempre positivo.</param>
    /// <param name="descripcion">Descripcion corta.</param>
    /// <param name="fecha">Fecha contable. Si se omite, se usa el 15/09/2026.</param>
    /// <returns>El movimiento registrado.</returns>
    public static async Task<MovimientoResumen> RegistrarAsync(
        HttpClient cliente,
        string tipo,
        Guid cuentaId,
        Guid? categoriaId,
        decimal monto,
        string descripcion,
        DateOnly? fecha = null)
    {
        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/movimientos",
            new SolicitudRegistrarMovimiento(
                tipo, cuentaId, categoriaId, monto, null,
                fecha ?? new DateOnly(2026, 9, 15), descripcion,
                null, null, "Personal", null, null));

        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<MovimientoResumen>())!;
    }

    /// <summary>Vuelve a leer una cuenta para comprobar su saldo.</summary>
    /// <param name="cliente">Cliente ya autenticado.</param>
    /// <param name="cuentaId">Cuenta consultada.</param>
    /// <returns>La cuenta con su saldo actual.</returns>
    public static async Task<CuentaResumen> LeerCuentaAsync(HttpClient cliente, Guid cuentaId) =>
        (await cliente.GetFromJsonAsync<CuentaResumen>($"/api/v1/cuentas/{cuentaId}"))!;

    /// <summary>Crea un espacio con su propietario y devuelve un cliente autenticado.</summary>
    /// <param name="fabrica">Fabrica de la API.</param>
    /// <param name="correo">Correo del propietario.</param>
    /// <param name="nombreEspacio">Nombre del espacio.</param>
    /// <returns>Cliente listo para operar sobre ese espacio.</returns>
    public static async Task<HttpClient> CrearHogarAsync(
        FabricaApiDePrueba fabrica,
        string correo,
        string nombreEspacio)
    {
        var propietario = await AyudanteApi.CrearEspacioConPropietarioAsync(
            fabrica, correo, nombreEspacio);

        return AyudanteApi.ClienteConToken(fabrica, propietario.TokenAcceso);
    }
}
