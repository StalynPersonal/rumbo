using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Calculadoras;
using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Monedas;
using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Monedas;

/// <summary>
/// Catalogo de monedas y tasas de cambio.
/// </summary>
/// <remarks>
/// <para>
/// Sin este servicio el sistema es monomoneda de hecho: el conversor necesita tasas y no
/// habria forma de cargarlas, asi que registrar un gasto en otra divisa fallaria siempre.
/// </para>
/// <para>
/// Son tablas GLOBALES. Cualquier miembro autenticado puede consultarlas, pero solo quien
/// administra un espacio puede registrar tasas: una cotizacion equivocada distorsiona los
/// informes de todo el hogar.
/// </para>
/// </remarks>
/// <param name="contexto">Acceso a los datos.</param>
/// <param name="conversor">Conversor de importes.</param>
/// <param name="fechaHora">Proveedor de fecha y hora.</param>
public class ServicioMonedas(
    IContextoRumbo contexto,
    ConversorMonedas conversor,
    IProveedorFechaHora fechaHora) : IServicioMonedas
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<MonedaDto>> ListarMonedasAsync(
        CancellationToken cancelacion = default) =>
        await contexto.Monedas
            .AsNoTracking()
            .OrderBy(m => m.Codigo)
            .Select(m => new MonedaDto(m.Codigo, m.Nombre, m.Simbolo, m.Decimales, m.Activa))
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<TasaCambioDto>> ListarTasasAsync(
        string monedaOrigen,
        string monedaDestino,
        DateOnly? desde = null,
        DateOnly? hasta = null,
        CancellationToken cancelacion = default)
    {
        var origen = Normalizar(monedaOrigen);
        var destino = Normalizar(monedaDestino);

        var consulta = contexto.TasasCambio
            .AsNoTracking()
            .Where(t => t.MonedaOrigen == origen && t.MonedaDestino == destino);

        if (desde.HasValue)
        {
            consulta = consulta.Where(t => t.Fecha >= desde.Value);
        }

        if (hasta.HasValue)
        {
            consulta = consulta.Where(t => t.Fecha <= hasta.Value);
        }

        return await consulta
            .OrderByDescending(t => t.Fecha)
            .Select(t => new TasaCambioDto(
                t.Id, t.MonedaOrigen, t.MonedaDestino, t.Fecha, t.Tasa, t.Origen.ToString()))
            .ToListAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<TasaCambioDto> GuardarTasaAsync(
        SolicitudGuardarTasa solicitud,
        CancellationToken cancelacion = default)
    {
        var origen = Normalizar(solicitud.MonedaOrigen);
        var destino = Normalizar(solicitud.MonedaDestino);

        if (origen == destino)
        {
            throw new ExcepcionDominio(
                "El origen y el destino deben ser monedas distintas. Una moneda siempre "
                + "equivale a sí misma.");
        }

        if (solicitud.Tasa <= 0m)
        {
            throw new ExcepcionDominio("La tasa debe ser mayor que cero.");
        }

        await VerificarMonedaAsync(origen, cancelacion);
        await VerificarMonedaAsync(destino, cancelacion);

        var existente = await contexto.TasasCambio.FirstOrDefaultAsync(
            t => t.MonedaOrigen == origen
                 && t.MonedaDestino == destino
                 && t.Fecha == solicitud.Fecha,
            cancelacion);

        if (existente is not null)
        {
            // Se actualiza en lugar de crear otra: dos valores para el mismo dia harian que
            // el mismo movimiento se convirtiera distinto segun cual se leyera.
            existente.Tasa = solicitud.Tasa;
            existente.Origen = OrigenTasaCambio.Manual;

            await contexto.SaveChangesAsync(cancelacion);

            return Proyectar(existente);
        }

        var tasa = new TasaCambio
        {
            MonedaOrigen = origen,
            MonedaDestino = destino,
            Fecha = solicitud.Fecha,
            Tasa = solicitud.Tasa,
            Origen = OrigenTasaCambio.Manual,
            FechaCreacion = fechaHora.AhoraUtc,
        };

        contexto.TasasCambio.Add(tasa);
        await contexto.SaveChangesAsync(cancelacion);

        return Proyectar(tasa);
    }

    /// <inheritdoc />
    public async Task EliminarTasaAsync(Guid tasaId, CancellationToken cancelacion = default)
    {
        var tasa = await contexto.TasasCambio
            .FirstOrDefaultAsync(t => t.Id == tasaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la tasa de cambio");

        // Los movimientos ya registrados no se ven afectados: guardan su importe convertido
        // y la tasa aplicada, congelados en el momento del registro.
        contexto.TasasCambio.Remove(tasa);
        await contexto.SaveChangesAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<ResultadoConversionDto> ConvertirAsync(
        decimal monto,
        string monedaOrigen,
        string monedaDestino,
        DateOnly fecha,
        CancellationToken cancelacion = default)
    {
        var origen = Normalizar(monedaOrigen);
        var destino = Normalizar(monedaDestino);

        var resultado = await conversor.ConvertirAsync(monto, origen, destino, fecha, cancelacion);

        return new ResultadoConversionDto(
            monto, origen, resultado.Monto, destino, resultado.Tasa, fecha, resultado.EsAproximada);
    }

    /// <summary>Deja el codigo en mayusculas y sin espacios.</summary>
    /// <param name="codigo">Codigo tal como llego.</param>
    /// <returns>El codigo normalizado.</returns>
    private static string Normalizar(string codigo) =>
        (codigo ?? string.Empty).Trim().ToUpperInvariant();

    /// <summary>Comprueba que la moneda existe en el catalogo.</summary>
    /// <param name="codigo">Codigo normalizado.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando termina la comprobacion.</returns>
    private async Task VerificarMonedaAsync(string codigo, CancellationToken cancelacion)
    {
        if (!await contexto.Monedas.AnyAsync(m => m.Codigo == codigo, cancelacion))
        {
            throw new ExcepcionDominio($"La moneda {codigo} no existe en el catálogo.");
        }
    }

    /// <summary>Convierte la entidad en su DTO.</summary>
    /// <param name="tasa">Tasa guardada.</param>
    /// <returns>El DTO.</returns>
    private static TasaCambioDto Proyectar(TasaCambio tasa) =>
        new(tasa.Id, tasa.MonedaOrigen, tasa.MonedaDestino, tasa.Fecha, tasa.Tasa,
            tasa.Origen.ToString());
}
