using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Rumbo.Dominio.Entidades.Financiero;

namespace Rumbo.Infraestructura.Persistencia.Configuraciones;

/// <summary>Configura los traspasos entre cuentas.</summary>
public class ConfiguracionTransferenciaEntidad : IEntityTypeConfiguration<Transferencia>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Transferencia> constructor)
    {
        constructor.ToTable("Transferencias", tabla =>
        {
            tabla.HasCheckConstraint("CK_Transferencias_MontosPositivos",
                "[MontoOrigen] > 0 AND [MontoDestino] > 0");

            // Transferir dinero de una cuenta a si misma no significa nada y descuadraria
            // los informes con dos asientos que se anulan.
            tabla.HasCheckConstraint("CK_Transferencias_CuentasDistintas",
                "[CuentaOrigenId] <> [CuentaDestinoId]");

            tabla.HasCheckConstraint("CK_Transferencias_ComisionNoNegativa", "[Comision] >= 0");
        });

        constructor.Property(t => t.Descripcion).HasMaxLength(256).IsRequired();
        constructor.Property(t => t.TasaCambioAplicada).HasPrecision(19, 8);

        constructor.HasIndex(t => new { t.EspacioId, t.Fecha })
            .HasDatabaseName("IX_Transferencias_Espacio_Fecha")
            .IsDescending(false, true);

        constructor.HasOne<Cuenta>()
            .WithMany()
            .HasForeignKey(t => t.CuentaOrigenId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne<Cuenta>()
            .WithMany()
            .HasForeignKey(t => t.CuentaDestinoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Configura los gastos recurrentes.</summary>
public class ConfiguracionGastoRecurrente : IEntityTypeConfiguration<GastoRecurrente>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GastoRecurrente> constructor)
    {
        constructor.ToTable("GastosRecurrentes", tabla =>
            tabla.HasCheckConstraint("CK_GastosRecurrentes_MontoNoNegativo", "[MontoEstimado] >= 0"));

        constructor.Property(g => g.Nombre).HasMaxLength(150).IsRequired();
        constructor.Property(g => g.Moneda).HasMaxLength(3).IsFixedLength().IsRequired();
        constructor.Property(g => g.Notas).HasMaxLength(1000);

        constructor.Property(g => g.Frecuencia).HasConversion<string>().HasMaxLength(20);
        constructor.Property(g => g.Estado).HasConversion<string>().HasMaxLength(20);
        constructor.Property(g => g.Reparto).HasConversion<string>().HasMaxLength(20);

        // El panel pregunta a diario "que vence pronto": este indice es el que responde.
        constructor.HasIndex(g => new { g.EspacioId, g.Estado, g.ProximaFechaPago })
            .HasDatabaseName("IX_GastosRecurrentes_Espacio_Estado_Proxima");

        constructor.HasOne(g => g.Categoria)
            .WithMany()
            .HasForeignKey(g => g.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(g => g.Cuenta)
            .WithMany()
            .HasForeignKey(g => g.CuentaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Configura los ingresos recurrentes.</summary>
public class ConfiguracionIngresoRecurrente : IEntityTypeConfiguration<IngresoRecurrente>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IngresoRecurrente> constructor)
    {
        constructor.ToTable("IngresosRecurrentes", tabla =>
            tabla.HasCheckConstraint("CK_IngresosRecurrentes_MontoNoNegativo", "[MontoEstimado] >= 0"));

        constructor.Property(i => i.Nombre).HasMaxLength(150).IsRequired();
        constructor.Property(i => i.Moneda).HasMaxLength(3).IsFixedLength().IsRequired();
        constructor.Property(i => i.Notas).HasMaxLength(1000);

        constructor.Property(i => i.Frecuencia).HasConversion<string>().HasMaxLength(20);
        constructor.Property(i => i.Estado).HasConversion<string>().HasMaxLength(20);

        constructor.HasIndex(i => new { i.EspacioId, i.Estado, i.ProximaFechaCobro })
            .HasDatabaseName("IX_IngresosRecurrentes_Espacio_Estado_Proxima");

        constructor.HasOne(i => i.Categoria)
            .WithMany()
            .HasForeignKey(i => i.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(i => i.Cuenta)
            .WithMany()
            .HasForeignKey(i => i.CuentaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
