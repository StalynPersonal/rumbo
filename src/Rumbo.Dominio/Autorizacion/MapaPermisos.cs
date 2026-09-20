using Rumbo.Dominio.Enums;

namespace Rumbo.Dominio.Autorizacion;

/// <summary>
/// Define que permisos concede cada rol dentro de un espacio.
/// </summary>
/// <remarks>
/// <para>
/// Es el unico lugar del sistema donde se decide quien puede hacer que. Anadir un rol nuevo
/// consiste en anadir una entrada aqui, sin tocar ningun controlador.
/// </para>
/// <para>
/// <b>Criterio de reparto.</b> Un <c>Miembro</c> puede llevar las cuentas del dia a dia:
/// registrar movimientos y consultarlo todo. Lo que NO puede es cambiar las reglas del
/// hogar (presupuestos, metas) ni tocar a las personas. Un <c>Administrador</c> gestiona el
/// hogar completo pero no puede eliminarlo ni quitar al propietario. Solo el
/// <c>Propietario</c> puede borrar el espacio, porque es una accion irreversible que afecta
/// a todos.
/// </para>
/// </remarks>
public static class MapaPermisos
{
    /// <summary>
    /// Permisos de un miembro: opera las finanzas del dia a dia, no administra.
    /// </summary>
    private static readonly string[] DeMiembro =
    [
        Permisos.Cuentas.Leer,
        Permisos.Movimientos.Leer,
        Permisos.Movimientos.Escribir,
        Permisos.Categorias.Leer,
        Permisos.Presupuestos.Leer,
        Permisos.Metas.Leer,
        Permisos.Viajes.Leer,
        Permisos.Deudas.Leer,
        Permisos.Reportes.Leer,
        Permisos.Recomendaciones.Leer,
        Permisos.Espacio.Leer,
    ];

    /// <summary>
    /// Permisos de un administrador: todo lo del miembro mas la gestion del hogar.
    /// </summary>
    private static readonly string[] DeAdministrador =
    [
        .. DeMiembro,
        Permisos.Cuentas.Escribir,
        Permisos.Cuentas.Eliminar,
        Permisos.Movimientos.Eliminar,
        Permisos.Categorias.Escribir,
        Permisos.Presupuestos.Escribir,
        Permisos.Metas.Escribir,
        Permisos.Viajes.Escribir,
        Permisos.Deudas.Escribir,
        Permisos.Espacio.Escribir,
        Permisos.Recomendaciones.Responder,
        Permisos.Espacio.Invitar,
        Permisos.Espacio.GestionarMiembros,
        Permisos.Auditoria.Leer,
    ];

    /// <summary>
    /// Permisos de un propietario: todo lo del administrador mas eliminar el espacio.
    /// </summary>
    private static readonly string[] DePropietario =
    [
        .. DeAdministrador,
        Permisos.Espacio.Eliminar,

        // Configurar el correo del espacio implica guardar la contrasena de una cuenta de
        // correo personal. Solo quien creo el hogar deberia poder ponerla o cambiarla.
        Permisos.Espacio.ConfigurarCorreo,
    ];

    /// <summary>
    /// Devuelve los permisos que concede un rol.
    /// </summary>
    /// <param name="rol">Rol dentro del espacio.</param>
    /// <returns>Lista de permisos.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Si el rol no esta contemplado. Se lanza a proposito en lugar de devolver una lista
    /// vacia: un rol nuevo sin permisos asignados es un olvido, y fallar de forma ruidosa lo
    /// saca a la luz de inmediato en vez de dejar a alguien sin acceso sin explicacion.
    /// </exception>
    public static IReadOnlyList<string> ParaRol(RolEspacio rol) => rol switch
    {
        RolEspacio.Miembro => DeMiembro,
        RolEspacio.Administrador => DeAdministrador,
        RolEspacio.Propietario => DePropietario,
        _ => throw new ArgumentOutOfRangeException(
            nameof(rol), rol, "Rol de espacio sin permisos definidos en MapaPermisos."),
    };

    /// <summary>
    /// Indica si un rol concede un permiso concreto.
    /// </summary>
    /// <param name="rol">Rol dentro del espacio.</param>
    /// <param name="permiso">Permiso que se quiere comprobar.</param>
    /// <returns><c>true</c> si el rol lo incluye.</returns>
    public static bool Concede(RolEspacio rol, string permiso) =>
        ParaRol(rol).Contains(permiso);
}
