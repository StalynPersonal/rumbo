using Microsoft.AspNetCore.Authorization;

using Rumbo.Aplicacion.Comun;
using Rumbo.Dominio.Autorizacion;

namespace Rumbo.Api.Autorizacion;

/// <summary>
/// Decide si quien hace la peticion tiene el permiso exigido.
/// </summary>
/// <remarks>
/// <para>
/// El permiso se deriva del rol que el usuario tiene EN SU ESPACIO ACTIVO, y ese rol lo
/// verifico el middleware de resolucion contra la base de datos. No se lee del token
/// directamente: si se leyera, expulsar a alguien del hogar no surtiria efecto hasta que su
/// token caducara.
/// </para>
/// <para>
/// <b>El administrador de plataforma NO recibe permisos financieros por este camino.</b>
/// Gestiona espacios, usuarios e invitaciones desde su propio modulo, pero aqui se le trata
/// como a cualquiera: sin membresia en un espacio, no tiene ningun permiso sobre el.
/// </para>
/// </remarks>
/// <param name="usuarioActual">Identidad y rol de quien hace la peticion.</param>
public class ManejadorPermisos(IUsuarioActual usuarioActual)
    : AuthorizationHandler<RequisitoPermiso>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext contexto,
        RequisitoPermiso requisito)
    {
        if (!usuarioActual.EstaAutenticado)
        {
            return Task.CompletedTask;
        }

        // Sin rol verificado en un espacio no hay ningun permiso sobre datos financieros.
        if (usuarioActual.RolEnEspacio is not { } rol)
        {
            return Task.CompletedTask;
        }

        if (MapaPermisos.Concede(rol, requisito.Permiso))
        {
            contexto.Succeed(requisito);
        }

        return Task.CompletedTask;
    }
}
