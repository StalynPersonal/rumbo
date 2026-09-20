using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Rumbo.Infraestructura.Identidad;

namespace Rumbo.Infraestructura.Persistencia.Configuraciones;

/// <summary>
/// Configura las tablas de ASP.NET Core Identity con nombres en espanol.
/// </summary>
/// <remarks>
/// Por defecto Identity crea <c>AspNetUsers</c>, <c>AspNetRoles</c> y companhia. Se renombran
/// para que la base de datos sea coherente con el resto del proyecto: abrir el diagrama y
/// encontrar la mitad de las tablas en ingles y la otra mitad en espanol es una molestia
/// permanente.
/// </remarks>
public class ConfiguracionUsuario : IEntityTypeConfiguration<Usuario>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Usuario> constructor)
    {
        constructor.ToTable("Usuarios");

        constructor.Property(u => u.NombreCompleto).HasMaxLength(200).IsRequired();
        constructor.Property(u => u.CulturaPreferida).HasMaxLength(10);

        // El correo se usa como identificador de acceso, asi que debe ser unico.
        // Identity ya crea un indice sobre NormalizedEmail, pero no unico por defecto.
        constructor.HasIndex(u => u.NormalizedEmail)
            .HasDatabaseName("IX_Usuarios_CorreoNormalizado")
            .IsUnique();
    }
}

/// <summary>Configura la tabla de roles de plataforma.</summary>
public class ConfiguracionRol : IEntityTypeConfiguration<Rol>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Rol> constructor)
    {
        constructor.ToTable("Roles");
        constructor.Property(r => r.Descripcion).HasMaxLength(256);
    }
}

/// <summary>Renombra la tabla intermedia de usuarios y roles.</summary>
public class ConfiguracionUsuarioRol : IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> constructor) =>
        constructor.ToTable("UsuariosRoles");
}

/// <summary>Renombra la tabla de reclamaciones de usuario.</summary>
public class ConfiguracionUsuarioReclamacion : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> constructor) =>
        constructor.ToTable("UsuariosReclamaciones");
}

/// <summary>Renombra la tabla de inicios de sesion externos.</summary>
public class ConfiguracionUsuarioInicioSesion : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> constructor) =>
        constructor.ToTable("UsuariosInicioSesion");
}

/// <summary>Renombra la tabla de tokens de usuario.</summary>
public class ConfiguracionUsuarioToken : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> constructor) =>
        constructor.ToTable("UsuariosTokens");
}

/// <summary>Renombra la tabla de reclamaciones de rol.</summary>
public class ConfiguracionRolReclamacion : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> constructor) =>
        constructor.ToTable("RolesReclamaciones");
}
