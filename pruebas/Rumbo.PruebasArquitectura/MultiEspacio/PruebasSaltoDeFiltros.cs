using System.Reflection;
using System.Text.RegularExpressions;

namespace Rumbo.PruebasArquitectura.MultiEspacio;

// IgnoreQueryFilters desactiva el aislamiento entre espacios. Es la unica forma de que una
// consulta devuelva datos de otro hogar, asi que cada uso debe ser deliberado y justificado.
//
// Esta prueba lee el CODIGO FUENTE, no los ensamblados: en el binario la llamada queda
// mezclada con el resto de la expresion LINQ y no hay forma fiable de localizarla.
public partial class PruebasSaltoDeFiltros
{
    /// <summary>
    /// Usos de <c>IgnoreQueryFilters</c> permitidos, con su justificacion.
    /// </summary>
    /// <remarks>
    /// Anadir una entrada aqui es una decision consciente sobre el aislamiento de datos, no
    /// un tramite: la clave es el fichero y el valor tiene que explicar por que ese caso no
    /// compromete la separacion entre hogares.
    /// </remarks>
    private static readonly Dictionary<string, string> UsosJustificados = new(StringComparer.Ordinal)
    {
        ["PruebasAislamientoEspacio.cs"] = "Es una prueba: comprueba que la fila borrada sigue en la base de datos aunque no se vea. Necesita saltarse el filtro para demostrarlo.",
    };

    [GeneratedRegex(@"IgnoreQueryFilters\s*\(", RegexOptions.Compiled)]
    private static partial Regex PatronSalto();

    [Fact]
    public void TodoUsoDeIgnoreQueryFiltersEstaJustificado()
    {
        var raiz = LocalizarRaizDelRepositorio();

        var infractores = new List<string>();

        foreach (var fichero in Directory.EnumerateFiles(raiz, "*.cs", SearchOption.AllDirectories))
        {
            // Se excluyen las carpetas de compilacion y el codigo generado por EF Core.
            if (fichero.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || fichero.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || fichero.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var contenido = File.ReadAllText(fichero);

            if (!PatronSalto().IsMatch(contenido))
            {
                continue;
            }

            var nombre = Path.GetFileName(fichero);

            if (!UsosJustificados.ContainsKey(nombre))
            {
                infractores.Add(nombre);
            }
        }

        Assert.True(infractores.Count == 0,
            "Estos ficheros usan IgnoreQueryFilters, que desactiva el aislamiento entre "
            + "espacios, sin figurar en la lista de usos justificados: "
            + string.Join(", ", infractores)
            + ". Si el uso es correcto, añádelo a UsosJustificados explicando por qué no "
            + "compromete la separación entre hogares. Si no lo es, quítalo.");
    }

    [Fact]
    public void LaListaDeUsosJustificadosNoTieneEntradasObsoletas()
    {
        // Una justificacion huerfana es peor que ninguna: deja abierta una excepcion para el
        // proximo fichero que se llame igual.
        var raiz = LocalizarRaizDelRepositorio();

        var ficherosConSalto = Directory
            .EnumerateFiles(raiz, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => PatronSalto().IsMatch(File.ReadAllText(f)))
            .Select(Path.GetFileName)
            .ToHashSet(StringComparer.Ordinal);

        var obsoletas = UsosJustificados.Keys
            .Where(nombre => !ficherosConSalto.Contains(nombre))
            .ToList();

        Assert.True(obsoletas.Count == 0,
            "La lista de usos justificados menciona ficheros que ya no usan "
            + "IgnoreQueryFilters: " + string.Join(", ", obsoletas));
    }

    /// <summary>
    /// Sube desde el ensamblado de pruebas hasta la carpeta que contiene la solucion.
    /// </summary>
    /// <returns>Ruta de la raiz del repositorio.</returns>
    private static string LocalizarRaizDelRepositorio()
    {
        var directorio = new DirectoryInfo(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!);

        while (directorio is not null && !directorio.EnumerateFiles("*.slnx").Any())
        {
            directorio = directorio.Parent;
        }

        return directorio?.FullName
               ?? throw new InvalidOperationException(
                   "No se encontró la raíz del repositorio (ningún .slnx en los directorios "
                   + "superiores).");
    }
}
