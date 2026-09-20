using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Comun;
using Rumbo.Dominio.Enums;
using Rumbo.Infraestructura.MultiEspacio;
using Rumbo.Infraestructura.Persistencia;
using Rumbo.Infraestructura.Persistencia.Interceptores;

namespace Rumbo.PruebasIntegracion.Persistencia;

/// <summary>
/// Crea bases de datos reales de SQL Server para las pruebas de integracion.
/// </summary>
/// <remarks>
/// Se usa SQL Server de verdad y no el proveedor en memoria a proposito. El proveedor en
/// memoria no aplica restricciones CHECK, ni claves foraneas, ni tipos decimales: una prueba
/// que pase con el no demuestra que el esquema real impida nada. Y justamente lo que hay que
/// demostrar aqui es que la base de datos protege el dinero.
/// </remarks>
public sealed class BaseDeDatosDePrueba : IAsyncDisposable
{
    /// <summary>Prefijo obligatorio de toda base de datos creada por las pruebas.</summary>
    public const string PrefijoBaseDePruebas = "RumboPruebas_";

    private readonly string _nombreBase;

    /// <summary>Crea una base de datos vacia con un nombre unico.</summary>
    public BaseDeDatosDePrueba()
    {
        _nombreBase = PrefijoBaseDePruebas + Guid.NewGuid().ToString("N")[..12];
    }

    /// <summary>Cadena de conexion a la base de datos de la prueba.</summary>
    public string CadenaConexion =>
        $@"Server=localhost\SQLEXPRESS;Database={_nombreBase};Trusted_Connection=True;"
        + "TrustServerCertificate=True;MultipleActiveResultSets=True";

    /// <summary>Aplica el esquema completo ejecutando las migraciones.</summary>
    /// <returns>Tarea que finaliza cuando la base esta lista.</returns>
    public async Task CrearAsync()
    {
        await using var contexto = CrearContexto(espacioActivo: null);
        await contexto.Database.MigrateAsync();
    }

    /// <summary>
    /// Crea un contexto que opera como si la peticion perteneciera al espacio indicado.
    /// </summary>
    /// <param name="espacioActivo">
    /// Espacio activo, o <c>null</c> para simular una peticion sin espacio.
    /// </param>
    /// <returns>Un contexto nuevo.</returns>
    public ContextoRumbo CrearContexto(Guid? espacioActivo)
    {
        var contextoEspacio = new ContextoEspacio();
        if (espacioActivo.HasValue)
        {
            contextoEspacio.Establecer(espacioActivo.Value);
        }

        var usuarioActual = new UsuarioActual();
        usuarioActual.Establecer(UsuarioDePrueba, "pruebas@rumbo.local", RolEspacio.Propietario, false);

        var fechaHora = new ProveedorFechaHoraFijo();

        // Los interceptores se registran en el MISMO orden que en produccion
        // (ver InyeccionDependencias). Sin ellos, la prueba verificaria una tuberia distinta
        // de la real y no demostraria nada sobre el sistema que se despliega.
        var opciones = new DbContextOptionsBuilder<ContextoRumbo>()
            .UseSqlServer(CadenaConexion)
            .AddInterceptors(
                new InterceptorBorradoLogico(usuarioActual, fechaHora),
                new InterceptorEspacio(contextoEspacio),
                new InterceptorAuditoria(usuarioActual, fechaHora))
            .Options;

        return new ContextoRumbo(opciones, contextoEspacio);
    }

    /// <summary>Usuario ficticio que figura como autor de los cambios en las pruebas.</summary>
    public static Guid UsuarioDePrueba { get; } = Guid.CreateVersion7();

    /// <summary>
    /// Reloj detenido en una fecha conocida, para que las pruebas sean reproducibles.
    /// </summary>
    private sealed class ProveedorFechaHoraFijo : IProveedorFechaHora
    {
        public DateTimeOffset AhoraUtc { get; } = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);

        public DateOnly HoyEn(string zonaHorariaIana) => new(2026, 9, 19);
    }

    /// <summary>
    /// Crea un contexto que ignora el contexto de espacio, para preparar datos de prueba de
    /// varios espacios a la vez.
    /// </summary>
    /// <returns>Un contexto sin espacio activo.</returns>
    /// <remarks>
    /// Solo debe usarse para SEMBRAR datos, nunca para comprobar el aislamiento: comprobarlo
    /// con un contexto sin filtro no demostraria nada.
    /// </remarks>
    public ContextoRumbo CrearContextoDeSiembra() => CrearContexto(espacioActivo: null);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Misma red de seguridad que en FabricaApiDePrueba: nunca borrar una base que no
        // sea de pruebas.
        if (!_nombreBase.StartsWith(PrefijoBaseDePruebas, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Se iba a borrar la base de datos '{_nombreBase}', que no es de pruebas.");
        }

        await using var contexto = CrearContexto(espacioActivo: null);
        await contexto.Database.EnsureDeletedAsync();
    }
}
