using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Rumbo.Dominio.Entidades.Planificacion;

namespace Rumbo.Infraestructura.Persistencia.Configuraciones;

/// <summary>Configura las deudas.</summary>
public class ConfiguracionDeudaEntidad : IEntityTypeConfiguration<Deuda>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Deuda> constructor)
    {
        constructor.ToTable("Deudas", tabla =>
        {
            tabla.HasCheckConstraint("CK_Deudas_MontoOriginalPositivo", "[MontoOriginal] > 0");
            tabla.HasCheckConstraint("CK_Deudas_SaldoNoNegativo", "[SaldoActual] >= 0");
            tabla.HasCheckConstraint("CK_Deudas_Vencimiento",
                "[DiaVencimiento] IS NULL OR [DiaVencimiento] BETWEEN 1 AND 28");
        });

        constructor.Property(d => d.Nombre).HasMaxLength(150).IsRequired();
        constructor.Property(d => d.Acreedor).HasMaxLength(150);
        constructor.Property(d => d.Moneda).HasMaxLength(3).IsFixedLength().IsRequired();
        constructor.Property(d => d.Notas).HasMaxLength(1000);

        constructor.Property(d => d.Tipo).HasConversion<string>().HasMaxLength(20);
        constructor.Property(d => d.Estado).HasConversion<string>().HasMaxLength(20);

        // Porcentaje anual con dos decimales: 18,50 por ciento.
        constructor.Property(d => d.TasaInteres).HasPrecision(6, 3);

        constructor.Ignore(d => d.PorcentajePagado);

        constructor.HasIndex(d => new { d.EspacioId, d.Estado })
            .HasDatabaseName("IX_Deudas_Espacio_Estado");

        constructor.HasOne(d => d.CuentaVinculada)
            .WithMany()
            .HasForeignKey(d => d.CuentaVinculadaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Configura los pagos de deuda.</summary>
public class ConfiguracionPagoDeuda : IEntityTypeConfiguration<PagoDeuda>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PagoDeuda> constructor)
    {
        constructor.ToTable("PagosDeuda", tabla =>
        {
            tabla.HasCheckConstraint("CK_PagosDeuda_TotalPositivo", "[MontoTotal] > 0");

            // El desglose tiene que sumar el total. Si no cuadrara, la deuda bajaria a un
            // ritmo distinto del que dicen los pagos y nadie sabria cual de los dos creer.
            tabla.HasCheckConstraint("CK_PagosDeuda_DesgloseCuadra",
                "[MontoCapital] + [MontoInteres] + [MontoCargos] = [MontoTotal]");

            tabla.HasCheckConstraint("CK_PagosDeuda_ComponentesNoNegativos",
                "[MontoCapital] >= 0 AND [MontoInteres] >= 0 AND [MontoCargos] >= 0");
        });

        constructor.Property(p => p.Moneda).HasMaxLength(3).IsFixedLength().IsRequired();
        constructor.Property(p => p.Notas).HasMaxLength(500);

        constructor.HasIndex(p => new { p.DeudaId, p.Fecha })
            .HasDatabaseName("IX_PagosDeuda_Deuda_Fecha")
            .IsDescending(false, true);

        constructor.HasOne(p => p.Deuda)
            .WithMany(d => d.Pagos)
            .HasForeignKey(p => p.DeudaId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(p => p.Movimiento)
            .WithMany()
            .HasForeignKey(p => p.MovimientoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
