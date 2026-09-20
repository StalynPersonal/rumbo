using Microsoft.EntityFrameworkCore;

using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Movimientos;

/// <summary>
/// Validaciones y conversiones que comparten las operaciones del libro mayor.
/// </summary>
public partial class ServicioMovimientos
{
    /// <summary>
    /// Comprueba que la categoria existe, esta activa y sirve para ese tipo de movimiento.
    /// </summary>
    /// <param name="categoriaId">Categoria indicada, o <c>null</c>.</param>
    /// <param name="tipo">Tipo del movimiento.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La categoria, o <c>null</c> si no hacia falta.</returns>
    /// <remarks>
    /// Los ajustes no se categorizan: no representan consumo ni ingreso, y meterlos en una
    /// categoria real ensuciaria los informes de gasto.
    /// </remarks>
    private async Task<Categoria?> ValidarCategoriaAsync(
        Guid? categoriaId,
        TipoMovimiento tipo,
        CancellationToken cancelacion)
    {
        if (tipo == TipoMovimiento.Ajuste)
        {
            return null;
        }

        if (!categoriaId.HasValue)
        {
            throw new ExcepcionDominio("Hay que indicar una categoría.");
        }

        // Si la categoria fuera de otro espacio, el filtro global hace que no aparezca y el
        // resultado es "no encontrada": nunca se confirma que exista en otro hogar.
        var categoria = await contexto.Categorias
            .FirstOrDefaultAsync(c => c.Id == categoriaId.Value, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la categoría");

        if (!categoria.Activa)
        {
            throw new ExcepcionDominio(
                $"La categoría «{categoria.Nombre}» está desactivada.");
        }

        var compatible = categoria.Tipo == TipoCategoria.Ambos
            || (tipo == TipoMovimiento.Ingreso && categoria.Tipo == TipoCategoria.Ingreso)
            || (tipo == TipoMovimiento.Gasto && categoria.Tipo == TipoCategoria.Gasto);

        if (!compatible)
        {
            throw new ExcepcionDominio(
                $"La categoría «{categoria.Nombre}» es de tipo {categoria.Tipo} y no puede "
                + $"usarse en un movimiento de tipo {tipo}.");
        }

        return categoria;
    }

    /// <summary>Comprueba que el viaje pertenece al espacio activo.</summary>
    /// <param name="viajeId">Viaje indicado.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando termina la comprobacion.</returns>
    private async Task VerificarQueElViajeExisteAsync(Guid viajeId, CancellationToken cancelacion)
    {
        var existe = await contexto.Viajes.AnyAsync(v => v.Id == viajeId, cancelacion);

        if (!existe)
        {
            throw new ExcepcionNoEncontrado("el viaje");
        }
    }

    /// <summary>Interpreta el tipo de reparto recibido.</summary>
    /// <param name="valor">Texto del cliente.</param>
    /// <returns>El valor de la enumeracion.</returns>
    private static TipoReparto LeerReparto(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return TipoReparto.Personal;
        }

        return Enum.TryParse<TipoReparto>(valor, ignoreCase: true, out var reparto)
            ? reparto
            : throw new ExcepcionDominio("El reparto debe ser Personal o Compartido.");
    }

    /// <summary>Interpreta el metodo de pago recibido.</summary>
    /// <param name="valor">Texto del cliente.</param>
    /// <returns>El valor de la enumeracion, o <c>null</c> si no se indico.</returns>
    private static MetodoPago? LeerMetodoPago(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        return Enum.TryParse<MetodoPago>(valor, ignoreCase: true, out var metodo)
            ? metodo
            : throw new ExcepcionDominio("El método de pago indicado no es válido.");
    }

    /// <summary>
    /// Convierte un importe a la moneda base del espacio con la tasa de esa fecha.
    /// </summary>
    /// <param name="monto">Importe original.</param>
    /// <param name="moneda">Moneda del movimiento.</param>
    /// <param name="fecha">Fecha contable.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El importe en moneda base y si la tasa fue aproximada.</returns>
    /// <remarks>
    /// El resultado se CONGELA en el movimiento. Los informes suman por ese campo, de modo
    /// que un mes ya cerrado no cambia de resultado si manana se mueve el tipo de cambio.
    /// </remarks>
    private async Task<(decimal MontoEnBase, bool Aproximada)> ConvertirAMonedaBaseAsync(
        decimal monto,
        string moneda,
        DateOnly fecha,
        CancellationToken cancelacion)
    {
        var monedaBase = await ObtenerMonedaBaseAsync(cancelacion);

        if (string.Equals(moneda, monedaBase, StringComparison.OrdinalIgnoreCase))
        {
            return (monto, false);
        }

        var conversion = await conversor.ConvertirAsync(
            monto, moneda, monedaBase, fecha, cancelacion);

        return (conversion.Monto, conversion.EsAproximada);
    }

    /// <summary>Moneda en la que se consolidan los informes del espacio.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El codigo ISO-4217.</returns>
    private async Task<string> ObtenerMonedaBaseAsync(CancellationToken cancelacion)
    {
        var espacioId = contextoEspacio.ObtenerEspacioObligatorio();

        return await contexto.Espacios
            .AsNoTracking()
            .Where(e => e.Id == espacioId)
            .Select(e => e.MonedaBase)
            .FirstOrDefaultAsync(cancelacion) ?? "DOP";
    }
}
