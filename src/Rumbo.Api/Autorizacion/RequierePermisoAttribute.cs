using Microsoft.AspNetCore.Authorization;

namespace Rumbo.Api.Autorizacion;

/// <summary>
/// Exige que quien llama tenga un permiso concreto dentro de su espacio activo.
/// </summary>
/// <remarks>
/// <para>
/// Se usa asi sobre una accion del controlador:
/// </para>
/// <code>
/// [RequierePermiso(Permisos.Movimientos.Escribir)]
/// public async Task&lt;IActionResult&gt; Crear(...)
/// </code>
/// <para>
/// Declarar el PERMISO y no el rol es lo que permite anadir manana un rol nuevo sin repasar
/// todos los controladores: basta con decidir en <c>MapaPermisos</c> que permisos concede.
/// </para>
/// </remarks>
/// <param name="permiso">Permiso necesario, tomado de la clase <c>Permisos</c>.</param>
public class RequierePermisoAttribute(string permiso)
    : AuthorizeAttribute(PoliticaPermiso.Prefijo + permiso);

/// <summary>
/// Convenciones para construir el nombre de las politicas basadas en permisos.
/// </summary>
public static class PoliticaPermiso
{
    /// <summary>Prefijo que identifica una politica de permiso.</summary>
    public const string Prefijo = "permiso:";

    /// <summary>Extrae el permiso del nombre de una politica.</summary>
    /// <param name="nombrePolitica">Nombre completo de la politica.</param>
    /// <returns>El permiso, o <c>null</c> si la politica no es de permiso.</returns>
    public static string? ExtraerPermiso(string nombrePolitica) =>
        nombrePolitica.StartsWith(Prefijo, StringComparison.Ordinal)
            ? nombrePolitica[Prefijo.Length..]
            : null;
}
