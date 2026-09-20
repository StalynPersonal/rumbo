using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Entidades.Identidad;
using Rumbo.Infraestructura.Identidad;

namespace Rumbo.Infraestructura.Persistencia.Configuraciones;

/// <summary>
/// Configura la tabla central del sistema: el libro mayor.
/// </summary>
/// <remarks>
/// Es la tabla que mas crece y la que mas se consulta, asi que sus indices son los que
/// determinan si el panel abre en medio segundo o en diez.
/// </remarks>
public class ConfiguracionMovimiento : IEntityTypeConfiguration<Movimiento>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Movimiento> constructor)
    {
        constructor.ToTable("Movimientos", tabla =>
        {
            // El importe SIEMPRE es positivo; la direccion la marca el signo. Sin esta
            // restriccion, un gasto grabado en negativo aumentaria el saldo en silencio.
            tabla.HasCheckConstraint("CK_Movimientos_MontoPositivo", "[Monto] > 0");

            tabla.HasCheckConstraint("CK_Movimientos_Signo", "[Signo] IN (-1, 1)");

            tabla.HasCheckConstraint("CK_Movimientos_TasaPositiva", "[TasaCambioAplicada] > 0");
        });

        constructor.Property(m => m.Descripcion).HasMaxLength(256).IsRequired();
        constructor.Property(m => m.Notas).HasMaxLength(1000);
        constructor.Property(m => m.Moneda).HasMaxLength(3).IsFixedLength().IsRequired();

        constructor.Property(m => m.Tipo).HasConversion<string>().HasMaxLength(20);
        constructor.Property(m => m.Reparto).HasConversion<string>().HasMaxLength(20);
        constructor.Property(m => m.MetodoPago).HasConversion<string>().HasMaxLength(20);

        constructor.Property(m => m.TasaCambioAplicada).HasPrecision(19, 8);

        // --- Indices -------------------------------------------------------
        // EspacioId va SIEMPRE primero: es la columna por la que filtra cada consulta por
        // efecto del filtro global, y ademas deja el aislamiento a la vista en el plan de
        // ejecucion de SQL Server.

        // El que usa el panel y casi todos los reportes. Las columnas incluidas evitan que
        // SQL Server tenga que ir a buscar la fila completa.
        constructor.HasIndex(m => new { m.EspacioId, m.FechaMovimiento })
            .HasDatabaseName("IX_Movimientos_Espacio_Fecha")
            .IsDescending(false, true)
            .IncludeProperties(m => new { m.Monto, m.MontoEnMonedaBase, m.Tipo, m.CuentaId, m.CategoriaId })
            .HasFilter("[Eliminado] = 0");

        constructor.HasIndex(m => new { m.EspacioId, m.CuentaId, m.FechaMovimiento })
            .HasDatabaseName("IX_Movimientos_Espacio_Cuenta_Fecha")
            .HasFilter("[Eliminado] = 0");

        constructor.HasIndex(m => new { m.EspacioId, m.CategoriaId, m.FechaMovimiento })
            .HasDatabaseName("IX_Movimientos_Espacio_Categoria_Fecha")
            .HasFilter("[Eliminado] = 0");

        // Para recuperar las dos patas de una transferencia de una sola vez.
        constructor.HasIndex(m => m.TransferenciaId)
            .HasDatabaseName("IX_Movimientos_Transferencia")
            .HasFilter("[TransferenciaId] IS NOT NULL");

        constructor.HasIndex(m => new { m.EspacioId, m.MetaId })
            .HasDatabaseName("IX_Movimientos_Espacio_Meta")
            .HasFilter("[MetaId] IS NOT NULL");

        constructor.HasIndex(m => new { m.EspacioId, m.ViajeId })
            .HasDatabaseName("IX_Movimientos_Espacio_Viaje")
            .HasFilter("[ViajeId] IS NOT NULL");

        // --- Relaciones ----------------------------------------------------
        // Todo en Restrict: ninguna cascada debe poder borrar historial financiero. Si alguien
        // borra una categoria con movimientos, la operacion debe fallar y obligar a decidir
        // que hacer con ellos, no llevarselos por delante.

        constructor.HasOne(m => m.Cuenta)
            .WithMany(c => c.Movimientos)
            .HasForeignKey(m => m.CuentaId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(m => m.Categoria)
            .WithMany(c => c.Movimientos)
            .HasForeignKey(m => m.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(m => m.Transferencia)
            .WithMany()
            .HasForeignKey(m => m.TransferenciaId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(m => m.Meta)
            .WithMany()
            .HasForeignKey(m => m.MetaId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(m => m.Viaje)
            .WithMany(v => v.Movimientos)
            .HasForeignKey(m => m.ViajeId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne<Espacio>()
            .WithMany()
            .HasForeignKey(m => m.EspacioId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(m => m.PagadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
