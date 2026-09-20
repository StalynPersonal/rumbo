using Rumbo.Aplicacion.Comun;

namespace Rumbo.Infraestructura.MultiEspacio;

/// <summary>
/// Implementacion del contexto de espacio para una peticion.
/// </summary>
/// <remarks>
/// <para>
/// Se registra con ambito <i>scoped</i>: hay una instancia por peticion HTTP, y todo lo que
/// participa en esa peticion (el <c>ContextoRumbo</c>, los interceptores, los servicios) ve el
/// mismo valor.
/// </para>
/// <para>
/// El valor solo puede establecerse UNA vez y a traves de <see cref="Establecer"/>, que es lo
/// que invoca el middleware de resolucion despues de verificar en base de datos que el usuario
/// tiene una membresia activa. Impedir que se cambie despues evita el peor escenario posible:
/// que a mitad de una operacion el espacio cambie y una parte de los datos se escriba en el
/// hogar equivocado.
/// </para>
/// </remarks>
public class ContextoEspacio : IContextoEspacio
{
    /// <inheritdoc />
    public Guid? EspacioId { get; private set; }

    /// <inheritdoc />
    public bool HayEspacio => EspacioId.HasValue;

    /// <inheritdoc />
    public Guid ObtenerEspacioObligatorio() =>
        EspacioId ?? throw new InvalidOperationException(
            "No hay un espacio activo en esta peticion. Es un error de programacion: la "
            + "operacion solicitada requiere espacio, pero la ruta no paso por el middleware "
            + "de resolucion o el usuario no tiene una membresia activa.");

    /// <summary>
    /// Fija el espacio activo de la peticion.
    /// </summary>
    /// <param name="espacioId">Espacio verificado del usuario autenticado.</param>
    /// <exception cref="InvalidOperationException">Si ya se habia fijado.</exception>
    /// <remarks>
    /// Lo llama unicamente el middleware de resolucion de espacio, y solo despues de
    /// comprobar contra la base de datos que la membresia esta activa. Nunca debe llamarse
    /// con un valor que venga del cuerpo o de una cabecera de la peticion.
    /// </remarks>
    public void Establecer(Guid espacioId)
    {
        if (EspacioId.HasValue)
        {
            throw new InvalidOperationException(
                "El espacio de la peticion ya estaba establecido y no puede cambiarse.");
        }

        if (espacioId == Guid.Empty)
        {
            throw new ArgumentException("El espacio no puede ser vacio.", nameof(espacioId));
        }

        EspacioId = espacioId;
    }
}
