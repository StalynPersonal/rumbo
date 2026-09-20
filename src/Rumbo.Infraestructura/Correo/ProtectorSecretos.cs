using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

using Rumbo.Aplicacion.Contratos;

namespace Rumbo.Infraestructura.Correo;

/// <summary>
/// Cifra secretos con la proteccion de datos de ASP.NET Core.
/// </summary>
/// <remarks>
/// <para>
/// El <i>proposito</i> que se pasa a <c>CreateProtector</c> aisla estos secretos: un valor
/// cifrado con este proposito no puede descifrarse con otro, aunque compartan las mismas
/// claves maestras.
/// </para>
/// <para>
/// <b>Aviso para el despliegue en Azure (Fase 10).</b> Por defecto las claves de Data
/// Protection se guardan en el sistema de ficheros local. En App Service eso significa que
/// se pierden al reiniciar y que no se comparten entre instancias: las contrasenas SMTP
/// guardadas dejarian de poder descifrarse y habria que volver a introducirlas. Allí hay que
/// persistirlas en Blob Storage y protegerlas con Key Vault.
/// </para>
/// </remarks>
/// <param name="proveedor">Proveedor de proteccion de datos.</param>
/// <param name="registro">Registro de eventos.</param>
public class ProtectorSecretos(
    IDataProtectionProvider proveedor,
    ILogger<ProtectorSecretos> registro) : IProtectorSecretos
{
    /// <summary>Proposito que aisla estos secretos de cualquier otro uso.</summary>
    private const string Proposito = "Rumbo.Correo.CredencialesSmtp.v1";

    private readonly IDataProtector _protector = proveedor.CreateProtector(Proposito);

    /// <inheritdoc />
    public string Cifrar(string valorEnClaro) => _protector.Protect(valorEnClaro);

    /// <inheritdoc />
    public string? Descifrar(string valorCifrado)
    {
        try
        {
            return _protector.Unprotect(valorCifrado);
        }
        catch (Exception excepcion)
        {
            // Ocurre si se perdieron las claves de Data Protection o si el valor se
            // manipulo. No se registra el valor, solo el hecho.
            registro.LogError(excepcion,
                "No se pudo descifrar una credencial SMTP guardada. Habrá que volver a "
                + "introducirla.");

            return null;
        }
    }
}
