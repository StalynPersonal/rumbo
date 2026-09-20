using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Entidades.Planificacion;

namespace Rumbo.Infraestructura.Persistencia.Configuraciones;

/// <summary>Configura las metas de ahorro.</summary>
public class ConfiguracionMetaEntidad : IEntityTypeConfiguration<Meta>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Meta> constructor)
    {
        constructor.ToTable("Metas", tabla =>
        {
            tabla.HasCheckConstraint("CK_Metas_ObjetivoPositivo", "[MontoObjetivo] > 0");
            tabla.HasCheckConstraint("CK_Metas_ActualNoNegativo", "[MontoActual] >= 0");
        });

        constructor.Property(m => m.Nombre).HasMaxLength(150).IsRequired();
        constructor.Property(m => m.Descripcion).HasMaxLength(1000);
        constructor.Property(m => m.Moneda).HasMaxLength(3).IsFixedLength().IsRequired();
        constructor.Property(m => m.Icono).HasMaxLength(50);

        constructor.Property(m => m.Prioridad).HasConversion<string>().HasMaxLength(20);
        constructor.Property(m => m.Estado).HasConversion<string>().HasMaxLength(20);

        constructor.Property(m => m.Version).IsRowVersion();

        // MontoFaltante y PorcentajeCompletado se calculan en C# a partir de los otros dos
        // campos: no son columnas, y hay que decirselo a EF Core explicitamente.
        constructor.Ignore(m => m.MontoFaltante);
        constructor.Ignore(m => m.PorcentajeCompletado);

        constructor.HasIndex(m => new { m.EspacioId, m.Estado, m.Prioridad })
            .HasDatabaseName("IX_Metas_Espacio_Estado_Prioridad");

        constructor.HasOne(m => m.CuentaVinculada)
            .WithMany()
            .HasForeignKey(m => m.CuentaVinculadaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Configura los aportes a metas.</summary>
public class ConfiguracionAporteMeta : IEntityTypeConfiguration<AporteMeta>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AporteMeta> constructor)
    {
        constructor.ToTable("AportesMeta", tabla =>
            tabla.HasCheckConstraint("CK_AportesMeta_MontoPositivo", "[Monto] > 0"));

        constructor.Property(a => a.Moneda).HasMaxLength(3).IsFixedLength().IsRequired();
        constructor.Property(a => a.Notas).HasMaxLength(500);

        constructor.HasIndex(a => new { a.MetaId, a.Fecha })
            .HasDatabaseName("IX_AportesMeta_Meta_Fecha")
            .IsDescending(false, true);

        constructor.HasOne(a => a.Meta)
            .WithMany(m => m.Aportes)
            .HasForeignKey(a => a.MetaId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(a => a.Movimiento)
            .WithMany()
            .HasForeignKey(a => a.MovimientoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
