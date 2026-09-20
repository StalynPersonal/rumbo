using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Rumbo.Dominio.Entidades.Identidad;
using Rumbo.Dominio.Entidades.Soporte;

namespace Rumbo.Infraestructura.Persistencia.Configuraciones;

/// <summary>Configura el servidor SMTP de la plataforma.</summary>
/// <remarks>
/// Tabla global de una sola fila. No lleva filtro de espacio: la usan los correos que no
/// pertenecen a ningun hogar.
/// </remarks>
public class ConfiguracionCorreoPlataformaEf : IEntityTypeConfiguration<ConfiguracionCorreoPlataforma>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ConfiguracionCorreoPlataforma> constructor)
    {
        constructor.ToTable("ConfiguracionCorreoPlataforma", tabla =>
            tabla.HasCheckConstraint(
                "CK_ConfiguracionCorreoPlataforma_Puerto", "[Puerto] BETWEEN 1 AND 65535"));

        AplicarComunes(constructor);

        constructor.Property(c => c.UrlBase).HasMaxLength(256);
    }

    /// <summary>Aplica las restricciones comunes a las dos tablas de correo.</summary>
    /// <typeparam name="T">Tipo concreto de configuracion.</typeparam>
    /// <param name="constructor">Constructor de la entidad.</param>
    internal static void AplicarComunes<T>(EntityTypeBuilder<T> constructor)
        where T : ConfiguracionCorreoBase
    {
        constructor.Property(c => c.Host).HasMaxLength(256);
        constructor.Property(c => c.Usuario).HasMaxLength(256);
        constructor.Property(c => c.RemitenteCorreo).HasMaxLength(256);
        constructor.Property(c => c.RemitenteNombre).HasMaxLength(150);
        constructor.Property(c => c.UltimoErrorPrueba).HasMaxLength(500);

        // La contraseña cifrada con Data Protection es bastante más larga que el original:
        // lleva cabecera, vector de inicialización y firma. 2000 caracteres dan margen de
        // sobra sin recurrir a nvarchar(max).
        constructor.Property(c => c.ClaveCifrada).HasMaxLength(2000);
    }
}

/// <summary>Configura el servidor SMTP de cada espacio.</summary>
public class ConfiguracionCorreoEspacioEf : IEntityTypeConfiguration<ConfiguracionCorreoEspacio>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ConfiguracionCorreoEspacio> constructor)
    {
        constructor.ToTable("ConfiguracionesCorreoEspacio", tabla =>
            tabla.HasCheckConstraint(
                "CK_ConfiguracionesCorreoEspacio_Puerto", "[Puerto] BETWEEN 1 AND 65535"));

        ConfiguracionCorreoPlataformaEf.AplicarComunes(constructor);

        // Un solo servidor por espacio.
        constructor.HasIndex(c => c.EspacioId)
            .HasDatabaseName("UX_ConfiguracionesCorreoEspacio_Espacio")
            .IsUnique();

        constructor.HasOne<Espacio>()
            .WithMany()
            .HasForeignKey(c => c.EspacioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
