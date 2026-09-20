using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Enums;

namespace Rumbo.Dominio.Entidades.Financiero;

/// <summary>
/// Entrada de dinero que se repite: un salario, un alquiler cobrado o una pension.
/// </summary>
/// <remarks>
/// Es la contraparte de <see cref="GastoRecurrente"/> y cumple una funcion importante para el
/// motor de recomendaciones: permite proyectar el ingreso esperado de los proximos meses
/// incluso cuando todavia hay poco historial registrado.
/// </remarks>
public class IngresoRecurrente : EntidadDeEspacio
{
    /// <summary>Nombre del ingreso, por ejemplo "Salario de Maria".</summary>
    public required string Nombre { get; set; }

    /// <summary>Categoria con la que se registrara.</summary>
    public Guid CategoriaId { get; set; }

    /// <summary>Categoria con la que se registrara.</summary>
    public Categoria? Categoria { get; set; }

    /// <summary>Cuenta en la que suele ingresarse.</summary>
    public Guid CuentaId { get; set; }

    /// <summary>Cuenta en la que suele ingresarse.</summary>
    public Cuenta? Cuenta { get; set; }

    /// <summary>Importe previsto.</summary>
    public decimal MontoEstimado { get; set; }

    /// <summary>Codigo ISO-4217 de la moneda.</summary>
    public required string Moneda { get; set; }

    /// <summary>Indica si el importe es fijo o variable, como una comision.</summary>
    public bool EsMontoFijo { get; set; } = true;

    /// <summary>Cada cuanto se repite.</summary>
    public Frecuencia Frecuencia { get; set; } = Frecuencia.Mensual;

    /// <summary>Fecha del proximo cobro previsto.</summary>
    public DateOnly ProximaFechaCobro { get; set; }

    /// <summary>Persona que percibe el ingreso.</summary>
    public Guid? RecibidoPorUsuarioId { get; set; }

    /// <summary>Situacion de la recurrencia.</summary>
    public EstadoRecurrencia Estado { get; set; } = EstadoRecurrencia.Activa;

    /// <summary>Notas libres.</summary>
    public string? Notas { get; set; }
}
