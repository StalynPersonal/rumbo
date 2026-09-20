using Microsoft.EntityFrameworkCore;

using Rumbo.Contratos.Cuentas;
using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Cuentas;

/// <summary>
/// Parte del servicio de cuentas que crea, modifica y da de baja.
/// </summary>
public partial class ServicioCuentas
{
    /// <inheritdoc />
    public async Task<CuentaResumen> CrearAsync(
        SolicitudCrearCuenta solicitud,
        CancellationToken cancelacion = default)
    {
        var nombre = solicitud.Nombre?.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ExcepcionDominio("La cuenta necesita un nombre.");
        }

        if (!Enum.TryParse<TipoCuenta>(solicitud.Tipo, ignoreCase: true, out var tipo))
        {
            throw new ExcepcionDominio(
                "El tipo debe ser Bancaria, TarjetaCredito, Efectivo, Ahorro, Inversion u Otra.");
        }

        var moneda = (solicitud.Moneda ?? string.Empty).Trim().ToUpperInvariant();

        await VerificarMonedaAsync(moneda, cancelacion);
        ValidarDatosDeTarjeta(tipo, solicitud.LimiteCredito, solicitud.DiaCorte, solicitud.DiaPago);

        var cuenta = new Cuenta
        {
            Nombre = nombre,
            Tipo = tipo,
            Moneda = moneda,
            SaldoInicial = solicitud.SaldoInicial,

            // El saldo arranca igual que el inicial: todavia no hay ningun movimiento.
            SaldoActual = solicitud.SaldoInicial,

            PropietarioUsuarioId = solicitud.PropietarioUsuarioId,
            EsCompartida = solicitud.EsCompartida,
            Activa = true,
            Institucion = solicitud.Institucion?.Trim(),
            UltimosDigitos = solicitud.UltimosDigitos?.Trim(),
            Notas = solicitud.Notas?.Trim(),
            LimiteCredito = solicitud.LimiteCredito,
            DiaCorte = solicitud.DiaCorte,
            DiaPago = solicitud.DiaPago,
            Orden = await SiguienteOrdenAsync(cancelacion),
        };

        contexto.Cuentas.Add(cuenta);
        await contexto.SaveChangesAsync(cancelacion);

        return await ObtenerAsync(cuenta.Id, cancelacion);
    }

    /// <inheritdoc />
    public async Task<CuentaResumen> ActualizarAsync(
        Guid cuentaId,
        SolicitudActualizarCuenta solicitud,
        CancellationToken cancelacion = default)
    {
        var cuenta = await contexto.Cuentas
            .FirstOrDefaultAsync(c => c.Id == cuentaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la cuenta");

        var nombre = solicitud.Nombre?.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ExcepcionDominio("La cuenta necesita un nombre.");
        }

        ValidarDatosDeTarjeta(
            cuenta.Tipo, solicitud.LimiteCredito, solicitud.DiaCorte, solicitud.DiaPago);

        // Ni el tipo, ni la moneda, ni el saldo inicial se tocan. Los movimientos ya
        // registrados dependen de ellos: cambiar la moneda de una cuenta con historial
        // reinterpretaria importes pasados, y cambiar el saldo inicial descuadraria el
        // libro mayor entero.
        cuenta.Nombre = nombre;
        cuenta.PropietarioUsuarioId = solicitud.PropietarioUsuarioId;
        cuenta.EsCompartida = solicitud.EsCompartida;
        cuenta.Activa = solicitud.Activa;
        cuenta.Institucion = solicitud.Institucion?.Trim();
        cuenta.UltimosDigitos = solicitud.UltimosDigitos?.Trim();
        cuenta.Notas = solicitud.Notas?.Trim();
        cuenta.LimiteCredito = solicitud.LimiteCredito;
        cuenta.DiaCorte = solicitud.DiaCorte;
        cuenta.DiaPago = solicitud.DiaPago;
        cuenta.Orden = solicitud.Orden;

        await contexto.SaveChangesAsync(cancelacion);

        return await ObtenerAsync(cuentaId, cancelacion);
    }

    /// <inheritdoc />
    public async Task EliminarAsync(Guid cuentaId, CancellationToken cancelacion = default)
    {
        var cuenta = await contexto.Cuentas
            .FirstOrDefaultAsync(c => c.Id == cuentaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("la cuenta");

        var tieneMovimientos = await contexto.Movimientos
            .AnyAsync(m => m.CuentaId == cuentaId, cancelacion);

        if (tieneMovimientos)
        {
            // Borrarla dejaria sus movimientos apuntando a una cuenta invisible y el
            // historial sin cuadrar. Desactivarla la quita de las listas sin perder nada.
            throw new ExcepcionDominio(
                "Esta cuenta tiene movimientos registrados y no se puede eliminar. "
                + "Desactívala si ya no la usas: así deja de aparecer al registrar gastos "
                + "pero su historial se conserva.");
        }

        contexto.Cuentas.Remove(cuenta);
        await contexto.SaveChangesAsync(cancelacion);
    }

    /// <summary>Comprueba que la moneda existe y esta activa.</summary>
    private async Task VerificarMonedaAsync(string moneda, CancellationToken cancelacion)
    {
        if (moneda.Length != 3)
        {
            throw new ExcepcionDominio(
                "La moneda debe ser un código ISO-4217 de tres letras, por ejemplo DOP.");
        }

        var existe = await contexto.Monedas
            .AnyAsync(m => m.Codigo == moneda && m.Activa, cancelacion);

        if (!existe)
        {
            throw new ExcepcionDominio($"La moneda {moneda} no está disponible.");
        }
    }

    /// <summary>Valida los campos que solo tienen sentido en una tarjeta de credito.</summary>
    private static void ValidarDatosDeTarjeta(
        TipoCuenta tipo,
        decimal? limiteCredito,
        int? diaCorte,
        int? diaPago)
    {
        if (tipo != TipoCuenta.TarjetaCredito)
        {
            if (limiteCredito.HasValue || diaCorte.HasValue || diaPago.HasValue)
            {
                throw new ExcepcionDominio(
                    "El límite de crédito y los días de corte y pago solo aplican a las "
                    + "tarjetas de crédito.");
            }

            return;
        }

        if (limiteCredito is < 0)
        {
            throw new ExcepcionDominio("El límite de crédito no puede ser negativo.");
        }

        // Entre 1 y 28 porque es el ultimo dia que existe en todos los meses: admitir el 31
        // obligaria a decidir que pasa en febrero.
        if (diaCorte is < 1 or > 28 || diaPago is < 1 or > 28)
        {
            throw new ExcepcionDominio(
                "Los días de corte y de pago deben estar entre 1 y 28, para que existan en "
                + "todos los meses.");
        }
    }

    /// <summary>Devuelve el orden siguiente, para que la cuenta nueva quede al final.</summary>
    private async Task<int> SiguienteOrdenAsync(CancellationToken cancelacion) =>
        await contexto.Cuentas.AnyAsync(cancelacion)
            ? await contexto.Cuentas.MaxAsync(c => c.Orden, cancelacion) + 1
            : 0;
}
