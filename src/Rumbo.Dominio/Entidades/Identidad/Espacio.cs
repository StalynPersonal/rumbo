using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Enums;

namespace Rumbo.Dominio.Entidades.Identidad;

/// <summary>
/// Unidad de aislamiento del sistema: el hogar, la pareja, la familia o el negocio cuyas
/// finanzas se administran en conjunto.
/// </summary>
/// <remarks>
/// <para>
/// Es lo que en la jerga tecnica se llama <i>tenant</i>. Todo dato financiero pertenece a
/// exactamente un espacio, y ningun usuario puede ver datos de un espacio al que no pertenece.
/// </para>
/// <para>
/// El propio <c>Espacio</c> NO implementa <see cref="IEntidadDeEspacio"/>: es la tabla raiz que
/// define los espacios, asi que no puede filtrarse por si misma.
/// </para>
/// </remarks>
public class Espacio : EntidadAuditable
{
    /// <summary>Nombre visible, por ejemplo "Casa de Juan y Maria".</summary>
    public required string Nombre { get; set; }

    /// <summary>Naturaleza del grupo que comparte estas finanzas.</summary>
    public TipoEspacio Tipo { get; set; } = TipoEspacio.Personal;

    /// <summary>
    /// Moneda en la que se consolidan todos los informes, en formato ISO-4217 (por ejemplo DOP).
    /// </summary>
    /// <remarks>
    /// Las cuentas pueden estar en otras monedas; cada movimiento guarda ademas su importe
    /// convertido a esta moneda con la tasa vigente en su fecha.
    /// </remarks>
    public required string MonedaBase { get; set; }

    /// <summary>
    /// Zona horaria IANA del espacio, por ejemplo <c>America/Santo_Domingo</c>.
    /// </summary>
    /// <remarks>
    /// Determina a que dia contable pertenece un movimiento registrado de madrugada.
    /// </remarks>
    public required string ZonaHoraria { get; set; }

    /// <summary>Situacion operativa del espacio.</summary>
    public EstadoEspacio Estado { get; set; } = EstadoEspacio.Activo;

    /// <summary>Personas que pertenecen a este espacio.</summary>
    public ICollection<MembresiaEspacio> Membresias { get; set; } = [];

    /// <summary>Preferencias configurables del espacio.</summary>
    public ConfiguracionEspacio? Configuracion { get; set; }
}
