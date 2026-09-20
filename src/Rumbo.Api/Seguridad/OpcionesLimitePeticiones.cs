namespace Rumbo.Api.Seguridad;

/// <summary>
/// Cupos del limitador de peticiones.
/// </summary>
/// <remarks>
/// Son configurables y no constantes por dos razones. Una: el cupo bueno depende de cuanta
/// gente use la instalacion, y ajustarlo no deberia exigir recompilar. Dos: las pruebas de
/// integracion necesitan poder subirlos para no estorbarse entre ellas, y bajarlos en una
/// prueba concreta para comprobar que el limite <b>de verdad</b> corta.
/// </remarks>
public class OpcionesLimitePeticiones
{
    /// <summary>Nombre de la seccion en la configuracion.</summary>
    public const string Seccion = "LimitePeticiones";

    /// <summary>
    /// Peticiones por minuto permitidas en los endpoints de autenticacion.
    /// </summary>
    /// <remarks>
    /// Cinco: de sobra para quien se equivoca al teclear, insuficiente para probar
    /// contrasenas en serie.
    /// </remarks>
    public int PorMinutoAutenticacion { get; set; } = 5;

    /// <summary>Peticiones por minuto permitidas en el resto de la API.</summary>
    public int PorMinutoGeneral { get; set; } = 100;
}
