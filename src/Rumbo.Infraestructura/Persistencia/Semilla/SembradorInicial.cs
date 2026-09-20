using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Rumbo.Aplicacion.Comun;
using Rumbo.Infraestructura.Identidad;

namespace Rumbo.Infraestructura.Persistencia.Semilla;

/// <summary>
/// Crea, al arrancar, los roles de plataforma y el primer administrador.
/// </summary>
/// <remarks>
/// <para>
/// Rumbo no tiene registro publico: sin un administrador de plataforma que emita la primera
/// invitacion, nadie podria entrar nunca. Este sembrador resuelve ese arranque en frio.
/// </para>
/// <para>
/// <b>La contrasena inicial viene de la configuracion, jamas del codigo.</b> En desarrollo se
/// pone con <c>dotnet user-secrets</c>; en produccion, en Azure Key Vault. Si no hay
/// contrasena configurada, no se crea ningun usuario: es preferible arrancar sin
/// administrador a arrancar con uno de contrasena conocida.
/// </para>
/// </remarks>
public static class SembradorInicial
{
    /// <summary>Seccion de configuracion del administrador inicial.</summary>
    public const string SeccionAdministrador = "Rumbo:AdministradorInicial";

    /// <summary>
    /// Ejecuta la siembra: roles de plataforma y primer administrador.
    /// </summary>
    /// <param name="proveedor">Proveedor de servicios de la aplicacion.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando termina la siembra.</returns>
    public static async Task SembrarAsync(
        IServiceProvider proveedor,
        CancellationToken cancelacion = default)
    {
        using var ambito = proveedor.CreateScope();
        var servicios = ambito.ServiceProvider;

        var registro = servicios.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(SembradorInicial));

        await SembrarRolesAsync(servicios, registro);
        await SembrarAdministradorAsync(servicios, registro);
    }

    /// <summary>Crea los roles de plataforma que falten.</summary>
    private static async Task SembrarRolesAsync(IServiceProvider servicios, ILogger registro)
    {
        var roles = servicios.GetRequiredService<RoleManager<Rol>>();

        foreach (var nombre in RolesPlataforma.Todos)
        {
            if (await roles.RoleExistsAsync(nombre))
            {
                continue;
            }

            await roles.CreateAsync(new Rol
            {
                Id = Guid.CreateVersion7(),
                Name = nombre,
                Descripcion = "Gestiona espacios, usuarios e invitaciones. "
                              + "No tiene acceso a datos financieros de ningún espacio.",
            });

            registro.LogInformation("Rol de plataforma '{Rol}' creado.", nombre);
        }
    }

    /// <summary>Crea el primer administrador de plataforma si la configuracion lo define.</summary>
    private static async Task SembrarAdministradorAsync(IServiceProvider servicios, ILogger registro)
    {
        var configuracion = servicios.GetRequiredService<IConfiguration>();
        var seccion = configuracion.GetSection(SeccionAdministrador);

        var correo = seccion["Correo"]?.Trim().ToLowerInvariant();
        var clave = seccion["Clave"];
        var nombre = seccion["NombreCompleto"] ?? "Administrador de Rumbo";

        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(clave))
        {
            registro.LogWarning(
                "No hay administrador inicial configurado ({Seccion}). Nadie podrá emitir "
                + "invitaciones hasta que se configure un correo y una contraseña.",
                SeccionAdministrador);

            return;
        }

        var usuarios = servicios.GetRequiredService<UserManager<Usuario>>();

        if (await usuarios.FindByEmailAsync(correo) is not null)
        {
            // Ya existe: no se toca. Reescribir su contrasena en cada arranque permitiria
            // recuperar la cuenta con solo cambiar un fichero de configuracion.
            return;
        }

        var fechaHora = servicios.GetRequiredService<IProveedorFechaHora>();

        var administrador = new Usuario
        {
            Id = Guid.CreateVersion7(),
            UserName = correo,
            Email = correo,
            NombreCompleto = nombre,
            FechaCreacion = fechaHora.AhoraUtc,
            EmailConfirmed = true,
        };

        var resultado = await usuarios.CreateAsync(administrador, clave);

        if (!resultado.Succeeded)
        {
            registro.LogError(
                "No se pudo crear el administrador inicial: {Errores}",
                string.Join("; ", resultado.Errors.Select(e => e.Description)));

            return;
        }

        await usuarios.AddToRoleAsync(administrador, RolesPlataforma.AdministradorPlataforma);

        registro.LogInformation(
            "Administrador de plataforma creado con el correo {Correo}. "
            + "Cambia su contraseña en el primer acceso.", correo);
    }
}
