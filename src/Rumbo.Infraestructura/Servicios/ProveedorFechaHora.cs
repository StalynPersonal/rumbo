using Rumbo.Aplicacion.Comun;

namespace Rumbo.Infraestructura.Servicios;

/// <summary>
/// Implementacion real del proveedor de fecha y hora: consulta el reloj del sistema.
/// </summary>
/// <remarks>
/// En las pruebas se sustituye por una version con fecha fija, que es justo la razon de que
/// esta interfaz exista.
/// </remarks>
public class ProveedorFechaHora : IProveedorFechaHora
{
    /// <inheritdoc />
    public DateTimeOffset AhoraUtc => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public DateOnly HoyEn(string zonaHorariaIana)
    {
        // TimeZoneInfo acepta identificadores IANA en Windows desde .NET 6, asi que el mismo
        // identificador (America/Santo_Domingo) funciona en el portatil y en Azure Linux.
        var zona = TimeZoneInfo.FindSystemTimeZoneById(zonaHorariaIana);
        var ahoraLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zona);

        return DateOnly.FromDateTime(ahoraLocal.DateTime);
    }
}
