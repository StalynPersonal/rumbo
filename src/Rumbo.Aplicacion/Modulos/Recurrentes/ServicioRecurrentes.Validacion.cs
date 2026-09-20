using Microsoft.EntityFrameworkCore;

using Rumbo.Contratos.Recurrentes;
using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Recurrentes;

/// <summary>
/// Validaciones y volcado de datos que comparten gastos e ingresos recurrentes.
/// </summary>
public partial class ServicioRecurrentes
{
    /// <summary>Vuelca la solicitud de gasto sobre la entidad.</summary>
    /// <param name="gasto">Entidad que se rellena.</param>
    /// <param name="solicitud">Datos recibidos.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando se aplican los datos.</returns>
    private async Task AplicarAsync(
        GastoRecurrente gasto,
        SolicitudGuardarGastoRecurrente solicitud,
        CancellationToken cancelacion)
    {
        var nombre = ValidarNombre(solicitud.Nombre);

        if (solicitud.MontoEstimado < 0m)
        {
            throw new ExcepcionDominio("El importe estimado no puede ser negativo.");
        }

        if (solicitud.DiasAvisoPrevio is < 0 or > 30)
        {
            throw new ExcepcionDominio("Los días de aviso deben estar entre 0 y 30.");
        }

        var categoria = await BuscarCategoriaAsync(solicitud.CategoriaId, cancelacion);

        if (categoria.Tipo == TipoCategoria.Ingreso)
        {
            throw new ExcepcionDominio(
                $"«{categoria.Nombre}» es una categoría de ingreso y no puede usarse en un "
                + "gasto recurrente.");
        }

        var cuenta = await BuscarCuentaAsync(solicitud.CuentaId, cancelacion);

        gasto.Nombre = nombre;
        gasto.CategoriaId = categoria.Id;
        gasto.CuentaId = cuenta.Id;
        gasto.MontoEstimado = solicitud.MontoEstimado;
        gasto.Moneda = ResolverMoneda(solicitud.Moneda, cuenta.Moneda);
        gasto.EsMontoFijo = solicitud.EsMontoFijo;
        gasto.Frecuencia = LeerFrecuencia(solicitud.Frecuencia);
        gasto.ProximaFechaPago = solicitud.ProximaFechaPago;
        gasto.DiasAvisoPrevio = solicitud.DiasAvisoPrevio;
        gasto.Notas = solicitud.Notas?.Trim();

        gasto.Reparto = Enum.TryParse<TipoReparto>(solicitud.Reparto, ignoreCase: true, out var r)
            ? r
            : TipoReparto.Compartido;
    }

    /// <summary>Vuelca la solicitud de ingreso sobre la entidad.</summary>
    /// <param name="ingreso">Entidad que se rellena.</param>
    /// <param name="solicitud">Datos recibidos.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando se aplican los datos.</returns>
    private async Task AplicarAsync(
        IngresoRecurrente ingreso,
        SolicitudGuardarIngresoRecurrente solicitud,
        CancellationToken cancelacion)
    {
        var nombre = ValidarNombre(solicitud.Nombre);

        if (solicitud.MontoEstimado < 0m)
        {
            throw new ExcepcionDominio("El importe estimado no puede ser negativo.");
        }

        var categoria = await BuscarCategoriaAsync(solicitud.CategoriaId, cancelacion);

        if (categoria.Tipo == TipoCategoria.Gasto)
        {
            throw new ExcepcionDominio(
                $"«{categoria.Nombre}» es una categoría de gasto y no puede usarse en un "
                + "ingreso recurrente.");
        }

        var cuenta = await BuscarCuentaAsync(solicitud.CuentaId, cancelacion);

        ingreso.Nombre = nombre;
        ingreso.CategoriaId = categoria.Id;
        ingreso.CuentaId = cuenta.Id;
        ingreso.MontoEstimado = solicitud.MontoEstimado;
        ingreso.Moneda = ResolverMoneda(solicitud.Moneda, cuenta.Moneda);
        ingreso.EsMontoFijo = solicitud.EsMontoFijo;
        ingreso.Frecuencia = LeerFrecuencia(solicitud.Frecuencia);
        ingreso.ProximaFechaCobro = solicitud.ProximaFechaCobro;
        ingreso.RecibidoPorUsuarioId = solicitud.RecibidoPorUsuarioId;
        ingreso.Notas = solicitud.Notas?.Trim();
    }

    /// <summary>Comprueba que el nombre no esta vacio.</summary>
    /// <param name="nombre">Nombre recibido.</param>
    /// <returns>El nombre sin espacios sobrantes.</returns>
    private static string ValidarNombre(string? nombre)
    {
        var limpio = nombre?.Trim();

        return string.IsNullOrWhiteSpace(limpio)
            ? throw new ExcepcionDominio("Hace falta un nombre.")
            : limpio;
    }

    /// <summary>Busca una categoria del espacio activo.</summary>
    /// <param name="categoriaId">Categoria indicada.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La categoria.</returns>
    /// <remarks>
    /// Si fuera de otro espacio, el filtro global hace que no aparezca y el resultado es
    /// "no encontrada": nunca se confirma que exista en otro hogar.
    /// </remarks>
    private async Task<Categoria> BuscarCategoriaAsync(
        Guid categoriaId,
        CancellationToken cancelacion) =>
        await contexto.Categorias.FirstOrDefaultAsync(c => c.Id == categoriaId, cancelacion)
        ?? throw new ExcepcionNoEncontrado("la categoría");

    /// <summary>Busca una cuenta del espacio activo.</summary>
    /// <param name="cuentaId">Cuenta indicada.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La cuenta.</returns>
    private async Task<Cuenta> BuscarCuentaAsync(Guid cuentaId, CancellationToken cancelacion) =>
        await contexto.Cuentas.FirstOrDefaultAsync(c => c.Id == cuentaId, cancelacion)
        ?? throw new ExcepcionNoEncontrado("la cuenta");

    /// <summary>Usa la moneda indicada o, si falta, la de la cuenta.</summary>
    /// <param name="indicada">Moneda del cliente.</param>
    /// <param name="deLaCuenta">Moneda de la cuenta.</param>
    /// <returns>El codigo ISO-4217 a usar.</returns>
    private static string ResolverMoneda(string? indicada, string deLaCuenta) =>
        string.IsNullOrWhiteSpace(indicada) ? deLaCuenta : indicada.Trim().ToUpperInvariant();
}
