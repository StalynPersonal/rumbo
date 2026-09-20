using System.Reflection;
using System.Text.RegularExpressions;

namespace Rumbo.PruebasArquitectura.MultiEspacio;

// El administrador de plataforma gestiona altas y estados, pero NO puede leer las finanzas
// de ningun hogar. Es una promesa de privacidad hacia los usuarios, no una preferencia.
//
// La decision original (D6) planteaba un ContextoAdministracion aparte que solo expusiera
// las tablas permitidas. Se sustituyo por esta prueba: da la misma garantia, se verifica
// sola en cada compilacion y evita mantener un segundo DbContext con su propia
// configuracion, que es una fuente de divergencias silenciosas.
public partial class PruebasAlcanceDelAdministrador
{
    /// <summary>Tablas que el servicio de administracion NO puede consultar.</summary>
    /// <remarks>
    /// Son las que contienen dinero o planes financieros del hogar. Las que si puede usar
    /// (Espacios, MembresiasEspacio, Users, Invitaciones, Roles) quedan fuera de esta lista
    /// a proposito.
    /// </remarks>
    private static readonly string[] TablasProhibidas =
    [
        "Movimientos",
        "Cuentas",
        "Transferencias",
        "Metas",
        "AportesMeta",
        "Viajes",
        "Deudas",
        "PagosDeuda",
        "Presupuestos",
        "LineasPresupuesto",
        "GastosRecurrentes",
        "IngresosRecurrentes",
    ];

    [GeneratedRegex(@"contexto\.(\w+)", RegexOptions.Compiled)]
    private static partial Regex PatronAccesoATabla();

    [Fact]
    public void ElServicioDeAdministracionNoConsultaTablasFinancieras()
    {
        var raiz = LocalizarRaiz();

        var ficheros = Directory
            .EnumerateFiles(raiz, "ServicioAdministracion*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .ToList();

        Assert.True(ficheros.Count > 0,
            "No se encontró ServicioAdministracion. Si se renombró, actualiza esta prueba: "
            + "sin ella, nada impediría que el administrador leyera las finanzas de un hogar.");

        var accesosProhibidos = new List<string>();

        foreach (var fichero in ficheros)
        {
            var contenido = File.ReadAllText(fichero);

            foreach (Match coincidencia in PatronAccesoATabla().Matches(contenido))
            {
                var tabla = coincidencia.Groups[1].Value;

                if (TablasProhibidas.Contains(tabla, StringComparer.Ordinal))
                {
                    accesosProhibidos.Add($"{Path.GetFileName(fichero)} → {tabla}");
                }
            }
        }

        Assert.True(accesosProhibidos.Count == 0,
            "El servicio de administración consulta tablas con datos financieros de los "
            + "hogares: " + string.Join(", ", accesosProhibidos.Distinct())
            + ". El administrador de plataforma gestiona altas y estados, no finanzas ajenas. "
            + "Es una promesa de privacidad hacia los usuarios (ver D6 y D31).");
    }

    [Fact]
    public void LosControladoresDeDatosNoSeAbrenAlRolDePlataforma()
    {
        // Si un controlador financiero admitiera el rol de plataforma, el administrador
        // entraria por la puerta de al lado sin necesitar el servicio de administracion.
        var raiz = LocalizarRaiz();

        var controladoresFinancieros = new[]
        {
            "CuentasController.cs",
            "MovimientosController.cs",
            "CategoriasController.cs",
            "RecurrentesController.cs",
        };

        var infractores = new List<string>();

        foreach (var nombre in controladoresFinancieros)
        {
            var fichero = Directory
                .EnumerateFiles(raiz, nombre, SearchOption.AllDirectories)
                .FirstOrDefault(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal));

            if (fichero is null)
            {
                continue;
            }

            if (File.ReadAllText(fichero).Contains("AdministradorPlataforma", StringComparison.Ordinal))
            {
                infractores.Add(nombre);
            }
        }

        Assert.True(infractores.Count == 0,
            "Estos controladores financieros mencionan el rol de plataforma: "
            + string.Join(", ", infractores)
            + ". El administrador no debe tener acceso a datos financieros por ninguna vía.");
    }

    /// <summary>Sube hasta la carpeta que contiene la solucion.</summary>
    /// <returns>Ruta de la raiz del repositorio.</returns>
    private static string LocalizarRaiz()
    {
        var directorio = new DirectoryInfo(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!);

        while (directorio is not null && !directorio.EnumerateFiles("*.slnx").Any())
        {
            directorio = directorio.Parent;
        }

        return directorio?.FullName
               ?? throw new InvalidOperationException("No se encontró la raíz del repositorio.");
    }
}
