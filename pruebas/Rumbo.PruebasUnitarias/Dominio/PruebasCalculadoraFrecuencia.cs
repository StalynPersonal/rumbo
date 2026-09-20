using Rumbo.Aplicacion.Modulos.Recurrentes;
using Rumbo.Dominio.Enums;

namespace Rumbo.PruebasUnitarias.Dominio;

// El calculo del siguiente vencimiento parece trivial hasta que aparecen los meses cortos.
public class PruebasCalculadoraFrecuencia
{
    [Fact]
    public void UnPagoMensualAvanzaAlMesSiguiente()
    {
        var siguiente = CalculadoraFrecuencia.Siguiente(
            new DateOnly(2026, 9, 15), Frecuencia.Mensual);

        Assert.Equal(new DateOnly(2026, 10, 15), siguiente);
    }

    [Fact]
    public void UnPagoDel31DeEneroCaeEl28DeFebrero()
    {
        // El 31 de febrero no existe. AddMonths ajusta al ultimo dia del mes, que es el
        // comportamiento correcto y el mismo que aplican los bancos.
        var siguiente = CalculadoraFrecuencia.Siguiente(
            new DateOnly(2026, 1, 31), Frecuencia.Mensual);

        Assert.Equal(new DateOnly(2026, 2, 28), siguiente);
    }

    [Fact]
    public void EnAnioBisiestoElAjusteLlegaAl29()
    {
        // 2028 es bisiesto.
        var siguiente = CalculadoraFrecuencia.Siguiente(
            new DateOnly(2028, 1, 31), Frecuencia.Mensual);

        Assert.Equal(new DateOnly(2028, 2, 29), siguiente);
    }

    [Theory]
    [InlineData(Frecuencia.Semanal, 7)]
    [InlineData(Frecuencia.Quincenal, 15)]
    public void LasFrecuenciasEnDiasAvanzanLoEsperado(Frecuencia frecuencia, int dias)
    {
        var inicio = new DateOnly(2026, 9, 1);

        Assert.Equal(inicio.AddDays(dias), CalculadoraFrecuencia.Siguiente(inicio, frecuencia));
    }

    [Fact]
    public void UnSeguroAnualPesaLoMismoAlMesQueUnServicioMensualEquivalente()
    {
        // Es lo que permite al motor de recomendaciones comparar obligaciones de
        // periodicidad distinta sin engañarse.
        var anual = CalculadoraFrecuencia.EquivalenteMensual(12_000m, Frecuencia.Anual);
        var mensual = CalculadoraFrecuencia.EquivalenteMensual(1_000m, Frecuencia.Mensual);

        Assert.Equal(1_000m, anual);
        Assert.Equal(mensual, anual);
    }

    [Fact]
    public void UnPagoSemanalEquivaleACuatroYPicoAlMes()
    {
        // 52 semanas al año entre 12 meses: 4,333... pagos mensuales. Redondear a 4 seria
        // subestimar el gasto anual en casi un mes de recibo.
        var mensual = CalculadoraFrecuencia.EquivalenteMensual(1_000m, Frecuencia.Semanal);

        Assert.Equal(4_333.3333m, mensual);
    }
}
