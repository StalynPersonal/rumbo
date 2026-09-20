using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Rumbo.Aplicacion.Comun;
using Microsoft.IdentityModel.Tokens;

using Rumbo.Aplicacion.Contratos;
using Rumbo.Infraestructura.Correo;
using Rumbo.Infraestructura.Identidad;
using Rumbo.Infraestructura.MultiEspacio;
using Rumbo.Infraestructura.Persistencia;
using Rumbo.Infraestructura.Persistencia.Interceptores;
using Rumbo.Infraestructura.Servicios;

namespace Rumbo.Infraestructura;

/// <summary>
/// Registra en el contenedor de dependencias todo lo que aporta la capa de infraestructura.
/// </summary>
/// <remarks>
/// Concentrarlo aqui mantiene <c>Program.cs</c> legible y hace que la API dependa de la
/// infraestructura en un unico punto: la composicion de dependencias.
/// </remarks>
public static class InyeccionDependencias
{
    /// <summary>
    /// Registra la base de datos, la identidad y los servicios de infraestructura.
    /// </summary>
    /// <param name="servicios">Coleccion de servicios de la aplicacion.</param>
    /// <param name="configuracion">Configuracion de la aplicacion.</param>
    /// <returns>La misma coleccion, para poder encadenar llamadas.</returns>
    /// <exception cref="InvalidOperationException">
    /// Si no hay cadena de conexion configurada. Se falla al arrancar y no en la primera
    /// consulta: un error de configuracion debe notarse de inmediato.
    /// </exception>
    public static IServiceCollection AgregarInfraestructura(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        var cadenaConexion = configuracion.GetConnectionString("Rumbo");

        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException(
                "Falta la cadena de conexion 'Rumbo'. En desarrollo se define en "
                + "appsettings.Development.json; en produccion, en Azure Key Vault.");
        }

        // --- Contexto de la peticion ---------------------------------------
        // Scoped: una instancia por peticion. Los interceptores y el DbContext comparten la
        // misma, de modo que todos ven el mismo espacio y el mismo usuario.
        servicios.AddScoped<ContextoEspacio>();
        servicios.AddScoped<IContextoEspacio>(s => s.GetRequiredService<ContextoEspacio>());

        servicios.AddScoped<UsuarioActual>();
        servicios.AddScoped<IUsuarioActual>(s => s.GetRequiredService<UsuarioActual>());

        servicios.AddSingleton<IProveedorFechaHora, ProveedorFechaHora>();

        // --- Interceptores --------------------------------------------------
        servicios.AddScoped<InterceptorEspacio>();
        servicios.AddScoped<InterceptorAuditoria>();
        servicios.AddScoped<InterceptorBorradoLogico>();

        // --- Base de datos ---------------------------------------------------
        servicios.AddDbContext<ContextoRumbo>((proveedor, opciones) =>
        {
            opciones.UseSqlServer(cadenaConexion, sql =>
            {
                sql.MigrationsAssembly(typeof(ContextoRumbo).Assembly.FullName);

                // Reintentos ante fallos transitorios. En Azure SQL son habituales: la base
                // puede reconfigurarse sola y cortar la conexion un instante.
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            });

            // El ORDEN de los interceptores importa:
            //  1. Borrado logico convierte los Remove en marcas, para que la auditoria los
            //     registre como modificacion y el dato no se pierda.
            //  2. Espacio asigna y valida el espacio de cada fila.
            //  3. Auditoria escribe el historial, ya con el espacio correcto asignado.
            opciones.AddInterceptors(
                proveedor.GetRequiredService<InterceptorBorradoLogico>(),
                proveedor.GetRequiredService<InterceptorEspacio>(),
                proveedor.GetRequiredService<InterceptorAuditoria>());
        });

        // --- Proteccion de datos ---------------------------------------------
        // Los proveedores de token de Identity (restablecer clave, confirmar correo) cifran
        // esos tokens con Data Protection, asi que hay que registrarla explicitamente:
        // AddIdentityCore no lo hace por su cuenta.
        //
        // ATENCION para la Fase 10: por defecto las claves se guardan en el sistema de
        // ficheros local. En Azure App Service eso significa que se pierden al reiniciar y
        // que no se comparten entre instancias, de modo que un enlace de "olvide mi clave"
        // dejaria de funcionar sin motivo aparente. Alli hay que persistirlas en Blob Storage
        // y protegerlas con Key Vault.
        servicios.AddDataProtection();

        // --- Identidad --------------------------------------------------------
        servicios.AddIdentityCore<Usuario>(opciones =>
            {
                // Politica de contrasenas. Se exige longitud antes que complejidad: una frase
                // larga resiste mucho mejor que "Abc123!" y es mas facil de recordar.
                opciones.Password.RequiredLength = 12;
                opciones.Password.RequireDigit = true;
                opciones.Password.RequireLowercase = true;
                opciones.Password.RequireUppercase = true;
                opciones.Password.RequireNonAlphanumeric = false;

                // Bloqueo tras intentos fallidos, contra los ataques de fuerza bruta.
                opciones.Lockout.MaxFailedAccessAttempts = 5;
                opciones.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                opciones.Lockout.AllowedForNewUsers = true;

                opciones.User.RequireUniqueEmail = true;
                opciones.SignIn.RequireConfirmedEmail = false;
            })
            .AddRoles<Rol>()
            .AddEntityFrameworkStores<ContextoRumbo>()
            .AddDefaultTokenProviders();

        // --- Opciones tipadas -------------------------------------------------
        servicios.Configure<OpcionesJwt>(configuracion.GetSection(OpcionesJwt.Seccion));
        servicios.Configure<OpcionesCorreo>(configuracion.GetSection(OpcionesCorreo.Seccion));

        // --- Servicios propios ------------------------------------------------
        servicios.AddMemoryCache();
        servicios.AddSingleton<CacheMembresias>();
        servicios.AddScoped<IServicioTokens, ServicioTokensJwt>();
        servicios.AddScoped<IProtectorSecretos, ProtectorSecretos>();
        servicios.AddScoped<ResolvedorCorreo>();
        servicios.AddScoped<IEnviadorCorreo, EnviadorCorreoSmtp>();
        servicios.AddScoped<IServicioConfiguracionCorreo, ServicioConfiguracionCorreo>();
        servicios.AddScoped<IServicioAutenticacion, ServicioAutenticacion>();
        servicios.AddScoped<IServicioInvitaciones, ServicioInvitaciones>();
        servicios.AddScoped<IServicioEspacios, ServicioEspacios>();

        // --- Validacion del token en cada peticion -----------------------------
        var opcionesJwt = configuracion.GetSection(OpcionesJwt.Seccion).Get<OpcionesJwt>()
            ?? new OpcionesJwt();

        if (string.IsNullOrWhiteSpace(opcionesJwt.ClaveFirma) || opcionesJwt.ClaveFirma.Length < 32)
        {
            // Se falla al arrancar y no en el primer inicio de sesion. Una clave corta o
            // ausente haria que los tokens fueran falsificables, y eso no puede depender de
            // que alguien se de cuenta al probar.
            throw new InvalidOperationException(
                "Falta la clave de firma de los tokens (Jwt:ClaveFirma) o tiene menos de 32 "
                + "caracteres. En desarrollo se configura con 'dotnet user-secrets set "
                + "\"Jwt:ClaveFirma\" \"...\"'; en produccion, en Azure Key Vault.");
        }

        servicios.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opciones =>
            {
                opciones.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = opcionesJwt.Emisor,

                    ValidateAudience = true,
                    ValidAudience = opcionesJwt.Audiencia,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(opcionesJwt.ClaveFirma)),

                    ValidateLifetime = true,

                    // Sin holgura de reloj. El valor por defecto son 5 minutos, que
                    // alargarian en un tercio la vida util de un token de 15 minutos.
                    ClockSkew = TimeSpan.Zero,
                };
            });

        servicios.AddAuthorization();

        return servicios;
    }
}
