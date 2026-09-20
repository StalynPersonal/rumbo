namespace Rumbo.Aplicacion.Contratos;

/// <summary>
/// Traduce identificadores de usuario a nombres legibles.
/// </summary>
/// <remarks>
/// Existe porque los usuarios viven en las tablas de Identity, que son cosa de la capa de
/// infraestructura. Un informe necesita escribir «María» junto a una cifra, pero la capa de
/// aplicacion no debe saber que detras hay un <c>IdentityDbContext</c>: con esta interfaz
/// pide nombres y no se entera de donde salen.
/// </remarks>
public interface IDirectorioUsuarios
{
    /// <summary>Devuelve el nombre de cada usuario pedido.</summary>
    /// <param name="usuarioIds">Usuarios consultados.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>
    /// Un diccionario de identificador a nombre. Los que no existan simplemente no aparecen.
    /// </returns>
    Task<IReadOnlyDictionary<Guid, string>> ObtenerNombresAsync(
        IEnumerable<Guid> usuarioIds,
        CancellationToken cancelacion = default);
}
