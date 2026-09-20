using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Rumbo.Dominio.Entidades.Planificacion;

namespace Rumbo.Infraestructura.Persistencia.Configuraciones;

/// <summary>Configura los presupuestos.</summary>
public class ConfiguracionPresupuestoEntidad : IEntityTypeConfiguration<Presupuesto>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Presupuesto> constructor)
    {
        constructor.ToTable("Presupuestos", tabla =>
            tabla.HasCheckConstraint("CK_Presupuestos_Periodo", "[FinPeriodo] >= [InicioPeriodo]"));

        constructor.Property(p => p.Nombre).HasMaxLength(150).IsRequired();
        constructor.Property(p => p.Moneda).HasMaxLength(3).IsFixedLength().IsRequired();
        constructor.Property(p => p.Notas).HasMaxLength(1000);

        constructor.Property(p => p.TipoPeriodo).HasConversion<string>().HasMaxLength(20);
        constructor.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20);

        // Al abrir el panel se busca el presupuesto vigente para la fecha de hoy.
        constructor.HasIndex(p => new { p.EspacioId, p.Estado, p.InicioPeriodo, p.FinPeriodo })
            .HasDatabaseName("IX_Presupuestos_Espacio_Estado_Periodo");
    }
}

/// <summary>Configura las partidas de presupuesto.</summary>
public class ConfiguracionLineaPresupuesto : IEntityTypeConfiguration<LineaPresupuesto>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LineaPresupuesto> constructor)
    {
        constructor.ToTable("LineasPresupuesto", tabla =>
            tabla.HasCheckConstraint("CK_LineasPresupuesto_MontoNoNegativo", "[MontoAsignado] >= 0"));

        constructor.Property(l => l.Notas).HasMaxLength(500);
        constructor.Property(l => l.UmbralAviso).HasPrecision(5, 2);
        constructor.Property(l => l.UmbralCritico).HasPrecision(5, 2);
        constructor.Property(l => l.UmbralExcedido).HasPrecision(5, 2);

        // Una categoria no puede aparecer dos veces en el mismo presupuesto: si apareciera,
        // el consumo se compararia contra un limite ambiguo.
        constructor.HasIndex(l => new { l.PresupuestoId, l.CategoriaId })
            .HasDatabaseName("UX_LineasPresupuesto_Presupuesto_Categoria")
            .IsUnique();

        // Cascade aqui SI es correcto: una partida no significa nada sin su presupuesto, y no
        // contiene movimientos ni dinero, solo una cifra prevista.
        constructor.HasOne(l => l.Presupuesto)
            .WithMany(p => p.Lineas)
            .HasForeignKey(l => l.PresupuestoId)
            .OnDelete(DeleteBehavior.Cascade);

        constructor.HasOne(l => l.Categoria)
            .WithMany()
            .HasForeignKey(l => l.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
