using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using Rumbo.Aplicacion.Comun;

namespace Rumbo.Infraestructura.Persistencia;

/// <summary>
/// Construye un <see cref="ContextoRumbo"/> para las herramientas de linea de comandos
/// (<c>dotnet ef migrations</c>, <c>dotnet ef database update</c>).
/// </summary>
/// <remarks>
/// <para>
/// Al generar una migracion no hay peticion HTTP ni contenedor de dependencias en marcha, asi
/// que EF Core no sabe como crear el contexto: necesita esta fabrica.
/// </para>
/// <para>
/// El espacio que se le pasa esta VACIO a proposito. Las migraciones solo leen la forma del
/// modelo (tablas, columnas, indices), nunca datos, y un contexto de diseno con un espacio
/// real seria una puerta trasera para leer informacion de un hogar desde una herramienta de
/// consola.
/// </para>
/// </remarks>
public class FabricaContextoTiempoDiseno : IDesignTimeDbContextFactory<ContextoRumbo>
{
    /// <inheritdoc />
    public ContextoRumbo CreateDbContext(string[] args)
    {
        // Esta cadena solo la usan las herramientas de EF en desarrollo. La aplicacion real
        // lee la suya de la configuracion (appsettings o Azure Key Vault).
        var cadenaConexion = Environment.GetEnvironmentVariable("RUMBO_CONEXION")
            ?? @"Server=localhost\SQLEXPRESS;Database=Rumbo;Trusted_Connection=True;"
               + @"TrustServerCertificate=True;MultipleActiveResultSets=True";

        var opciones = new DbContextOptionsBuilder<ContextoRumbo>()
            .UseSqlServer(cadenaConexion, sql =>
                sql.MigrationsAssembly(typeof(ContextoRumbo).Assembly.FullName))
            .Options;

        return new ContextoRumbo(opciones, new ContextoEspacioDeDiseno());
    }

    /// <summary>
    /// Contexto de espacio sin espacio, para uso exclusivo de las herramientas de EF Core.
    /// </summary>
    private sealed class ContextoEspacioDeDiseno : IContextoEspacio
    {
        public Guid? EspacioId => null;

        public bool HayEspacio => false;

        public Guid ObtenerEspacioObligatorio() =>
            throw new InvalidOperationException(
                "El contexto de tiempo de diseno no tiene espacio: las herramientas de EF Core "
                + "solo deben leer la estructura del modelo, nunca datos de un espacio.");
    }
}
