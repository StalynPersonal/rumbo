using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Entidades.Financiero;

namespace Rumbo.Dominio.Entidades.Planificacion;

/// <summary>
/// Objetivo de ahorro: un fondo de emergencia, la inicial de una vivienda o el dinero de un
/// viaje.
/// </summary>
/// <remarks>
/// <para>
/// Una meta NO guarda dinero. El dinero esta siempre en una cuenta; la meta es la etiqueta que
/// dice para que se esta guardando. Por eso aportar a una meta se registra como una
/// transferencia hacia una cuenta de ahorro, nunca como un gasto: ahorrar no es gastar.
/// </para>
/// <para>
/// <see cref="MontoActual"/> es, igual que el saldo de una cuenta, una instantanea que se
/// mantiene al dia con los aportes; la verdad es la suma de los <see cref="AporteMeta"/>.
/// </para>
/// </remarks>
public class Meta : EntidadDeEspacio
{
    /// <summary>Nombre de la meta, por ejemplo "Viaje a Colombia".</summary>
    public required string Nombre { get; set; }

    /// <summary>Descripcion opcional de para que es.</summary>
    public string? Descripcion { get; set; }

    /// <summary>Importe que se quiere reunir.</summary>
    public decimal MontoObjetivo { get; set; }

    /// <summary>Importe reunido hasta ahora.</summary>
    public decimal MontoActual { get; set; }

    /// <summary>Codigo ISO-4217 de la moneda de la meta.</summary>
    public required string Moneda { get; set; }

    /// <summary>Fecha en que se quiere tener el dinero completo.</summary>
    public DateOnly? FechaObjetivo { get; set; }

    /// <summary>Importancia relativa frente a otras metas.</summary>
    public PrioridadMeta Prioridad { get; set; } = PrioridadMeta.Media;

    /// <summary>
    /// Aporte mensual que el usuario se compromete a hacer, si quiere fijarlo el mismo.
    /// </summary>
    /// <remarks>
    /// Es independiente del aporte que CALCULA el sistema. Comparar ambos es justo lo que
    /// permite avisar de que una meta va atrasada.
    /// </remarks>
    public decimal? AporteMensualMinimo { get; set; }

    /// <summary>Situacion de la meta.</summary>
    public EstadoMeta Estado { get; set; } = EstadoMeta.Activa;

    /// <summary>Cuenta de ahorro donde se acumula el dinero de esta meta.</summary>
    public Guid? CuentaVinculadaId { get; set; }

    /// <summary>Cuenta de ahorro vinculada.</summary>
    public Cuenta? CuentaVinculada { get; set; }

    /// <summary>Icono que la representa en la aplicacion movil.</summary>
    public string? Icono { get; set; }

    /// <summary>Fecha en que se alcanzo, si se alcanzo.</summary>
    public DateOnly? FechaAlcanzada { get; set; }

    /// <summary>Marca de version para controlar escrituras simultaneas.</summary>
    public byte[]? Version { get; set; }

    /// <summary>Aportes realizados a esta meta.</summary>
    public ICollection<AporteMeta> Aportes { get; set; } = [];

    /// <summary>Importe que todavia falta por reunir. Nunca es negativo.</summary>
    public decimal MontoFaltante => Math.Max(0m, MontoObjetivo - MontoActual);

    /// <summary>Porcentaje completado, entre 0 y 100.</summary>
    /// <remarks>
    /// Si el objetivo fuera cero, devuelve 100 en lugar de dividir entre cero.
    /// </remarks>
    public decimal PorcentajeCompletado =>
        MontoObjetivo <= 0m ? 100m : Math.Min(100m, MontoActual / MontoObjetivo * 100m);
}
