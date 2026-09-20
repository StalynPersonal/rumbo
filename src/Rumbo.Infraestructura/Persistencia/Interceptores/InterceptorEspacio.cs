using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Rumbo.Aplicacion.Comun;
using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Infraestructura.Persistencia.Interceptores;

/// <summary>
/// Garantiza que ninguna escritura cruce la frontera entre espacios.
/// </summary>
/// <remarks>
/// <para>
/// Es la TERCERA capa del aislamiento multi-tenant. Las dos primeras (el contexto de espacio y
/// los filtros globales de consulta) protegen la LECTURA; esta protege la ESCRITURA, que es un
/// agujero distinto: un filtro de consulta no impide guardar una fila con el espacio
/// equivocado.
/// </para>
/// <para>Hace dos cosas al guardar cambios:</para>
/// <list type="number">
/// <item><description>
/// A las entidades nuevas les asigna el espacio activo. Asi ningun servicio tiene que
/// acordarse de hacerlo, y olvidarlo deja de ser posible.
/// </description></item>
/// <item><description>
/// A las entidades modificadas o borradas les comprueba el espacio y ABORTA la operacion
/// completa si pertenecen a otro. Es el ultimo cortafuegos: si por un fallo en cualquier otra
/// capa llegara aqui un registro ajeno, no se escribe.
/// </description></item>
/// </list>
/// <para>
/// Aborta lanzando una excepcion, no ignorando la fila en silencio: un intento de escribir en
/// otro espacio es un sintoma de que algo esta mal, y esconderlo solo retrasa el diagnostico.
/// </para>
/// </remarks>
/// <param name="contextoEspacio">Espacio activo de la peticion.</param>
public class InterceptorEspacio(IContextoEspacio contextoEspacio) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData datos,
        InterceptionResult<int> resultado)
    {
        AplicarEspacio(datos.Context);
        return base.SavingChanges(datos, resultado);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData datos,
        InterceptionResult<int> resultado,
        CancellationToken cancelacion = default)
    {
        AplicarEspacio(datos.Context);
        return base.SavingChangesAsync(datos, resultado, cancelacion);
    }

    /// <summary>
    /// Asigna el espacio a las entidades nuevas y verifica el de las modificadas.
    /// </summary>
    /// <param name="contexto">Contexto que esta guardando los cambios.</param>
    /// <exception cref="ExcepcionEspacioNoCoincide">
    /// Si alguna entidad pertenece a un espacio distinto del activo.
    /// </exception>
    private void AplicarEspacio(DbContext? contexto)
    {
        if (contexto is null || !contextoEspacio.HayEspacio)
        {
            // Sin espacio activo no hay nada que asignar. Ocurre en el registro de usuarios,
            // en la creacion de un espacio y en las gestiones del administrador de plataforma.
            // Esas operaciones asignan el espacio de forma explicita en su propio servicio.
            return;
        }

        var espacioActivo = contextoEspacio.ObtenerEspacioObligatorio();

        foreach (var entrada in contexto.ChangeTracker.Entries<IEntidadDeEspacio>())
        {
            switch (entrada.State)
            {
                case EntityState.Added:
                    if (entrada.Entity.EspacioId == Guid.Empty)
                    {
                        entrada.Entity.EspacioId = espacioActivo;
                    }
                    else if (entrada.Entity.EspacioId != espacioActivo)
                    {
                        throw ExcepcionEspacioNoCoincide.AlCrear(
                            entrada.Entity.GetType().Name, entrada.Entity.EspacioId, espacioActivo);
                    }

                    break;

                case EntityState.Modified:
                case EntityState.Deleted:
                    // Se compara con el valor ORIGINAL, el que tenia la fila al leerse de la
                    // base de datos. Si se comparara con el actual, bastaria con cambiar la
                    // propiedad en memoria para saltarse la comprobacion.
                    var espacioOriginal = entrada.OriginalValues
                        .GetValue<Guid>(nameof(IEntidadDeEspacio.EspacioId));

                    if (espacioOriginal != espacioActivo)
                    {
                        throw ExcepcionEspacioNoCoincide.AlModificar(
                            entrada.Entity.GetType().Name, espacioOriginal, espacioActivo);
                    }

                    break;

                default:
                    break;
            }
        }
    }
}
