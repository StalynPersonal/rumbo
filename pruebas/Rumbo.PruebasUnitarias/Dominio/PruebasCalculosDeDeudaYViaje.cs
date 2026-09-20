using Rumbo.Dominio.Entidades.Identidad;
using Rumbo.Dominio.Entidades.Planificacion;

namespace Rumbo.PruebasUnitarias.Dominio;

public class PruebasCalculosDeDeuda
{
    private static Deuda CrearDeuda(decimal original, decimal saldo) => new()
    {
        Nombre = "Prestamo del vehiculo",
        Moneda = "DOP",
        MontoOriginal = original,
        SaldoActual = saldo,
    };

    [Fact]
    public void ElPorcentajePagadoRefejaLoQueYaSeAmortizo()
    {
        var deuda = CrearDeuda(original: 500_000m, saldo: 200_000m);

        // Se han pagado 300.000 de 500.000.
        Assert.Equal(60m, deuda.PorcentajePagado);
    }

    [Fact]
    public void UnaDeudaSaldadaEstaAlCienPorCiento()
    {
        var deuda = CrearDeuda(original: 500_000m, saldo: 0m);

        Assert.Equal(100m, deuda.PorcentajePagado);
    }

    [Fact]
    public void UnSaldoMayorQueElOriginalNoDaUnPorcentajeNegativo()
    {
        // Puede ocurrir con intereses de mora acumulados: la deuda crece por encima del
        // principal. La barra debe quedarse en cero, no irse a negativo.
        var deuda = CrearDeuda(original: 100_000m, saldo: 120_000m);

        Assert.Equal(0m, deuda.PorcentajePagado);
    }
}

public class PruebasCalculosDeViaje
{
    [Fact]
    public void LaDuracionIncluyeElDiaDeSalidaYElDeRegreso()
    {
        var viaje = new Viaje
        {
            Nombre = "Viaje a Colombia",
            Moneda = "DOP",
            FechaInicio = new DateOnly(2027, 12, 15),
            FechaFin = new DateOnly(2027, 12, 24),
        };

        // Del 15 al 24 son 10 dias de viaje, no 9. Contar solo la diferencia dejaria fuera
        // uno de los dos extremos y el presupuesto por dia saldria mal.
        Assert.Equal(10, viaje.DuracionEnDias);
    }

    [Fact]
    public void UnViajeDeUnSoloDiaDuraUnDia()
    {
        var viaje = new Viaje
        {
            Nombre = "Excursion a Jarabacoa",
            Moneda = "DOP",
            FechaInicio = new DateOnly(2027, 3, 8),
            FechaFin = new DateOnly(2027, 3, 8),
        };

        Assert.Equal(1, viaje.DuracionEnDias);
    }
}

public class PruebasVigenciaDeTokens
{
    private static readonly DateTimeOffset Ahora = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);

    private static TokenRenovacion CrearToken(
        DateTimeOffset expiracion,
        DateTimeOffset? revocacion = null) => new()
    {
        Hash = new string('a', 64),
        FechaExpiracion = expiracion,
        FechaRevocacion = revocacion,
    };

    [Fact]
    public void UnTokenSinRevocarYDentroDePlazoEstaVigente()
    {
        var token = CrearToken(expiracion: Ahora.AddDays(30));

        Assert.True(token.EstaVigente(Ahora));
    }

    [Fact]
    public void UnTokenCaducadoNoEstaVigente()
    {
        var token = CrearToken(expiracion: Ahora.AddMinutes(-1));

        Assert.False(token.EstaVigente(Ahora));
    }

    [Fact]
    public void UnTokenRevocadoNoEstaVigenteAunqueNoHayaCaducado()
    {
        // Este es el caso que detecta un robo: el token sigue en plazo, pero ya se uso y se
        // roto. Que alguien lo presente significa que hay dos copias circulando.
        var token = CrearToken(
            expiracion: Ahora.AddDays(30),
            revocacion: Ahora.AddMinutes(-5));

        Assert.False(token.EstaVigente(Ahora));
    }
}
