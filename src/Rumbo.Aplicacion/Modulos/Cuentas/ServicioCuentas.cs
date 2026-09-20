using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Calculadoras;
using Rumbo.Aplicacion.Comun;
using Rumbo.Aplicacion.Contratos;
using Rumbo.Contratos.Cuentas;
using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Cuentas;

/// <summary>
/// Gestion de las cuentas del espacio activo.
/// </summary>
/// <remarks>
/// El aislamiento entre espacios lo garantizan los filtros globales del contexto: este
/// servicio no filtra por espacio a mano, y aun asi ninguna consulta puede devolver una
/// cuenta ajena.
/// </remarks>
/// <param name="contexto">Acceso a los datos.</param>
/// <param name="calculadora">Calculo del saldo real desde el libro mayor.</param>
/// <param name="conversor">Conversion a la moneda base del espacio.</param>
/// <param name="contextoEspacio">Espacio activo.</param>
/// <param name="fechaHora">Proveedor de fecha y hora.</param>
public partial class ServicioCuentas(
    IContextoRumbo contexto,
    CalculadoraSaldos calculadora,
    ConversorMonedas conversor,
    IContextoEspacio contextoEspacio,
    IProveedorFechaHora fechaHora) : IServicioCuentas
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CuentaResumen>> ListarAsync(
        bool incluirInactivas = false,
        CancellationToken cancelacion = default)
    {
        var consulta = contexto.Cuentas.AsNoTracking();

        if (!incluirInactivas)
        {
            consulta = consulta.Where(c => c.Activa);
        }

        var cuentas = await consulta
            .OrderBy(c => c.Orden)
            .ThenBy(c => c.Nombre)
            .ToListAsync(cancelacion);

        var monedaBase = await ObtenerMonedaBaseAsync(cancelacion);
        var hoy = await ObtenerHoyAsync(cancelacion);

        var resultado = new List<CuentaResumen>(cuentas.Count);

        foreach (var cuenta in cuentas)
        {
            resultado.Add(await ProyectarAsync(cuenta, monedaBase, hoy, cancelacion));
        }

        return resultado;
    }

    /// <inheritdoc />
    public async Task<CuentaResumen> ObtenerAsync(
        Guid cuentaId,
        CancellationToken cancelacion = default)
    {
        var cuenta = await contexto.Cuentas
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cuentaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la cuenta");

        return await ProyectarAsync(
            cuenta,
            await ObtenerMonedaBaseAsync(cancelacion),
            await ObtenerHoyAsync(cancelacion),
            cancelacion);
    }

    /// <inheritdoc />
    public async Task<ResultadoReconciliacion> ReconciliarAsync(
        Guid cuentaId,
        bool corregir = false,
        CancellationToken cancelacion = default)
    {
        var cuenta = await contexto.Cuentas
            .FirstOrDefaultAsync(c => c.Id == cuentaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la cuenta");

        var (saldoCalculado, cantidad) = await calculadora.CalcularAsync(
            cuentaId, hasta: null, cancelacion);

        var saldoRegistrado = cuenta.SaldoActual;
        var desviacion = saldoRegistrado - saldoCalculado;

        var seCorrige = corregir && desviacion != 0m;

        if (seCorrige)
        {
            cuenta.SaldoActual = saldoCalculado;
            await contexto.SaveChangesAsync(cancelacion);
        }

        return new ResultadoReconciliacion(
            cuentaId, saldoRegistrado, saldoCalculado, desviacion, seCorrige, cantidad);
    }

    /// <summary>Convierte la entidad en su DTO, con el saldo en moneda base.</summary>
    private async Task<CuentaResumen> ProyectarAsync(
        Cuenta cuenta,
        string monedaBase,
        DateOnly hoy,
        CancellationToken cancelacion)
    {
        decimal saldoEnBase;

        if (string.Equals(cuenta.Moneda, monedaBase, StringComparison.OrdinalIgnoreCase))
        {
            saldoEnBase = cuenta.SaldoActual;
        }
        else
        {
            try
            {
                var conversion = await conversor.ConvertirAsync(
                    cuenta.SaldoActual, cuenta.Moneda, monedaBase, hoy, cancelacion);

                saldoEnBase = conversion.Monto;
            }
            catch (ExcepcionDominio)
            {
                // Si falta la tasa, se muestra la cuenta igualmente con su saldo en la
                // moneda propia y un cero en moneda base. Impedir ver las cuentas porque
                // falta una cotizacion seria desproporcionado.
                saldoEnBase = 0m;
            }
        }

        // En una tarjeta, el saldo es deuda: el disponible es el limite menos lo debido.
        decimal? disponible = cuenta.Tipo == TipoCuenta.TarjetaCredito && cuenta.LimiteCredito.HasValue
            ? cuenta.LimiteCredito.Value + cuenta.SaldoActual
            : null;

        return new CuentaResumen(
            cuenta.Id,
            cuenta.Nombre,
            cuenta.Tipo.ToString(),
            cuenta.Moneda,
            cuenta.SaldoActual,
            saldoEnBase,
            cuenta.PropietarioUsuarioId,
            cuenta.EsCompartida,
            cuenta.Activa,
            cuenta.Institucion,
            cuenta.UltimosDigitos,
            cuenta.LimiteCredito,
            disponible,
            cuenta.Orden);
    }

    /// <summary>Moneda en la que se consolidan los informes del espacio.</summary>
    private async Task<string> ObtenerMonedaBaseAsync(CancellationToken cancelacion)
    {
        var espacioId = contextoEspacio.ObtenerEspacioObligatorio();

        return await contexto.Espacios
            .AsNoTracking()
            .Where(e => e.Id == espacioId)
            .Select(e => e.MonedaBase)
            .FirstOrDefaultAsync(cancelacion) ?? "DOP";
    }

    /// <summary>Fecha de hoy en la zona horaria del espacio.</summary>
    private async Task<DateOnly> ObtenerHoyAsync(CancellationToken cancelacion)
    {
        var espacioId = contextoEspacio.ObtenerEspacioObligatorio();

        var zona = await contexto.Espacios
            .AsNoTracking()
            .Where(e => e.Id == espacioId)
            .Select(e => e.ZonaHoraria)
            .FirstOrDefaultAsync(cancelacion);

        return fechaHora.HoyEn(zona ?? "America/Santo_Domingo");
    }
}
