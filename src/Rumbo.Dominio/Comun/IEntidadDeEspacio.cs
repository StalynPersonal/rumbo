namespace Rumbo.Dominio.Comun;

/// <summary>
/// Marca una entidad que pertenece a un <c>Espacio</c> concreto y que, por tanto, jamas debe
/// ser visible desde otro espacio.
/// </summary>
/// <remarks>
/// <para>
/// Esta interfaz es la pieza central del aislamiento multi-tenant. <c>ContextoRumbo</c> recorre
/// el modelo al construirlo y aplica un filtro global de consulta a TODA entidad que la
/// implemente, de modo que el aislamiento no depende de que alguien se acuerde de escribir
/// <c>WHERE EspacioId = ...</c> en cada consulta.
/// </para>
/// <para>
/// Consecuencia practica: si creas una entidad nueva que guarda datos de un hogar y olvidas
/// implementar esta interfaz, sus filas serian visibles para todos los espacios. Por eso existe
/// una prueba de arquitectura que enumera las entidades del modelo y exige que cada una este
/// marcada con esta interfaz o declarada explicitamente como global.
/// </para>
/// </remarks>
public interface IEntidadDeEspacio
{
    /// <summary>
    /// Espacio (hogar, pareja, familia o negocio) al que pertenece el registro.
    /// </summary>
    Guid EspacioId { get; set; }
}
