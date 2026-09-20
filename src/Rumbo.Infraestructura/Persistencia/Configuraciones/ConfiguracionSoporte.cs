using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Rumbo.Dominio.Entidades.Soporte;

namespace Rumbo.Infraestructura.Persistencia.Configuraciones;

/// <summary>Configura el historial de auditoria.</summary>
/// <remarks>
/// No implementa <c>IEntidadDeEspacio</c> aunque tenga <c>EspacioId</c>: hay acciones sin
/// espacio (un inicio de sesion fallido, una gestion del administrador de plataforma) y el
/// filtro global las haria invisibles. El aislamiento de la auditoria se aplica de forma
/// explicita en el servicio que la consulta.
/// </remarks>
public class ConfiguracionRegistroAuditoria : IEntityTypeConfiguration<RegistroAuditoria>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RegistroAuditoria> constructor)
    {
        constructor.ToTable("RegistrosAuditoria");

        constructor.Property(r => r.CorreoUsuario).HasMaxLength(256);
        constructor.Property(r => r.TipoEntidad).HasMaxLength(100);
        constructor.Property(r => r.DireccionIp).HasMaxLength(45);
        constructor.Property(r => r.IdCorrelacion).HasMaxLength(100);
        constructor.Property(r => r.Descripcion).HasMaxLength(1000);

        constructor.Property(r => r.Accion).HasConversion<string>().HasMaxLength(30);

        // El JSON de cambios puede ser largo, asi que se sale de la convencion de 256.
        constructor.Property(r => r.Cambios).HasColumnType("nvarchar(max)");

        constructor.HasIndex(r => new { r.EspacioId, r.FechaHora })
            .HasDatabaseName("IX_RegistrosAuditoria_Espacio_Fecha")
            .IsDescending(false, true);

        // Para responder "quien toco este movimiento".
        constructor.HasIndex(r => new { r.TipoEntidad, r.EntidadId })
            .HasDatabaseName("IX_RegistrosAuditoria_Entidad");

        constructor.HasIndex(r => new { r.UsuarioId, r.FechaHora })
            .HasDatabaseName("IX_RegistrosAuditoria_Usuario_Fecha")
            .IsDescending(false, true);
    }
}

/// <summary>Configura las notificaciones.</summary>
public class ConfiguracionNotificacion : IEntityTypeConfiguration<Notificacion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Notificacion> constructor)
    {
        constructor.ToTable("Notificaciones");

        constructor.Property(n => n.Titulo).HasMaxLength(150).IsRequired();
        constructor.Property(n => n.Cuerpo).HasMaxLength(1000).IsRequired();
        constructor.Property(n => n.Datos).HasColumnType("nvarchar(max)");

        constructor.Property(n => n.Tipo).HasConversion<string>().HasMaxLength(40);
        constructor.Property(n => n.Estado).HasConversion<string>().HasMaxLength(20);

        // Consulta del despachador: que hay pendiente y ya toca mostrar.
        constructor.HasIndex(n => new { n.EspacioId, n.Estado, n.ProgramadaPara })
            .HasDatabaseName("IX_Notificaciones_Espacio_Estado_Programada");
    }
}

/// <summary>Configura las recomendaciones del motor.</summary>
public class ConfiguracionRecomendacion : IEntityTypeConfiguration<Recomendacion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Recomendacion> constructor)
    {
        constructor.ToTable("Recomendaciones");

        constructor.Property(r => r.Titulo).HasMaxLength(150).IsRequired();
        constructor.Property(r => r.Cuerpo).HasMaxLength(2000).IsRequired();
        constructor.Property(r => r.Moneda).HasMaxLength(3).IsFixedLength();

        constructor.Property(r => r.Tipo).HasConversion<string>().HasMaxLength(40);
        constructor.Property(r => r.Estado).HasConversion<string>().HasMaxLength(20);

        // Los insumos son el JSON con los datos y la formula que justifican la sugerencia.
        // Es lo que permite que la aplicacion explique de donde sale cada cifra, asi que no
        // puede truncarse.
        constructor.Property(r => r.Insumos).HasColumnType("nvarchar(max)");

        constructor.HasIndex(r => new { r.EspacioId, r.Estado, r.FechaGeneracion })
            .HasDatabaseName("IX_Recomendaciones_Espacio_Estado_Fecha")
            .IsDescending(false, false, true);

        constructor.HasOne(r => r.Meta)
            .WithMany()
            .HasForeignKey(r => r.MetaId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(r => r.Viaje)
            .WithMany()
            .HasForeignKey(r => r.ViajeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
