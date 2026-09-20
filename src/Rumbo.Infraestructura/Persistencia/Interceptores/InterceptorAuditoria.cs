using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Rumbo.Aplicacion.Comun;
using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Entidades.Soporte;
using Rumbo.Dominio.Enums;

namespace Rumbo.Infraestructura.Persistencia.Interceptores;

/// <summary>
/// Rellena los campos de auditoria y escribe el historial de cambios.
/// </summary>
/// <remarks>
/// <para>
/// Se hace en un interceptor y no en cada servicio a proposito: si dependiera de que alguien
/// se acuerde de registrar la accion, el historial tendria huecos justo en las operaciones
/// menos habituales, que son las que mas interesa auditar.
/// </para>
/// <para>
/// <b>Datos sensibles.</b> Hay columnas que nunca se copian al historial, por mucho que
/// cambien: hashes de contrasena, hashes de token y codigos de invitacion. Auditar sirve para
/// saber QUE se modifico, no para dejar una segunda copia de los secretos.
/// </para>
/// </remarks>
/// <param name="usuarioActual">Usuario que realiza la operacion.</param>
/// <param name="fechaHora">Proveedor de la fecha y hora actuales.</param>
public class InterceptorAuditoria(
    IUsuarioActual usuarioActual,
    IProveedorFechaHora fechaHora) : SaveChangesInterceptor
{
    /// <summary>
    /// Propiedades cuyo valor nunca se registra en el historial de auditoria.
    /// </summary>
    private static readonly HashSet<string> PropiedadesSensibles =
    [
        "PasswordHash",
        "SecurityStamp",
        "ConcurrencyStamp",
        "Hash",
        "HashCodigo",
        "TwoFactorEnabled",
    ];

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData datos,
        InterceptionResult<int> resultado)
    {
        Auditar(datos.Context);
        return base.SavingChanges(datos, resultado);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData datos,
        InterceptionResult<int> resultado,
        CancellationToken cancelacion = default)
    {
        Auditar(datos.Context);
        return base.SavingChangesAsync(datos, resultado, cancelacion);
    }

    /// <summary>Rellena las marcas de auditoria y genera los registros del historial.</summary>
    /// <param name="contexto">Contexto que esta guardando los cambios.</param>
    private void Auditar(DbContext? contexto)
    {
        if (contexto is null)
        {
            return;
        }

        var ahora = fechaHora.AhoraUtc;
        var usuario = usuarioActual.UsuarioId;

        // Se materializa la lista ANTES de anadir los registros de auditoria: si se recorriera
        // el rastreador mientras se le agregan entidades, la iteracion fallaria.
        var entradas = contexto.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var registros = new List<RegistroAuditoria>();

        foreach (var entrada in entradas)
        {
            if (entrada.Entity is IAuditable auditable)
            {
                if (entrada.State == EntityState.Added)
                {
                    auditable.FechaCreacion = ahora;
                    auditable.CreadoPorUsuarioId = usuario;
                }
                else if (entrada.State == EntityState.Modified)
                {
                    auditable.FechaActualizacion = ahora;
                    auditable.ActualizadoPorUsuarioId = usuario;

                    // Nadie puede reescribir quien creo un registro ni cuando.
                    entrada.Property(nameof(IAuditable.FechaCreacion)).IsModified = false;
                    entrada.Property(nameof(IAuditable.CreadoPorUsuarioId)).IsModified = false;
                }
            }

            // El propio historial no se audita: se entraria en un bucle infinito.
            if (entrada.Entity is RegistroAuditoria)
            {
                continue;
            }

            var registro = ConstruirRegistro(entrada, ahora, usuario);
            if (registro is not null)
            {
                registros.Add(registro);
            }
        }

        if (registros.Count > 0)
        {
            contexto.Set<RegistroAuditoria>().AddRange(registros);
        }
    }

    /// <summary>Crea la fila de historial correspondiente a un cambio.</summary>
    /// <param name="entrada">Entrada del rastreador de cambios.</param>
    /// <param name="ahora">Instante de la operacion.</param>
    /// <param name="usuario">Usuario que la realiza.</param>
    /// <returns>El registro, o <c>null</c> si el cambio no merece auditarse.</returns>
    private static RegistroAuditoria? ConstruirRegistro(
        EntityEntry entrada,
        DateTimeOffset ahora,
        Guid? usuario)
    {
        var accion = entrada.State switch
        {
            EntityState.Added => AccionAuditoria.Creacion,
            EntityState.Deleted => AccionAuditoria.Eliminacion,
            _ => AccionAuditoria.Actualizacion,
        };

        // Un borrado logico llega aqui como modificacion; se detecta por la marca.
        if (accion == AccionAuditoria.Actualizacion
            && entrada.Entity is IBorradoLogico { Eliminado: true }
            && entrada.Property(nameof(IBorradoLogico.Eliminado)).IsModified)
        {
            accion = AccionAuditoria.Eliminacion;
        }

        var cambios = new Dictionary<string, object?>();

        foreach (var propiedad in entrada.Properties)
        {
            var nombre = propiedad.Metadata.Name;

            if (PropiedadesSensibles.Contains(nombre))
            {
                continue;
            }

            if (entrada.State == EntityState.Added)
            {
                cambios[nombre] = propiedad.CurrentValue;
            }
            else if (propiedad.IsModified)
            {
                cambios[nombre] = new
                {
                    antes = propiedad.OriginalValue,
                    despues = propiedad.CurrentValue,
                };
            }
        }

        if (cambios.Count == 0)
        {
            return null;
        }

        var entidad = entrada.Entity;

        return new RegistroAuditoria
        {
            EspacioId = entidad is IEntidadDeEspacio deEspacio ? deEspacio.EspacioId : null,
            UsuarioId = usuario,
            Accion = accion,
            TipoEntidad = entidad.GetType().Name,
            EntidadId = entidad is EntidadBase conId ? conId.Id : null,
            Cambios = JsonSerializer.Serialize(cambios),
            FechaHora = ahora,
        };
    }
}
