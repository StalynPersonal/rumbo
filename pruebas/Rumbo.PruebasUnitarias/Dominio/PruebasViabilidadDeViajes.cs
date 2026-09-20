using Rumbo.Aplicacion.Calculadoras;

namespace Rumbo.PruebasUnitarias.Dominio;

// «¿Podemos permitirnos este viaje?» es la pregunta que da sentido a toda la aplicación.
// La respuesta tiene que ser honesta: tres escenarios, no una promesa.
public class PruebasViabilidadDeViajes
{
    private static readonly DateOnly Hoy = new(2026, 9, 20);

    [Fact]
    public void ConUnExcedenteHolgadoElViajeEsViableHastaEnElEscenarioPrudente()
    {
        // Viaje de 120.000, nada ahorrado, 12 meses por delante y un excedente de 20.000
        // al mes. Hacen falta 10.000 al mes; el escenario prudente ya aporta 12.000.
        var resultado = CalculadoraViajes.Proyectar(
            costoTotal: 120_000m,
            fondoActual: 0m,
            fechaSalida: Hoy.AddMonths(12),
            hoy: Hoy,
            disponibleMensual: 20_000m,
            confianzaBaja: false,
            moneda: "DOP");

        Assert.Equal("Si", resultado.Veredicto);
        Assert.Equal(12, resultado.MesesHastaLaSalida);
        Assert.Equal(10_000m, resultado.AporteMensualNecesario);

        // El viaje se come la mitad del excedente mensual.
        Assert.Equal(50m, resultado.EsfuerzoRequerido);

        Assert.All(resultado.Escenarios, e => Assert.True(e.Alcanza));

        // Ya alcanza, así que no se propone una fecha alternativa.
        Assert.Null(resultado.FechaViableMasCercana);
    }

    [Fact]
    public void CuandoSoloSeLlegaApretandoElVeredictoEsAjustado()
    {
        // Hacen falta 10.000 al mes y sobran 12.000. El prudente aporta 7.200 y no llega;
        // el esperado aporta 10.200 y llega por poco.
        var resultado = CalculadoraViajes.Proyectar(
            costoTotal: 120_000m,
            fondoActual: 0m,
            fechaSalida: Hoy.AddMonths(12),
            hoy: Hoy,
            disponibleMensual: 12_000m,
            confianzaBaja: false,
            moneda: "DOP");

        Assert.Equal("Ajustado", resultado.Veredicto);

        Assert.False(resultado.Escenarios[0].Alcanza);
        Assert.True(resultado.Escenarios[1].Alcanza);

        // Y se dice cuándo sí se podría sin forzar nada.
        Assert.NotNull(resultado.FechaViableMasCercana);
    }

    [Fact]
    public void SiNiAhorrandoTodoSeLlegaLaRespuestaEsNo()
    {
        // 300.000 en 6 meses con 10.000 de excedente: son 50.000 al mes contra 10.000.
        var resultado = CalculadoraViajes.Proyectar(
            costoTotal: 300_000m,
            fondoActual: 0m,
            fechaSalida: Hoy.AddMonths(6),
            hoy: Hoy,
            disponibleMensual: 10_000m,
            confianzaBaja: false,
            moneda: "DOP");

        Assert.Equal("No", resultado.Veredicto);
        Assert.All(resultado.Escenarios, e => Assert.False(e.Alcanza));

        // El optimista reuniría 60.000 de 300.000: un 20 %.
        Assert.Equal(20m, resultado.Escenarios[2].PorcentajeCubierto);

        // Y se dice cuántos meses más harían falta: tras los 6 meses quedarían 240.000
        // por reunir, que a 10.000 al mes son 24 meses adicionales.
        Assert.Equal(24, resultado.Escenarios[2].MesesQueFaltarian);
    }

    [Fact]
    public void SinExcedenteMensualLaRespuestaEsNoYSeDiceElPorque()
    {
        // El peor consejo posible seria fingir que se puede ahorrar sin margen.
        var resultado = CalculadoraViajes.Proyectar(
            costoTotal: 50_000m,
            fondoActual: 0m,
            fechaSalida: Hoy.AddMonths(10),
            hoy: Hoy,
            disponibleMensual: -3_000m,
            confianzaBaja: false,
            moneda: "DOP");

        Assert.Equal("No", resultado.Veredicto);
        Assert.Contains("excedente", resultado.Explicacion, StringComparison.OrdinalIgnoreCase);

        // Sin excedente no hay fecha alternativa que ofrecer.
        Assert.Null(resultado.FechaViableMasCercana);
    }

    [Fact]
    public void ElFondoYaAhorradoCuentaYPuedeCubrirElViajeEntero()
    {
        var resultado = CalculadoraViajes.Proyectar(
            costoTotal: 80_000m,
            fondoActual: 85_000m,
            fechaSalida: Hoy.AddMonths(4),
            hoy: Hoy,
            disponibleMensual: 5_000m,
            confianzaBaja: false,
            moneda: "DOP");

        Assert.Equal("Si", resultado.Veredicto);
        Assert.Equal(0m, resultado.Faltante);
        Assert.Equal(0m, resultado.AporteMensualNecesario);
        Assert.All(resultado.Escenarios, e => Assert.Equal(100m, e.PorcentajeCubierto));
    }

    [Fact]
    public void ElFondoParcialReduceLoQueHayQueAportarCadaMes()
    {
        // 150.000 de viaje con 60.000 ya reunidos y 9 meses: faltan 90.000, o sea
        // 10.000 al mes.
        var resultado = CalculadoraViajes.Proyectar(
            costoTotal: 150_000m,
            fondoActual: 60_000m,
            fechaSalida: Hoy.AddMonths(9),
            hoy: Hoy,
            disponibleMensual: 25_000m,
            confianzaBaja: false,
            moneda: "DOP");

        Assert.Equal(90_000m, resultado.Faltante);
        Assert.Equal(10_000m, resultado.AporteMensualNecesario);
        Assert.Equal("Si", resultado.Veredicto);
    }

    [Fact]
    public void UnViajeCuyaFechaYaPasoNoEsViable()
    {
        var resultado = CalculadoraViajes.Proyectar(
            costoTotal: 50_000m,
            fondoActual: 10_000m,
            fechaSalida: Hoy.AddMonths(-2),
            hoy: Hoy,
            disponibleMensual: 30_000m,
            confianzaBaja: false,
            moneda: "DOP");

        Assert.Equal("No", resultado.Veredicto);
        Assert.True(resultado.DiasHastaLaSalida < 0);
    }

    [Fact]
    public void ConMenosDeUnMesPorDelanteSeExigeTodoDeGolpe()
    {
        // No hay ritmo que repartir: o esta el dinero o no esta. Dividir entre cero meses
        // daria un resultado sin sentido.
        var resultado = CalculadoraViajes.Proyectar(
            costoTotal: 40_000m,
            fondoActual: 5_000m,
            fechaSalida: Hoy.AddDays(12),
            hoy: Hoy,
            disponibleMensual: 8_000m,
            confianzaBaja: false,
            moneda: "DOP");

        Assert.Equal(0, resultado.MesesHastaLaSalida);
        Assert.Equal(35_000m, resultado.AporteMensualNecesario);
        Assert.Equal("No", resultado.Veredicto);
    }

    [Fact]
    public void ConPocoHistorialLaProyeccionSeMarcaComoEstimacion()
    {
        // La aplicacion debe presentarla como estimacion, no como dato: con dos meses de
        // datos, un mes atipico manda sobre la media.
        var resultado = CalculadoraViajes.Proyectar(
            costoTotal: 60_000m,
            fondoActual: 0m,
            fechaSalida: Hoy.AddMonths(12),
            hoy: Hoy,
            disponibleMensual: 15_000m,
            confianzaBaja: true,
            moneda: "DOP");

        Assert.True(resultado.ConfianzaBaja);
    }

    [Fact]
    public void LosTresEscenariosVanDeMenosAMasAhorro()
    {
        // El orden importa: la pantalla los muestra del mas prudente al mas optimista, y
        // el prudente es el que debe leerse primero.
        var resultado = CalculadoraViajes.Proyectar(
            costoTotal: 200_000m,
            fondoActual: 0m,
            fechaSalida: Hoy.AddMonths(12),
            hoy: Hoy,
            disponibleMensual: 20_000m,
            confianzaBaja: false,
            moneda: "DOP");

        Assert.Equal(3, resultado.Escenarios.Count);
        Assert.Equal("Conservador", resultado.Escenarios[0].Nombre);
        Assert.Equal("Esperado", resultado.Escenarios[1].Nombre);
        Assert.Equal("Optimista", resultado.Escenarios[2].Nombre);

        Assert.Equal(12_000m, resultado.Escenarios[0].AporteMensualSupuesto);
        Assert.Equal(17_000m, resultado.Escenarios[1].AporteMensualSupuesto);
        Assert.Equal(20_000m, resultado.Escenarios[2].AporteMensualSupuesto);

        // El optimista nunca supone mas de lo que el hogar tiene.
        Assert.Equal(20_000m, resultado.DisponibleMensual);
    }
}
