namespace Rumbo.Dominio;

/// <summary>
/// Clase vacia que sirve de punto de referencia al ensamblado del Dominio.
/// Se usa en <c>typeof(MarcadorDominio).Assembly</c> para localizar el ensamblado sin escribir su
/// nombre como texto, que es fragil ante renombrados.
/// </summary>
public sealed class MarcadorDominio;
