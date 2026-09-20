namespace Rumbo.Dominio.Comun;

/// <summary>
/// Raiz de todas las entidades persistidas. Aporta unicamente la clave primaria.
/// </summary>
/// <remarks>
/// La clave es un <see cref="Guid"/> generado con <c>Guid.CreateVersion7()</c>. Se eligio la
/// version 7 y no la 4 porque incorpora una marca de tiempo: los identificadores salen
/// ordenados cronologicamente y no fragmentan el indice agrupado de SQL Server. Frente a un
/// entero autoincremental, tiene la ventaja de no ser adivinable desde fuera.
/// </remarks>
public abstract class EntidadBase
{
    /// <summary>
    /// Identificador unico de la entidad.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();
}
