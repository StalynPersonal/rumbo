using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Rumbo.Dominio.Entidades.Identidad;
using Rumbo.Infraestructura.Identidad;

namespace Rumbo.Infraestructura.Persistencia.Configuraciones;

/// <summary>Configura la tabla de espacios.</summary>
public class ConfiguracionEspacioEntidad : IEntityTypeConfiguration<Espacio>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Espacio> constructor)
    {
        constructor.ToTable("Espacios");

        constructor.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
        constructor.Property(e => e.MonedaBase).HasMaxLength(3).IsFixedLength().IsRequired();
        constructor.Property(e => e.ZonaHoraria).HasMaxLength(64).IsRequired();

        constructor.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(20);
        constructor.Property(e => e.Estado).HasConversion<string>().HasMaxLength(20);

        constructor.HasOne(e => e.Configuracion)
            .WithOne(c => c.Espacio)
            .HasForeignKey<ConfiguracionEspacio>(c => c.EspacioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Configura la pertenencia de usuarios a espacios.</summary>
public class ConfiguracionMembresiaEspacio : IEntityTypeConfiguration<MembresiaEspacio>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MembresiaEspacio> constructor)
    {
        constructor.ToTable("MembresiasEspacio");

        constructor.Property(m => m.Rol).HasConversion<string>().HasMaxLength(20);
        constructor.Property(m => m.Estado).HasConversion<string>().HasMaxLength(20);

        // Una persona no puede pertenecer dos veces al mismo espacio. Sin esta restriccion,
        // un doble clic en "aceptar invitacion" crearia dos membresias y el usuario podria
        // acabar con dos roles distintos en el mismo hogar.
        constructor.HasIndex(m => new { m.UsuarioId, m.EspacioId })
            .HasDatabaseName("UX_MembresiasEspacio_Usuario_Espacio")
            .IsUnique();

        // Consulta mas frecuente del sistema: el middleware la ejecuta en CADA peticion para
        // comprobar que el usuario sigue teniendo acceso al espacio que dice su token.
        constructor.HasIndex(m => new { m.EspacioId, m.Estado })
            .HasDatabaseName("IX_MembresiasEspacio_Espacio_Estado");

        constructor.HasOne(m => m.Espacio)
            .WithMany(e => e.Membresias)
            .HasForeignKey(m => m.EspacioId)
            .OnDelete(DeleteBehavior.Cascade);

        // La relacion con Usuario se declara sin propiedad de navegacion: las entidades del
        // dominio no conocen el tipo Usuario, que vive en Infraestructura por depender de
        // ASP.NET Core Identity.
        constructor.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(m => m.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Configura las preferencias de cada espacio.</summary>
public class ConfiguracionConfiguracionEspacio : IEntityTypeConfiguration<ConfiguracionEspacio>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ConfiguracionEspacio> constructor)
    {
        constructor.ToTable("ConfiguracionesEspacio", tabla =>
        {
            tabla.HasCheckConstraint("CK_ConfiguracionesEspacio_DiaInicioMes",
                "[DiaInicioMes] BETWEEN 1 AND 28");

            // El limite de 28 no es arbitrario: es el ultimo dia que existe en TODOS los
            // meses. Permitir el 31 obligaria a decidir que hacer en febrero.
            tabla.HasCheckConstraint("CK_ConfiguracionesEspacio_Umbrales",
                "[UmbralAvisoPresupuesto] > 0 AND [UmbralAvisoPresupuesto] <= [UmbralCriticoPresupuesto] "
                + "AND [UmbralCriticoPresupuesto] <= [UmbralExcedidoPresupuesto]");
        });

        constructor.Property(c => c.UmbralAvisoPresupuesto).HasPrecision(5, 2);
        constructor.Property(c => c.UmbralCriticoPresupuesto).HasPrecision(5, 2);
        constructor.Property(c => c.UmbralExcedidoPresupuesto).HasPrecision(5, 2);

        constructor.HasIndex(c => c.EspacioId)
            .HasDatabaseName("UX_ConfiguracionesEspacio_Espacio")
            .IsUnique();
    }
}
