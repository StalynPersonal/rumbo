namespace Rumbo.Aplicacion.Contratos;

/// <summary>
/// Cifra y descifra los secretos que Rumbo debe guardar en la base de datos.
/// </summary>
/// <remarks>
/// <para>
/// Se usa para las contrasenas SMTP. Es un caso distinto del de las contrasenas de acceso:
/// aquellas se <b>hashean</b> (jamas hace falta recuperarlas, solo comprobarlas), mientras
/// que estas hay que <b>recuperarlas en claro</b> para poder autenticarse contra el servidor
/// de correo. Por eso se cifran de forma reversible en lugar de hashearse.
/// </para>
/// <para>
/// Confundir ambos casos es un error clasico: hashear una credencial que se necesita
/// recuperar la inutiliza, y cifrar de forma reversible una contrasena de acceso deja una
/// puerta abierta innecesaria.
/// </para>
/// </remarks>
public interface IProtectorSecretos
{
    /// <summary>Cifra un valor para poder guardarlo.</summary>
    /// <param name="valorEnClaro">Valor original.</param>
    /// <returns>El valor cifrado.</returns>
    string Cifrar(string valorEnClaro);

    /// <summary>Descifra un valor guardado.</summary>
    /// <param name="valorCifrado">Valor tal como esta en la base de datos.</param>
    /// <returns>
    /// El valor original, o <c>null</c> si no se puede descifrar (por ejemplo, si se
    /// perdieron las claves de Data Protection). Devuelve <c>null</c> en vez de lanzar una
    /// excepcion para que un correo que no se puede enviar no tumbe la operacion completa.
    /// </returns>
    string? Descifrar(string valorCifrado);
}
