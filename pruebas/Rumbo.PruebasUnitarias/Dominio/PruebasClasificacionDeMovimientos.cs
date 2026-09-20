using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Enums;

namespace Rumbo.PruebasUnitarias.Dominio;

// La regla "una transferencia no es un gasto" es un requisito explicito del sistema, y es el
// tipo de regla que se rompe sola en cuanto alguien escribe un informe nuevo. Por eso vive en
// un metodo del dominio y esta cubierta por pruebas.
public class PruebasClasificacionDeMovimientos
{
    private static Movimiento CrearMovimiento(TipoMovimiento tipo) => new()
    {
        Tipo = tipo,
        Moneda = "DOP",
        Descripcion = "Movimiento de prueba",
        Monto = 1_000m,
    };

    [Theory]
    [InlineData(TipoMovimiento.Ingreso)]
    [InlineData(TipoMovimiento.Gasto)]
    public void LosIngresosYLosGastosCuentanParaLosTotales(TipoMovimiento tipo)
    {
        var movimiento = CrearMovimiento(tipo);

        Assert.True(movimiento.CuentaParaIngresosYGastos());
    }

    [Fact]
    public void UnaTransferenciaNoCuentaComoIngresoNiComoGasto()
    {
        // Es la regla mas importante del libro mayor: mover RD$10,000 de la cuenta de nomina
        // a la de ahorro no es gastar RD$10,000. Si contara, el informe del mes seria falso.
        var transferencia = CrearMovimiento(TipoMovimiento.Transferencia);

        Assert.False(transferencia.CuentaParaIngresosYGastos());
    }

    [Fact]
    public void UnAjusteNoCuentaComoIngresoNiComoGasto()
    {
        // Un ajuste corrige el saldo para cuadrarlo con la realidad; no representa dinero que
        // haya entrado o salido del hogar.
        var ajuste = CrearMovimiento(TipoMovimiento.Ajuste);

        Assert.False(ajuste.CuentaParaIngresosYGastos());
    }
}
