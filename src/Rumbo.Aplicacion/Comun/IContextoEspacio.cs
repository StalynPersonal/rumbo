namespace Rumbo.Aplicacion.Comun;

/// <summary>
/// Da acceso al espacio sobre el que trabaja la peticion actual.
/// </summary>
/// <remarks>
/// <para>
/// Es la pieza central del aislamiento multi-tenant. El valor lo establece
/// <c>MiddlewareResolucionEspacio</c> a partir del token firmado por el servidor y de una
/// comprobacion en base de datos de que el usuario tiene una membresia ACTIVA.
/// </para>
/// <para>
/// <b>Nunca</b> se toma de un parametro de la peticion. Si el espacio viniera en el cuerpo o
/// en una cabecera, cualquiera podria pedir los datos de otro hogar cambiando un identificador.
/// </para>
/// <para>
/// Se registra con ambito <i>scoped</i>: una instancia por peticion.
/// </para>
/// </remarks>
public interface IContextoEspacio
{
    /// <summary>
    /// Espacio activo, o <c>null</c> en peticiones sin espacio (inicio de sesion, endpoint de
    /// salud, gestion del administrador de plataforma).
    /// </summary>
    Guid? EspacioId { get; }

    /// <summary>Indica si la peticion tiene un espacio resuelto.</summary>
    bool HayEspacio { get; }

    /// <summary>
    /// Devuelve el espacio activo o lanza una excepcion si no lo hay.
    /// </summary>
    /// <returns>Identificador del espacio activo.</returns>
    /// <exception cref="InvalidOperationException">
    /// Si se invoca en una peticion que no tiene espacio resuelto. Es preferible fallar de
    /// forma ruidosa a devolver un <c>Guid.Empty</c> que acabaria filtrando o guardando datos
    /// en un espacio equivocado.
    /// </exception>
    Guid ObtenerEspacioObligatorio();
}
