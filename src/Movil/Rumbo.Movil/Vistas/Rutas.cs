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

    /// <summary>Menu de las pantallas secundarias.</summary>
    public const string Mas = "//principal/mas";

    /// <summary>Presupuesto del periodo vigente.</summary>
    /// <remarks>
    /// Las rutas de abajo NO llevan doble barra: se apilan encima de la pantalla actual y
    /// el boton Atras del telefono devuelve al menu. Con doble barra se borraria el
    /// historial y Atras sacaria de la aplicacion.
    /// </remarks>
    public const string Presupuestos = "presupuestos";

    /// <summary>Metas de ahorro.</summary>
    public const string Metas = "metas";

    /// <summary>Viajes y su viabilidad.</summary>
    public const string Viajes = "viajes";

    /// <summary>Informes.</summary>
    public const string Reportes = "reportes";

    /// <summary>Ajustes de la cuenta y del hogar.</summary>
    public const string Ajustes = "ajustes";
}
