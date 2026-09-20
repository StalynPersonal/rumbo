using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Rumbo.Dominio.Entidades.Financiero;

namespace Rumbo.Infraestructura.Persistencia.Configuraciones;

/// <summary>Configura el catalogo de monedas.</summary>
/// <remarks>
/// Es una tabla GLOBAL: no lleva <c>EspacioId</c> ni filtro de aislamiento, porque el catalogo
/// de divisas es el mismo para todos los hogares.
/// </remarks>
public class ConfiguracionMonedaEntidad : IEntityTypeConfiguration<Moneda>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Moneda> constructor)
    {
        constructor.ToTable("Monedas");

        // La clave es el propio codigo ISO-4217: no hace falta un Guid para algo que ya
        // tiene un identificador universal, estable y legible.
        constructor.HasKey(m => m.Codigo);

        constructor.Property(m => m.Codigo).HasMaxLength(3).IsFixedLength();
        constructor.Property(m => m.Nombre).HasMaxLength(100).IsRequired();
        constructor.Property(m => m.Simbolo).HasMaxLength(10).IsRequired();
    }
}

/// <summary>Configura las tasas de cambio historicas.</summary>
public class ConfiguracionTasaCambio : IEntityTypeConfiguration<TasaCambio>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TasaCambio> constructor)
    {
        constructor.ToTable("TasasCambio", tabla =>
            tabla.HasCheckConstraint("CK_TasasCambio_TasaPositiva", "[Tasa] > 0"));

        constructor.Property(t => t.MonedaOrigen).HasMaxLength(3).IsFixedLength().IsRequired();
        constructor.Property(t => t.MonedaDestino).HasMaxLength(3).IsFixedLength().IsRequired();
        constructor.Property(t => t.Origen).HasConversion<string>().HasMaxLength(20);

        // 8 decimales, no los 4 del resto: al convertir miles de movimientos, redondear la
        // tasa a 4 decimales introduce una desviacion que se acumula.
        constructor.Property(t => t.Tasa).HasPrecision(19, 8);

        // Una sola tasa por par de monedas y fecha. Dos filas distintas para el mismo dia
        // harian que el mismo movimiento se convirtiera distinto segun cual se leyera.
        constructor.HasIndex(t => new { t.MonedaOrigen, t.MonedaDestino, t.Fecha })
            .HasDatabaseName("UX_TasasCambio_Par_Fecha")
            .IsUnique();

        // Busqueda habitual: la tasa mas reciente anterior o igual a una fecha dada.
        constructor.HasIndex(t => new { t.MonedaOrigen, t.MonedaDestino, t.Fecha })
            .HasDatabaseName("IX_TasasCambio_Busqueda")
            .IsDescending(false, false, true);

        constructor.HasOne<Moneda>()
            .WithMany()
            .HasForeignKey(t => t.MonedaOrigen)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne<Moneda>()
            .WithMany()
            .HasForeignKey(t => t.MonedaDestino)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
