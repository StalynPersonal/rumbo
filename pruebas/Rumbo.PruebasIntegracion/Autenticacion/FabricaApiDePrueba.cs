using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Rumbo.Infraestructura.MultiEspacio;
using Rumbo.Infraestructura.Persistencia;

namespace Rumbo.PruebasIntegracion.Autenticacion;

/// <summary>
/// Levanta la API completa en memoria contra una base de datos SQL Server propia.
/// </summary>
/// <remarks>
/// <para>
/// Arranca el MISMO <c>Program.cs</c> que se despliega: la misma tuberia de middleware, los
/// mismos interceptores y las mismas reglas de autorizacion. Una prueba que montara su
/// propia tuberia verificaria un sistema que no existe.
/// </para>
/// <para>
/// Cada ejecucion usa una base de datos con nombre unico, que se borra al terminar, para que
/// las pruebas no dependan del orden ni se estorben entre si.
/// </para>
/// </remarks>
public class FabricaApiDePrueba : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Prefijo obligatorio de toda base de datos creada por las pruebas.</summary>
    public const string PrefijoBaseDePruebas = "RumboApi_";

    private readonly string _nombreBase = PrefijoBaseDePruebas + Guid.NewGuid().ToString("N")[..12];

    /// <summary>Correo del administrador de plataforma que se siembra al arrancar.</summary>
    public const string CorreoAdministrador = "admin@pruebas.local";

    /// <summary>Contrasena del administrador sembrado.</summary>
    public const string ClaveAdministrador = "AdminDePruebas2026!";

    private string CadenaConexion =>
        $@"Server=localhost\SQLEXPRESS;Database={_nombreBase};Trusted_Connection=True;"
        + "TrustServerCertificate=True;MultipleActiveResultSets=True";

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder constructor)
    {
        constructor.UseEnvironment("Development");

        // Se usa UseSetting y NO ConfigureAppConfiguration. Con el hospedaje minimo de
        // .NET, las fuentes anadidas en ConfigureAppConfiguration pueden quedar POR DEBAJO
        // de appsettings.Development.json, de modo que la cadena de conexion de desarrollo
        // gana y las pruebas acaban trabajando sobre la base de datos real. UseSetting
        // escribe en la configuracion del host, que tiene precedencia.
        constructor.UseSetting("ConnectionStrings:Rumbo", CadenaConexion);

        // Clave de firma exclusiva de las pruebas. La real vive en user-secrets o en Key
        // Vault y nunca aparece en el repositorio.
        constructor.UseSetting(
            "Jwt:ClaveFirma", "clave-de-firma-exclusiva-para-pruebas-automaticas-0123456789");
        constructor.UseSetting("Jwt:MinutosTokenAcceso", "15");

        constructor.UseSetting("Rumbo:AdministradorInicial:Correo", CorreoAdministrador);
        constructor.UseSetting("Rumbo:AdministradorInicial:Clave", ClaveAdministrador);
        constructor.UseSetting(
            "Rumbo:AdministradorInicial:NombreCompleto", "Administrador de Pruebas");

        // Cupos muy altos para el limitador. Con WebApplicationFactory la direccion IP es
        // nula, asi que TODAS las pruebas caen en la misma particion del limitador y
        // compartirian los cinco intentos por minuto de produccion: la suite se romperia
        // sola en cuanto creciera. Que el limite corta de verdad se comprueba en
        // PruebasLimiteDePeticiones, que levanta su propia fabrica con cupos bajos.
        constructor.UseSetting("LimitePeticiones:PorMinutoAutenticacion", "100000");
        constructor.UseSetting("LimitePeticiones:PorMinutoGeneral", "100000");

        // Sin servidor SMTP: el enviador registra el correo en el log y devuelve false, que
        // es justo uno de los comportamientos que hay que poder probar.
        constructor.UseSetting("Correo:Host", string.Empty);
    }

    /// <summary>Crea la base de datos aplicando las migraciones.</summary>
    /// <returns>Tarea que finaliza cuando la base esta lista.</returns>
    public async Task InitializeAsync()
    {
        // La base de datos se crea con un contexto PROPIO, no con el de la aplicacion.
        // Motivo: al tocar la propiedad Services, WebApplicationFactory construye y arranca
        // el host, lo que ejecuta Program.cs y con el al sembrador inicial, que consulta la
        // base. Si todavia no existiera, el arranque fallaria antes de poder migrarla.
        var opciones = new DbContextOptionsBuilder<ContextoRumbo>()
            .UseSqlServer(CadenaConexion)
            .Options;

        await using var contexto = new ContextoRumbo(opciones, new ContextoEspacio());
        await contexto.Database.MigrateAsync();

        // Ahora si: se arranca el host, que sembrara los roles y el administrador.
        _ = Services;
    }

    /// <summary>Borra la base de datos de la prueba.</summary>
    /// <returns>Tarea que finaliza cuando se elimina.</returns>
    async Task IAsyncLifetime.DisposeAsync()
    {
        using (var ambito = Services.CreateScope())
        {
            var contexto = ambito.ServiceProvider.GetRequiredService<ContextoRumbo>();

            // RED DE SEGURIDAD. Si por cualquier motivo la configuracion de prueba no se
            // hubiera aplicado, este contexto apuntaria a la base de datos de DESARROLLO y
            // la linea siguiente la borraria. Ya ocurrio una vez. Se comprueba el nombre
            // antes de borrar nada: es una guarda barata frente a una perdida irreversible.
            var nombreReal = contexto.Database.GetDbConnection().Database;

            if (!nombreReal.StartsWith(PrefijoBaseDePruebas, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Las pruebas iban a borrar la base de datos '{nombreReal}', que NO es una "
                    + $"base de pruebas (deberia empezar por '{PrefijoBaseDePruebas}'). Se aborta "
                    + "el borrado. Revisa la configuración de FabricaApiDePrueba.");
            }

            await contexto.Database.EnsureDeletedAsync();
        }

        await base.DisposeAsync();
    }
}
