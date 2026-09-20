using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Entidades.Financiero;

namespace Rumbo.Dominio.Entidades.Planificacion;

/// <summary>
/// Cada vez que se destina dinero a una meta queda registrado aqui.
/// </summary>
/// <remarks>
/// <para>
/// El aporte no crea dinero: apunta al <see cref="Movimiento"/> real que movio los fondos a la
/// cuenta de ahorro. Esta tabla existe para poder responder "de donde salieron estos
/// RD$50,000" sin recorrer todo el libro mayor.
/// </para>
/// <para>
/// <see cref="MovimientoId"/> es opcional unicamente para permitir registrar aportes
/// anteriores al uso de Rumbo, al dar de alta una meta que ya tenia dinero acumulado.
/// </para>
/// </remarks>
public class AporteMeta : EntidadDeEspacio
{
    /// <summary>Meta a la que se aporta.</summary>
    public Guid MetaId { get; set; }

    /// <summary>Meta a la que se aporta.</summary>
    public Meta? Meta { get; set; }

    /// <summary>Movimiento que ejecuto el aporte.</summary>
    public Guid? MovimientoId { get; set; }

    /// <summary>Movimiento que ejecuto el aporte.</summary>
    public Movimiento? Movimiento { get; set; }

    /// <summary>Importe aportado.</summary>
    public decimal Monto { get; set; }

    /// <summary>Codigo ISO-4217 de la moneda.</summary>
    public required string Moneda { get; set; }

    /// <summary>Fecha del aporte.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Persona que hizo el aporte.</summary>
    public Guid? AportadoPorUsuarioId { get; set; }

    /// <summary>Notas libres.</summary>
    public string? Notas { get; set; }

    /// <summary>
    /// Indica si el aporte nacio de aceptar una recomendacion del sistema.
    /// </summary>
    /// <remarks>
    /// Permite medir si las recomendaciones sirven de algo, y deja constancia de que hubo una
    /// confirmacion explicita del usuario: el sistema jamas mueve dinero por su cuenta.
    /// </remarks>
    public bool OrigenRecomendacion { get; set; }
}
