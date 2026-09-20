using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Rumbo.Api.Autorizacion;

/// <summary>
/// Crea al vuelo una politica de autorizacion por cada permiso que se use.
/// </summary>
/// <remarks>
/// <para>
/// Sin esto habria que registrar a mano una politica por permiso en <c>Program.cs</c>, y
/// cada permiso nuevo exigiria acordarse de anadirla. Olvidarlo produciria un error confuso
/// en tiempo de ejecucion ("no existe la politica X") en lugar de funcionar sin mas.
/// </para>
/// <para>
/// Este proveedor reconoce las politicas que empiezan por <c>permiso:</c> y las construye
/// sobre la marcha con el requisito correspondiente.
/// </para>
/// </remarks>
/// <param name="opciones">Opciones de autorizacion de ASP.NET Core.</param>
public class ProveedorPoliticasPermiso(IOptions<AuthorizationOptions> opciones)
    : DefaultAuthorizationPolicyProvider(opciones)
{
    /// <inheritdoc />
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var permiso = PoliticaPermiso.ExtraerPermiso(policyName);

        if (permiso is null)
        {
            // No es una politica de permiso: que la resuelva el proveedor por defecto.
            return await base.GetPolicyAsync(policyName);
        }

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new RequisitoPermiso(permiso))
            .Build();
    }
}

/// <summary>Requisito de autorizacion que representa un permiso concreto.</summary>
/// <param name="permiso">Permiso necesario.</param>
public class RequisitoPermiso(string permiso) : IAuthorizationRequirement
{
    /// <summary>Permiso que debe tener quien llama.</summary>
    public string Permiso { get; } = permiso;
}
