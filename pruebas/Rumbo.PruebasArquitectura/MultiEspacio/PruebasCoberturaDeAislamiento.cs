using Rumbo.Dominio;
using Rumbo.Dominio.Comun;

namespace Rumbo.PruebasArquitectura.MultiEspacio;

// Esta es la prueba de arquitectura mas importante del proyecto.
//
// El aislamiento entre espacios depende de que cada entidad de negocio implemente
// IEntidadDeEspacio: el ContextoRumbo le aplica el filtro global por esa marca. Si alguien
// crea una entidad nueva y olvida la interfaz, sus filas serian visibles desde CUALQUIER
// hogar, el compilador no diria nada y las pruebas funcionales seguirian pasando, porque en
// un entorno de prueba con un solo espacio no se nota.
//
// Por eso la lista de entidades globales es explicita: anadir una entidad nueva obliga a
// decidir conscientemente si pertenece a un espacio o no.
public class PruebasCoberturaDeAislamiento
{
    /// <summary>
    /// Entidades que NO pertenecen a ningun espacio, con la razon por la que es correcto.
    /// </summary>
    private static readonly Dictionary<string, string> EntidadesGlobalesJustificadas = new()
    {
        ["Espacio"] = "Es la tabla raiz que define los espacios; no puede filtrarse por si misma.",
        ["MembresiaEspacio"] = "Responde a que espacios puede entrar un usuario; se consulta ANTES de saber cual es el espacio activo.",
        ["Invitacion"] = "Se canjea antes de tener sesion ni espacio; ademas, las de tipo Propietario crean el espacio.",
        ["TokenRenovacion"] = "Pertenece a un usuario, no a un espacio: la sesion es la misma aunque cambie de hogar.",
        ["Moneda"] = "Catalogo global ISO-4217, identico para todos los espacios.",
        ["TasaCambio"] = "Las tasas de cambio son datos publicos, no de un hogar concreto.",
        ["RegistroAuditoria"] = "Registra tambien acciones sin espacio (inicios de sesion, gestion de plataforma); el aislamiento se aplica de forma explicita al consultarlo.",
    };

    /// <summary>
    /// Entidades de un espacio que NO usan borrado logico, con su justificacion.
    /// </summary>
    private static readonly Dictionary<string, string> SinBorradoLogicoJustificado = new()
    {
        ["ConfiguracionEspacio"] = "No contiene datos financieros, solo preferencias. Es 1 a 1 con el espacio y desaparece con el; conservar una configuracion huerfana marcada como borrada no aportaria nada a la auditoria.",
    };

    private static IEnumerable<Type> EntidadesDelDominio() =>
        typeof(MarcadorDominio).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsNested: false }
                        && t.Namespace?.StartsWith("Rumbo.Dominio.Entidades", StringComparison.Ordinal) == true);

    [Fact]
    public void TodaEntidadDeNegocioEstaMarcadaComoPertenecienteAUnEspacio()
    {
        var sinAislamiento = EntidadesDelDominio()
            .Where(t => !typeof(IEntidadDeEspacio).IsAssignableFrom(t))
            .Where(t => !EntidadesGlobalesJustificadas.ContainsKey(t.Name))
            .Select(t => t.Name)
            .ToList();

        Assert.True(sinAislamiento.Count == 0,
            "Estas entidades no implementan IEntidadDeEspacio y tampoco figuran en la lista de "
            + "entidades globales justificadas, asi que sus datos serian visibles desde cualquier "
            + "espacio: " + string.Join(", ", sinAislamiento)
            + ". Si pertenecen a un hogar, hereda de EntidadDeEspacio. Si de verdad son globales, "
            + "anadelas a EntidadesGlobalesJustificadas explicando por que.");
    }

    [Fact]
    public void LaListaDeEntidadesGlobalesNoTieneNombresObsoletos()
    {
        // Si una entidad global se renombra o se borra y nadie actualiza la lista, la
        // justificacion se queda huerfana y la siguiente entidad que herede ese nombre
        // quedaria exenta del aislamiento sin que nadie lo hubiera decidido.
        var nombresReales = EntidadesDelDominio().Select(t => t.Name).ToHashSet(StringComparer.Ordinal);

        var obsoletas = EntidadesGlobalesJustificadas.Keys
            .Where(nombre => !nombresReales.Contains(nombre))
            .ToList();

        Assert.True(obsoletas.Count == 0,
            "La lista de entidades globales menciona tipos que ya no existen: "
            + string.Join(", ", obsoletas));
    }

    [Fact]
    public void LasEntidadesFinancierasUsanBorradoLogico()
    {
        // Borrar fisicamente un movimiento o una cuenta destruiria la trazabilidad: la
        // reconciliacion de saldos dejaria de cuadrar y la auditoria quedaria incompleta.
        var financierasSinBorradoLogico = EntidadesDelDominio()
            .Where(t => typeof(IEntidadDeEspacio).IsAssignableFrom(t))
            .Where(t => !typeof(IBorradoLogico).IsAssignableFrom(t))
            .Where(t => !SinBorradoLogicoJustificado.ContainsKey(t.Name))
            .Select(t => t.Name)
            .ToList();

        Assert.True(financierasSinBorradoLogico.Count == 0,
            "Estas entidades pertenecen a un espacio pero pueden borrarse fisicamente, lo que "
            + "destruiria la trazabilidad: " + string.Join(", ", financierasSinBorradoLogico)
            + ". Hereda de EntidadDeEspacio, o anadelas a SinBorradoLogicoJustificado "
            + "explicando por que su borrado fisico no pierde informacion.");
    }

    [Fact]
    public void TodaEntidadDelDominioTieneClavePrimaria()
    {
        var sinClave = EntidadesDelDominio()
            .Where(t => !typeof(EntidadBase).IsAssignableFrom(t))
            .Where(t => t.Name != "Moneda")  // Su clave es el codigo ISO-4217, no un Guid.
            .Select(t => t.Name)
            .ToList();

        Assert.True(sinClave.Count == 0,
            "Estas entidades no heredan de EntidadBase y por tanto no tienen clave primaria: "
            + string.Join(", ", sinClave));
    }
}
