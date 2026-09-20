namespace Rumbo.Aplicacion.Comun;

/// <summary>
/// Proporciona la fecha y la hora actuales.
/// </summary>
/// <remarks>
/// <para>
/// Existe para que ningun servicio llame directamente a <c>DateTimeOffset.UtcNow</c>. Un
/// calculo financiero que depende del reloj del sistema es imposible de probar: no se puede
/// escribir un test que verifique "faltan 15 meses para la meta" si el resultado cambia cada
/// dia que pasa.
/// </para>
/// <para>
/// En las pruebas se sustituye por una implementacion con una fecha fija.
/// </para>
/// </remarks>
public interface IProveedorFechaHora
{
    /// <summary>Instante actual en UTC.</summary>
    DateTimeOffset AhoraUtc { get; }

    /// <summary>
    /// Fecha contable de hoy en la zona horaria indicada.
    /// </summary>
    /// <param name="zonaHorariaIana">
    /// Zona horaria IANA del espacio, por ejemplo <c>America/Santo_Domingo</c>.
    /// </param>
    /// <returns>La fecha del dia en esa zona horaria.</returns>
    /// <remarks>
    /// Un movimiento registrado a las 11 de la noche en Santo Domingo pertenece a ese dia, no
    /// al siguiente, que es lo que diria la fecha en UTC.
    /// </remarks>
    DateOnly HoyEn(string zonaHorariaIana);
}
