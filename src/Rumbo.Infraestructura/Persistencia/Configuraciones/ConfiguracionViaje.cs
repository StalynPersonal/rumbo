using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Rumbo.Dominio.Entidades.Planificacion;

namespace Rumbo.Infraestructura.Persistencia.Configuraciones;

/// <summary>Configura los viajes.</summary>
public class ConfiguracionViajeEntidad : IEntityTypeConfiguration<Viaje>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Viaje> constructor)
    {
        constructor.ToTable("Viajes", tabla =>
        {
            tabla.HasCheckConstraint("CK_Viajes_Fechas", "[FechaFin] >= [FechaInicio]");
            tabla.HasCheckConstraint("CK_Viajes_PresupuestoNoNegativo", "[PresupuestoTotal] >= 0");
            tabla.HasCheckConstraint("CK_Viajes_Viajeros", "[NumeroViajeros] >= 1");
        });

        constructor.Property(v => v.Nombre).HasMaxLength(150).IsRequired();
        constructor.Property(v => v.Destino).HasMaxLength(150);
        constructor.Property(v => v.Descripcion).HasMaxLength(1000);
        constructor.Property(v => v.Moneda).HasMaxLength(3).IsFixedLength().IsRequired();

        constructor.Property(v => v.Estado).HasConversion<string>().HasMaxLength(20);

        // Se calcula restando las dos fechas: no es una columna.
        constructor.Ignore(v => v.DuracionEnDias);

        constructor.HasIndex(v => new { v.EspacioId, v.Estado, v.FechaInicio })
            .HasDatabaseName("IX_Viajes_Espacio_Estado_Inicio");

        // El fondo de ahorro del viaje es una meta. Restrict porque borrar la meta no debe
        // llevarse el viaje ni dejarlo sin su historial de ahorro.
        constructor.HasOne(v => v.Meta)
            .WithMany()
            .HasForeignKey(v => v.MetaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Configura las partidas del presupuesto de un viaje.</summary>
public class ConfiguracionLineaPresupuestoViaje : IEntityTypeConfiguration<LineaPresupuestoViaje>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LineaPresupuestoViaje> constructor)
    {
        constructor.ToTable("LineasPresupuestoViaje", tabla =>
            tabla.HasCheckConstraint("CK_LineasPresupuestoViaje_MontoNoNegativo",
                "[MontoPlanificado] >= 0"));

        constructor.Property(l => l.Categoria).HasConversion<string>().HasMaxLength(20);
        constructor.Property(l => l.Notas).HasMaxLength(500);

        // Una partida por categoria y viaje.
        constructor.HasIndex(l => new { l.ViajeId, l.Categoria })
            .HasDatabaseName("UX_LineasPresupuestoViaje_Viaje_Categoria")
            .IsUnique();

        constructor.HasOne(l => l.Viaje)
            .WithMany(v => v.Lineas)
            .HasForeignKey(l => l.ViajeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
