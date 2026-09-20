namespace Rumbo.Dominio.Excepciones;

/// <summary>
/// Error provocado por una regla de negocio, no por un fallo tecnico.
/// </summary>
/// <remarks>
/// Distinguirlo de una excepcion cualquiera permite que la API responda con un 400 y un
/// mensaje util para la persona, en lugar de con un 500 generico.
/// </remarks>
/// <param name="mensaje">Descripcion del problema, en espanol y comprensible.</param>
public class ExcepcionDominio(string mensaje) : Exception(mensaje);
