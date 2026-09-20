using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Rumbo.Dominio.Entidades.Identidad;
using Rumbo.Infraestructura.Identidad;

namespace Rumbo.Infraestructura.Persistencia.Configuraciones;

/// <summary>Configura las invitaciones.</summary>
public class ConfiguracionInvitacion : IEntityTypeConfiguration<Invitacion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Invitacion> constructor)
    {
        constructor.ToTable("Invitaciones");

        constructor.Property(i => i.Correo).HasMaxLength(256).IsRequired();
        constructor.Property(i => i.NombreEspacioPropuesto).HasMaxLength(150);

        // SHA-256 en hexadecimal ocupa exactamente 64 caracteres.
        constructor.Property(i => i.HashCodigo).HasMaxLength(64).IsFixedLength().IsRequired();

        constructor.Property(i => i.Tipo).HasConversion<string>().HasMaxLength(20);
        constructor.Property(i => i.Estado).HasConversion<string>().HasMaxLength(20);
        constructor.Property(i => i.RolAsignado).HasConversion<string>().HasMaxLength(20);

        // Al canjear una invitacion se busca por el hash del codigo: tiene que ser unico y
        // estar indexado, porque es la operacion critica del alta.
        constructor.HasIndex(i => i.HashCodigo)
            .HasDatabaseName("UX_Invitaciones_HashCodigo")
            .IsUnique();

        constructor.HasIndex(i => new { i.Correo, i.Estado })
            .HasDatabaseName("IX_Invitaciones_Correo_Estado");

        constructor.HasOne(i => i.Espacio)
            .WithMany()
            .HasForeignKey(i => i.EspacioId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(i => i.EmitidaPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Configura los tokens de renovacion de sesion.</summary>
public class ConfiguracionTokenRenovacion : IEntityTypeConfiguration<TokenRenovacion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TokenRenovacion> constructor)
    {
        constructor.ToTable("TokensRenovacion");

        constructor.Property(t => t.Hash).HasMaxLength(64).IsFixedLength().IsRequired();
        constructor.Property(t => t.DireccionIp).HasMaxLength(45);
        constructor.Property(t => t.Dispositivo).HasMaxLength(256);

        constructor.HasIndex(t => t.Hash)
            .HasDatabaseName("UX_TokensRenovacion_Hash")
            .IsUnique();

        // Permite revocar de golpe todos los tokens de un usuario, por ejemplo al cambiar la
        // contrasena o al detectar una reutilizacion sospechosa.
        constructor.HasIndex(t => new { t.UsuarioId, t.FechaRevocacion })
            .HasDatabaseName("IX_TokensRenovacion_Usuario_Revocacion");

        // Al borrar un usuario se borran sus tokens: no tiene sentido conservarlos, y no son
        // informacion financiera que haya que auditar.
        constructor.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(t => t.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
