using Microsoft.EntityFrameworkCore;

using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Entidades.Identidad;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.PruebasIntegracion.Persistencia;

// LA PRUEBA MAS IMPORTANTE DEL SISTEMA.
//
// Verifica que un usuario del Espacio A no puede, bajo ninguna circunstancia, ver ni tocar
// datos del Espacio B. Es un requisito explicito del proyecto y la primera prioridad cuando
// dos requisitos entran en conflicto.
//
// Se ejecuta contra SQL Server real, con el esquema real generado por las migraciones. Una
// version en memoria no demostraria nada: no aplica restricciones ni se comporta como la
// base de produccion.
public class PruebasAislamientoEspacio : IAsyncLifetime
{
    private readonly BaseDeDatosDePrueba _baseDatos = new();

    private readonly Guid _espacioA = Guid.CreateVersion7();
    private readonly Guid _espacioB = Guid.CreateVersion7();

    private Guid _cuentaDelEspacioA;
    private Guid _cuentaDelEspacioB;
    private Guid _movimientoDelEspacioB;

    public async Task InitializeAsync()
    {
        await _baseDatos.CrearAsync();
        await SembrarDosEspaciosAsync();
    }

    public async Task DisposeAsync() => await _baseDatos.DisposeAsync();

    /// <summary>
    /// Crea dos hogares completos e independientes, cada uno con su cuenta y su movimiento.
    /// </summary>
    private async Task SembrarDosEspaciosAsync()
    {
        await using var contexto = _baseDatos.CrearContextoDeSiembra();

        contexto.Espacios.AddRange(
            new Espacio
            {
                Id = _espacioA,
                Nombre = "Hogar de Juan y Maria",
                Tipo = TipoEspacio.Pareja,
                MonedaBase = "DOP",
                ZonaHoraria = "America/Santo_Domingo",
            },
            new Espacio
            {
                Id = _espacioB,
                Nombre = "Hogar de Pedro",
                Tipo = TipoEspacio.Personal,
                MonedaBase = "DOP",
                ZonaHoraria = "America/Santo_Domingo",
            });

        var cuentaA = new Cuenta
        {
            EspacioId = _espacioA,
            Nombre = "Cuenta de nomina de Juan",
            Tipo = TipoCuenta.Bancaria,
            Moneda = "DOP",
            SaldoInicial = 50_000m,
            SaldoActual = 50_000m,
        };

        var cuentaB = new Cuenta
        {
            EspacioId = _espacioB,
            Nombre = "Cuenta de nomina de Pedro",
            Tipo = TipoCuenta.Bancaria,
            Moneda = "DOP",
            SaldoInicial = 80_000m,
            SaldoActual = 80_000m,
        };

        contexto.Cuentas.AddRange(cuentaA, cuentaB);

        var movimientoB = new Movimiento
        {
            EspacioId = _espacioB,
            Cuenta = cuentaB,
            Tipo = TipoMovimiento.Gasto,
            Signo = -1,
            Monto = 5_000m,
            Moneda = "DOP",
            MontoEnMonedaBase = 5_000m,
            FechaMovimiento = new DateOnly(2026, 9, 15),
            Descripcion = "Supermercado de Pedro",
        };

        contexto.Movimientos.Add(movimientoB);

        await contexto.SaveChangesAsync();

        _cuentaDelEspacioA = cuentaA.Id;
        _cuentaDelEspacioB = cuentaB.Id;
        _movimientoDelEspacioB = movimientoB.Id;
    }

    [Fact]
    public async Task UnEspacioSoloVeSusPropiasCuentas()
    {
        await using var contexto = _baseDatos.CrearContexto(_espacioA);

        var cuentas = await contexto.Cuentas.ToListAsync();

        Assert.Single(cuentas);
        Assert.Equal(_cuentaDelEspacioA, cuentas[0].Id);
    }

    [Fact]
    public async Task BuscarPorIdUnaCuentaDeOtroEspacioNoDevuelveNada()
    {
        // Este es el ataque mas simple: adivinar o filtrar un identificador y pedirlo
        // directamente. La API debe responder 404, no 403: confirmar que el registro existe
        // pero es de otro ya seria filtrar informacion.
        await using var contexto = _baseDatos.CrearContexto(_espacioA);

        var cuentaAjena = await contexto.Cuentas
            .FirstOrDefaultAsync(c => c.Id == _cuentaDelEspacioB);

        Assert.Null(cuentaAjena);
    }

    [Fact]
    public async Task BuscarPorIdUnMovimientoDeOtroEspacioNoDevuelveNada()
    {
        await using var contexto = _baseDatos.CrearContexto(_espacioA);

        var movimientoAjeno = await contexto.Movimientos
            .FirstOrDefaultAsync(m => m.Id == _movimientoDelEspacioB);

        Assert.Null(movimientoAjeno);
    }

    [Fact]
    public async Task ContarMovimientosSoloCuentaLosDelEspacioActivo()
    {
        // Las agregaciones son un punto ciego clasico: aunque no se devuelvan las filas, un
        // total mal filtrado ya revela cuanto gasta el otro hogar.
        await using var contextoA = _baseDatos.CrearContexto(_espacioA);
        await using var contextoB = _baseDatos.CrearContexto(_espacioB);

        Assert.Equal(0, await contextoA.Movimientos.CountAsync());
        Assert.Equal(1, await contextoB.Movimientos.CountAsync());

        var sumaA = await contextoA.Movimientos.SumAsync(m => (decimal?)m.Monto) ?? 0m;
        Assert.Equal(0m, sumaA);
    }

    [Fact]
    public async Task NoSePuedeCrearUnRegistroEnOtroEspacio()
    {
        // Cuarta capa del aislamiento: aunque alguien fije a mano el espacio ajeno en la
        // entidad, el interceptor debe abortar la escritura completa.
        await using var contexto = _baseDatos.CrearContexto(_espacioA);

        contexto.Cuentas.Add(new Cuenta
        {
            EspacioId = _espacioB,
            Nombre = "Cuenta infiltrada",
            Tipo = TipoCuenta.Efectivo,
            Moneda = "DOP",
        });

        await Assert.ThrowsAsync<ExcepcionEspacioNoCoincide>(
            () => contexto.SaveChangesAsync());
    }

    [Fact]
    public async Task ElEspacioSeAsignaSoloAlCrearSinIndicarlo()
    {
        await using var contexto = _baseDatos.CrearContexto(_espacioA);

        var cuenta = new Cuenta
        {
            Nombre = "Efectivo de Maria",
            Tipo = TipoCuenta.Efectivo,
            Moneda = "DOP",
        };

        contexto.Cuentas.Add(cuenta);
        await contexto.SaveChangesAsync();

        // El servicio no tuvo que acordarse de asignar el espacio: lo hizo el interceptor.
        Assert.Equal(_espacioA, cuenta.EspacioId);
    }

    [Fact]
    public async Task UnRegistroBorradoDejaDeVersePeroSigueEnLaBaseDeDatos()
    {
        await using var contexto = _baseDatos.CrearContexto(_espacioA);

        var cuenta = await contexto.Cuentas.SingleAsync(c => c.Id == _cuentaDelEspacioA);
        contexto.Cuentas.Remove(cuenta);
        await contexto.SaveChangesAsync();

        // Ya no aparece en las consultas normales...
        await using var contextoNuevo = _baseDatos.CrearContexto(_espacioA);
        Assert.Empty(await contextoNuevo.Cuentas.ToListAsync());

        // ...pero la fila sigue ahi, con su marca de borrado. Es lo que permite reconciliar
        // saldos historicos y auditar quien la elimino.
        var filaBorrada = await contextoNuevo.Cuentas
            .IgnoreQueryFilters()
            .SingleAsync(c => c.Id == _cuentaDelEspacioA);

        Assert.True(filaBorrada.Eliminado);
        Assert.NotNull(filaBorrada.FechaEliminacion);
    }
}
