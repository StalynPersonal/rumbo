namespace Rumbo.Movil.Vistas;

/// <summary>
/// Las rutas de navegacion, en un solo sitio.
/// </summary>
/// <remarks>
/// Escritas como cadenas sueltas por el codigo, una ruta mal tecleada no da error de
/// compilacion: falla al ejecutar, con una excepcion que no explica nada. Aqui, al menos,
/// el compilador avisa si el nombre no existe.
///
/// La doble barra significa "ve ahi y olvida de donde venias". Las rutas de las pestanas
/// llevan DOS partes porque viven dentro del TabBar llamado "principal": con solo
/// "//panel", Shell no las encuentra.
/// </remarks>
public static class Rutas
{
    /// <summary>Pantalla de acceso.</summary>
    public const string Acceso = "//acceso";

    /// <summary>Pantalla de inicio.</summary>
    public const string Panel = "//principal/panel";

    /// <summary>Movimientos y alta rapida.</summary>
    public const string Movimientos = "//principal/movimientos";

    /// <summary>Cuentas del espacio.</summary>
    public const string Cuentas = "//principal/cuentas";
}
