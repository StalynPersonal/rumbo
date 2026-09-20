using Rumbo.Dominio.Entidades.Planificacion;

namespace Rumbo.PruebasUnitarias.Dominio;

// Los calculos de progreso son de los pocos que viven en el propio dominio, porque son
// aritmetica pura sobre los datos de la entidad. Se prueban aqui porque un error de division
// o de redondeo se traduce en una barra de progreso que miente al usuario.
public class PruebasCalculosDeMeta
{
    private static Meta CrearMeta(decimal objetivo, decimal actual) => new()
    {
        Nombre = "Viaje a Colombia",
        Moneda = "DOP",
        MontoObjetivo = objetivo,
        MontoActual = actual,
    };

    [Fact]
    public void ElMontoFaltanteEsLaDiferenciaEntreObjetivoYAhorrado()
    {
        var meta = CrearMeta(objetivo: 180_000m, actual: 30_000m);

        Assert.Equal(150_000m, meta.MontoFaltante);
    }

    [Fact]
    public void ElMontoFaltanteNuncaEsNegativoAunqueSeSupereElObjetivo()
    {
        // Ahorrar de mas es posible y no debe mostrarse como "te faltan -5,000".
        var meta = CrearMeta(objetivo: 100_000m, actual: 105_000m);

        Assert.Equal(0m, meta.MontoFaltante);
    }

    [Fact]
    public void ElPorcentajeCompletadoRefejaLaProporcionAhorrada()
    {
        var meta = CrearMeta(objetivo: 180_000m, actual: 50_000m);

        // 50.000 / 180.000 = 27,77...%. Es el 28 por ciento que muestra el panel.
        Assert.Equal(27.78m, Math.Round(meta.PorcentajeCompletado, 2));
    }

    [Fact]
    public void ElPorcentajeCompletadoSeLimitaA100()
    {
        var meta = CrearMeta(objetivo: 100_000m, actual: 150_000m);

        Assert.Equal(100m, meta.PorcentajeCompletado);
    }

    [Fact]
    public void UnObjetivoDeCeroNoProvocaDivisionPorCero()
    {
        // No deberia poder crearse (hay una restriccion CHECK en la base de datos), pero el
        // calculo no debe reventar si llega una meta mal formada desde una version antigua.
        var meta = CrearMeta(objetivo: 0m, actual: 0m);

        Assert.Equal(100m, meta.PorcentajeCompletado);
        Assert.Equal(0m, meta.MontoFaltante);
    }

    [Fact]
    public void UnaMetaSinNadaAhorradoEstaAlCeroPorCiento()
    {
        var meta = CrearMeta(objetivo: 120_000m, actual: 0m);

        Assert.Equal(0m, meta.PorcentajeCompletado);
        Assert.Equal(120_000m, meta.MontoFaltante);
    }
}
