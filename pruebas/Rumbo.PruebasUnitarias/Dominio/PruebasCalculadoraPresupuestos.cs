using Rumbo.Aplicacion.Calculadoras;

namespace Rumbo.PruebasUnitarias.Dominio;

// Umbrales de presupuesto. El ejemplo del proyecto: presupuesto de RD$8,000, gastados
// RD$6,800, disponible RD$1,200, un 85 %.
public class PruebasCalculadoraPresupuestos
{
    private static readonly DateOnly Inicio = new(2026, 9, 1);
    private static readonly DateOnly Fin = new(2026, 9, 30);

    private static EstadoPartida Evaluar(decimal asignado, decimal gastado, DateOnly? hoy = null) =>
        CalculadoraPresupuestos.Evaluar(
            asignado, gastado, 80m, 90m, 100m, Inicio, Fin, hoy ?? new DateOnly(2026, 9, 15));

    [Fact]
    public void ElEjemploDeReferenciaDaOchentaYCincoPorCiento()
    {
        var estado = Evaluar(asignado: 8_000m, gastado: 6_800m);

        Assert.Equal(1_200m, estado.MontoDisponible);
        Assert.Equal(85m, estado.PorcentajeConsumido);
        Assert.Equal(NivelAlertaPresupuesto.Aviso, estado.Nivel);
    }

    // Los importes van como double porque C# no admite literales decimal en un atributo.
    // Se convierten dentro: la precision de estos valores redondos es exacta en ambos tipos.
    [Theory]
    [InlineData(7_900d, NivelAlertaPresupuesto.Normal)]
    [InlineData(8_000d, NivelAlertaPresupuesto.Aviso)]
    [InlineData(9_000d, NivelAlertaPresupuesto.Critico)]
    [InlineData(10_000d, NivelAlertaPresupuesto.Excedido)]
    [InlineData(12_000d, NivelAlertaPresupuesto.Excedido)]
    public void CadaUmbralDisparaSuNivel(double gastado, NivelAlertaPresupuesto esperado)
    {
        // Sobre un presupuesto de 10.000: 79 %, 80 %, 90 %, 100 % y 120 %.
        var estado = Evaluar(asignado: 10_000m, gastado: (decimal)gastado);

        Assert.Equal(esperado, estado.Nivel);
    }

    [Fact]
    public void ExcederElPresupuestoDaUnDisponibleNegativo()
    {
        // Mostrar cero en lugar del negativo ocultaria cuanto se paso, que es justo el dato
        // que hace falta para corregir.
        var estado = Evaluar(asignado: 5_000m, gastado: 6_500m);

        Assert.Equal(-1_500m, estado.MontoDisponible);
        Assert.Equal(130m, estado.PorcentajeConsumido);
    }

    [Fact]
    public void LaProyeccionAvisaAntesDeQueSeaTarde()
    {
        // Dia 15 de 30, gastados 6.000 de 8.000: al ritmo actual el mes cerraria en 12.000.
        // Avisar ahora deja margen de reaccion; avisar el dia 28 ya no sirve de nada.
        var estado = Evaluar(asignado: 8_000m, gastado: 6_000m, hoy: new DateOnly(2026, 9, 15));

        Assert.Equal(12_000m, estado.ProyeccionAlCierre);
    }

    [Fact]
    public void ElRitmoDiarioRepartLoQueQuedaEntreLosDiasQueFaltan()
    {
        // Dia 15 de 30: quedan 15 dias y 1.500 disponibles, o sea 100 al dia.
        var estado = Evaluar(asignado: 8_000m, gastado: 6_500m, hoy: new DateOnly(2026, 9, 15));

        Assert.Equal(100m, estado.RitmoDiarioNecesario);
    }

    [Fact]
    public void UnaPartidaAgotadaNoDaUnRitmoNegativo()
    {
        var estado = Evaluar(asignado: 5_000m, gastado: 7_000m, hoy: new DateOnly(2026, 9, 15));

        Assert.Equal(0m, estado.RitmoDiarioNecesario);
    }

    [Fact]
    public void UnPresupuestoDeCeroNoProvocaDivisionPorCero()
    {
        var sinGasto = Evaluar(asignado: 0m, gastado: 0m);
        var conGasto = Evaluar(asignado: 0m, gastado: 100m);

        Assert.Equal(0m, sinGasto.PorcentajeConsumido);
        Assert.Equal(100m, conGasto.PorcentajeConsumido);
        Assert.Equal(NivelAlertaPresupuesto.Excedido, conGasto.Nivel);
    }

    [Fact]
    public void EnElUltimoDiaNoQuedaRitmoQueRepartir()
    {
        var estado = Evaluar(asignado: 8_000m, gastado: 4_000m, hoy: Fin);

        Assert.Null(estado.RitmoDiarioNecesario);
    }
}
