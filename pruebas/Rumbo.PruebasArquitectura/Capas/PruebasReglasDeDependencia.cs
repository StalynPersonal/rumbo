using System.Reflection;

using NetArchTest.Rules;

using Rumbo.Api.Extensiones;
using Rumbo.Aplicacion;
using Rumbo.Contratos;
using Rumbo.Dominio;
using Rumbo.Infraestructura;

namespace Rumbo.PruebasArquitectura.Capas;

// Estas pruebas vigilan la regla de dependencia entre capas descrita en docs/ARQUITECTURA.md.
// Un compañero distraido (o yo dentro de seis meses) puede anadir una referencia de proyecto
// que invierta la direccion de las dependencias: el compilador no se queja, pero la arquitectura
// se degrada en silencio. Estas pruebas SI se quejan.
public class PruebasReglasDeDependencia
{
    private static readonly Assembly Dominio = typeof(MarcadorDominio).Assembly;
    private static readonly Assembly Aplicacion = typeof(MarcadorAplicacion).Assembly;
    private static readonly Assembly Infraestructura = typeof(MarcadorInfraestructura).Assembly;
    private static readonly Assembly Api = typeof(ExtensionesServicios).Assembly;

    [Fact]
    public void ElDominioNoDependeDeNingunaOtraCapa()
    {
        var resultado = Types.InAssembly(Dominio)
            .ShouldNot()
            .HaveDependencyOnAny("Rumbo.Aplicacion", "Rumbo.Infraestructura", "Rumbo.Api", "Rumbo.Contratos")
            .GetResult();

        Assert.True(resultado.IsSuccessful, DescribirFallo(
            "El Dominio debe poder compilarse y probarse solo. Tipos que rompen la regla",
            resultado));
    }

    [Fact]
    public void LaAplicacionNoDependeDeLaInfraestructuraNiDeLaApi()
    {
        var resultado = Types.InAssembly(Aplicacion)
            .ShouldNot()
            .HaveDependencyOnAny("Rumbo.Infraestructura", "Rumbo.Api")
            .GetResult();

        Assert.True(resultado.IsSuccessful, DescribirFallo(
            "La Aplicacion declara interfaces; la Infraestructura las implementa, nunca al reves. "
            + "Tipos que rompen la regla",
            resultado));
    }

    [Fact]
    public void LaInfraestructuraNoDependeDeLaApi()
    {
        var resultado = Types.InAssembly(Infraestructura)
            .ShouldNot()
            .HaveDependencyOn("Rumbo.Api")
            .GetResult();

        Assert.True(resultado.IsSuccessful, DescribirFallo(
            "La Infraestructura no debe saber que existe una API encima. Tipos que rompen la regla",
            resultado));
    }

    [Fact]
    public void LosContratosNoDependenDeNadaDelServidor()
    {
        // Rumbo.Contratos lo referencia tambien la app MAUI (Fase 8). Si arrastrase EF Core o
        // ASP.NET Core, todo eso acabaria dentro del APK.
        var resultado = Types.InAssembly(typeof(MarcadorContratos).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Rumbo.Dominio", "Rumbo.Aplicacion", "Rumbo.Infraestructura", "Rumbo.Api",
                                 "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        Assert.True(resultado.IsSuccessful, DescribirFallo(
            "Los Contratos viajan al APK: deben quedarse sin dependencias. Tipos que rompen la regla",
            resultado));
    }

    [Fact]
    public void SoloLaApiConoceLaInfraestructura()
    {
        // La API puede referenciar la Infraestructura, pero solo para registrar dependencias.
        // Esta prueba documenta que esa referencia existe a proposito y que es la unica.
        var dependenciasDeLaApi = Types.InAssembly(Api)
            .That()
            .HaveDependencyOn("Rumbo.Infraestructura")
            .GetTypes();

        Assert.True(dependenciasDeLaApi.Count() <= 2,
            "Solo la composicion de dependencias (Program.cs y sus extensiones) deberia conocer la "
            + "Infraestructura. Tipos que la usan: "
            + string.Join(", ", dependenciasDeLaApi.Select(t => t.FullName)));
    }

    private static string DescribirFallo(string mensaje, TestResult resultado)
    {
        var tipos = resultado.FailingTypeNames is null
            ? "(ninguno)"
            : string.Join(", ", resultado.FailingTypeNames);

        return $"{mensaje}: {tipos}";
    }
}
