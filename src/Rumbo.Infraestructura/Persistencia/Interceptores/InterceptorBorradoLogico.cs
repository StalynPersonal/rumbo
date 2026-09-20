using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Rumbo.Aplicacion.Comun;
using Rumbo.Dominio.Comun;

namespace Rumbo.Infraestructura.Persistencia.Interceptores;

/// <summary>
/// Convierte cualquier borrado de una entidad financiera en un borrado logico.
/// </summary>
/// <remarks>
/// <para>
/// En un sistema financiero, borrar una fila destruye informacion que despues hace falta: la
/// reconciliacion de saldos deja de cuadrar, la auditoria queda incompleta y no hay forma de
/// deshacer un error.
/// </para>
/// <para>
/// Este interceptor permite que el codigo llame a <c>Remove</c> con naturalidad y se encarga
/// de traducirlo: la fila se marca como eliminada y desaparece de las consultas por efecto del
/// filtro global, pero sigue en la base de datos.
/// </para>
/// <para>
/// El orden importa: se ejecuta ANTES del interceptor de auditoria, para que el cambio quede
/// registrado como una modificacion y no como un borrado.
/// </para>
/// </remarks>
/// <param name="usuarioActual">Usuario que realiza la operacion.</param>
/// <param name="fechaHora">Proveedor de la fecha y hora actuales.</param>
public class InterceptorBorradoLogico(
    IUsuarioActual usuarioActual,
    IProveedorFechaHora fechaHora) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData datos,
        InterceptionResult<int> resultado)
    {
        ConvertirBorrados(datos.Context);
        return base.SavingChanges(datos, resultado);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData datos,
        InterceptionResult<int> resultado,
        CancellationToken cancelacion = default)
    {
        ConvertirBorrados(datos.Context);
        return base.SavingChangesAsync(datos, resultado, cancelacion);
    }

    /// <summary>Transforma los borrados fisicos en marcas de borrado.</summary>
    /// <param name="contexto">Contexto que esta guardando los cambios.</param>
    private void ConvertirBorrados(DbContext? contexto)
    {
        if (contexto is null)
        {
            return;
        }

        foreach (var entrada in contexto.ChangeTracker.Entries<IBorradoLogico>())
        {
            if (entrada.State != EntityState.Deleted)
            {
                continue;
            }

            entrada.State = EntityState.Modified;

            entrada.Entity.Eliminado = true;
            entrada.Entity.FechaEliminacion = fechaHora.AhoraUtc;
            entrada.Entity.EliminadoPorUsuarioId = usuarioActual.UsuarioId;
        }
    }
}
