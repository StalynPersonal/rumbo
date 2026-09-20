using Rumbo.Aplicacion.Calculadoras;

namespace Rumbo.PruebasUnitarias.Dominio;

// El calculo que convierte a Rumbo en un asistente y no en un registro de gastos.
//
// El primer caso es EL EJEMPLO DE REFERENCIA del proyecto: meta de RD$180,000, ahorrados
// RD$30,000, quince meses por delante, aporte recomendado RD$10,000 al mes.
public class PruebasProyeccionDeMetas
{
    private static readonly DateOnly Hoy = new(2026, 9, 20);

    [Fact]
    public void ElEjemploDeReferenciaDaDiezMilAlMes()
    {
        // 180.000 - 30.000 = 150.000 a repartir en 15 meses.
        var proyeccion = CalculadoraMetas.Proyectar(
            montoObjetivo: 180_000m,
            montoActual: 30_000m,
            fechaObjetivo: new DateOnly(2027, 12, 20),
            hoy: Hoy);

        Assert.Equal(150_000m, proyeccion.MontoFaltante);
        Assert.Equal(15, proyeccion.MesesRestantes);
        Assert.Equal(10_000m, proyeccion.AporteMensualNecesario);
    }

    [Fact]
    public void ElPorcentajeCompletadoRefejaElAvanceReal()
    {
        var proyeccion = CalculadoraMetas.Proyectar(
            180_000m, 50_000m, new DateOnly(2027, 12, 20), Hoy);

        // 50.000 de 180.000 es el 27,78 %, el 28 % que muestra el panel.
        Assert.Equal(27.78m, proyeccion.PorcentajeCompletado);
    }

    [Fact]
    public void UnaMetaAlcanzadaNoPideMasAportes()
    {
        var proyeccion = CalculadoraMetas.Proyectar(
            100_000m, 100_000m, new DateOnly(2027, 1, 1), Hoy);

        Assert.True(proyeccion.EstaAlcanzada);
        Assert.Equal(0m, proyeccion.MontoFaltante);
        Assert.Equal(0m, proyeccion.AporteMensualNecesario);
        Assert.False(proyeccion.PlazoVencido);
    }

    [Fact]
    public void AhorrarDeMasNoDaUnFaltanteNegativo()
    {
        var proyeccion = CalculadoraMetas.Proyectar(
            100_000m, 130_000m, new DateOnly(2027, 1, 1), Hoy);

        Assert.Equal(0m, proyeccion.MontoFaltante);
        Assert.Equal(100m, proyeccion.PorcentajeCompletado);
    }

    [Fact]
    public void UnPlazoVencidoSeSenalaSinDividirEntreCero()
    {
        // La fecha paso y no se alcanzo. Repartir entre un numero negativo de meses daria
        // un aporte negativo, que no significa nada.
        var proyeccion = CalculadoraMetas.Proyectar(
            100_000m, 40_000m, new DateOnly(2026, 6, 1), Hoy);

        Assert.True(proyeccion.PlazoVencido);
        Assert.Equal(60_000m, proyeccion.MontoFaltante);

        // Todo lo que falta, de golpe: no queda tiempo que repartir.
        Assert.Equal(60_000m, proyeccion.AporteMensualNecesario);
    }

    [Fact]
    public void UnaMetaSinFechaNoInventaUnaUrgencia()
    {
        var proyeccion = CalculadoraMetas.Proyectar(
            100_000m, 20_000m, fechaObjetivo: null, hoy: Hoy);

        Assert.True(proyeccion.SinFechaObjetivo);
        Assert.Null(proyeccion.AporteMensualNecesario);
        Assert.Null(proyeccion.MesesRestantes);
        Assert.Equal(80_000m, proyeccion.MontoFaltante);
    }

    [Fact]
    public void ConMenosDeUnMesPorDelanteSeRepartenEnUno()
    {
        // Quedan 10 dias: 0 meses completos. Repartir entre cero es imposible, asi que se
        // considera un mes, que es lo que queda.
        var proyeccion = CalculadoraMetas.Proyectar(
            100_000m, 90_000m, new DateOnly(2026, 9, 30), Hoy);

        Assert.Equal(0, proyeccion.MesesRestantes);
        Assert.Equal(10_000m, proyeccion.AporteMensualNecesario);
    }

    [Fact]
    public void ElAporteSemanalUsaLasSemanasRealesYNoCuatroPorMes()
    {
        // 28 dias son 4 semanas exactas: 40.000 entre 4 son 10.000 por semana.
        var proyeccion = CalculadoraMetas.Proyectar(
            40_000m, 0m, Hoy.AddDays(28), Hoy);

        Assert.Equal(10_000m, proyeccion.AporteSemanalNecesario);
    }

    [Theory]
    [InlineData(2026, 9, 20, 2026, 10, 20, 1)]
    [InlineData(2026, 9, 20, 2026, 10, 19, 0)]
    [InlineData(2026, 9, 20, 2027, 9, 20, 12)]
    [InlineData(2026, 9, 20, 2026, 9, 20, 0)]
    [InlineData(2026, 9, 20, 2026, 1, 1, 0)]
    public void LosMesesCompletosSeCuentanSinRedondearAlAlza(
        int anioD, int mesD, int diaD, int anioH, int mesH, int diaH, int esperado)
    {
        // Redondear al alza haria creer que hay mas tiempo del que queda, y el aporte
        // calculado se quedaria corto justo cuando mas importa.
        var meses = CalculadoraMetas.MesesCompletosEntre(
            new DateOnly(anioD, mesD, diaD), new DateOnly(anioH, mesH, diaH));

        Assert.Equal(esperado, meses);
    }
}
