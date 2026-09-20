using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Entidades.Identidad;
using Rumbo.Infraestructura.Identidad;

namespace Rumbo.Infraestructura.Persistencia.Configuraciones;

/// <summary>Configura las cuentas.</summary>
public class ConfiguracionCuentaEntidad : IEntityTypeConfiguration<Cuenta>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Cuenta> constructor)
    {
        constructor.ToTable("Cuentas", tabla =>
        {
            tabla.HasCheckConstraint("CK_Cuentas_DiaCorte", "[DiaCorte] IS NULL OR [DiaCorte] BETWEEN 1 AND 28");
            tabla.HasCheckConstraint("CK_Cuentas_DiaPago", "[DiaPago] IS NULL OR [DiaPago] BETWEEN 1 AND 28");
            tabla.HasCheckConstraint("CK_Cuentas_LimiteCredito", "[LimiteCredito] IS NULL OR [LimiteCredito] >= 0");
        });

        constructor.Property(c => c.Nombre).HasMaxLength(150).IsRequired();
        constructor.Property(c => c.Moneda).HasMaxLength(3).IsFixedLength().IsRequired();
        constructor.Property(c => c.Institucion).HasMaxLength(150);
        constructor.Property(c => c.UltimosDigitos).HasMaxLength(4);
        constructor.Property(c => c.Notas).HasMaxLength(1000);

        constructor.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(20);

        // rowversion: SQL Server lo actualiza solo en cada UPDATE. Si dos miembros del hogar
        // registran un gasto sobre la misma cuenta a la vez, el segundo guardado falla con un
        // error de concurrencia en lugar de pisar el saldo calculado por el primero.
        constructor.Property(c => c.Version).IsRowVersion();

        constructor.HasIndex(c => new { c.EspacioId, c.Activa })
            .HasDatabaseName("IX_Cuentas_Espacio_Activa");

        constructor.HasOne<Espacio>()
            .WithMany()
            .HasForeignKey(c => c.EspacioId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne<Moneda>()
            .WithMany()
            .HasForeignKey(c => c.Moneda)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(c => c.PropietarioUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Configura las categorias jerarquicas.</summary>
public class ConfiguracionCategoria : IEntityTypeConfiguration<Categoria>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Categoria> constructor)
    {
        constructor.ToTable("Categorias");

        constructor.Property(c => c.Nombre).HasMaxLength(100).IsRequired();
        constructor.Property(c => c.Icono).HasMaxLength(50);
        constructor.Property(c => c.Color).HasMaxLength(9);
        constructor.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(20);

        // No puede haber dos categorias con el mismo nombre bajo el mismo padre dentro de un
        // espacio. Si se permitiera, los informes por categoria mostrarian dos filas
        // "Supermercado" y nadie sabria cual es cual.
        constructor.HasIndex(c => new { c.EspacioId, c.CategoriaPadreId, c.Nombre })
            .HasDatabaseName("UX_Categorias_Espacio_Padre_Nombre")
            .IsUnique();

        // Restrict, no Cascade: borrar una categoria padre no debe llevarse por delante sus
        // subcategorias ni, indirectamente, dejar movimientos huerfanos.
        constructor.HasOne(c => c.CategoriaPadre)
            .WithMany(c => c.Subcategorias)
            .HasForeignKey(c => c.CategoriaPadreId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne<Espacio>()
            .WithMany()
            .HasForeignKey(c => c.EspacioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
