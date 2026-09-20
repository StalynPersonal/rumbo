using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Entidades.Planificacion;

namespace Rumbo.Dominio.Entidades.Financiero;

/// <summary>
/// Asiento del libro mayor: cada vez que el dinero entra, sale o cambia de sitio, se crea uno.
/// </summary>
/// <remarks>
/// <para>
/// Es la entidad central de Rumbo. Presupuestos, metas, viajes y deudas no guardan dinero por
/// su cuenta: son vistas y etiquetas sobre estos movimientos. Esa decision evita el problema
/// clasico de tener dos verdades sobre el mismo dinero.
/// </para>
/// <para>
/// <b>El monto es SIEMPRE positivo.</b> La direccion la marca <see cref="Signo"/>, que se
/// deriva del <see cref="Tipo"/>. Guardar importes negativos parece comodo hasta que alguien
/// registra un gasto negativo por error y el saldo sube.
/// </para>
/// <para>
/// <b>Una transferencia son DOS movimientos.</b> Al mover dinero entre cuentas se crean dos
/// filas de tipo <see cref="TipoMovimiento.Transferencia"/>, la salida y la entrada, unidas por
/// el mismo <see cref="TransferenciaId"/>. Ninguna de las dos cuenta como ingreso ni como
/// gasto en los informes.
/// </para>
/// </remarks>
public class Movimiento : EntidadDeEspacio
{
    /// <summary>Naturaleza del asiento.</summary>
    public TipoMovimiento Tipo { get; set; }

    /// <summary>Cuenta sobre la que se registra.</summary>
    public Guid CuentaId { get; set; }

    /// <summary>Cuenta sobre la que se registra.</summary>
    public Cuenta? Cuenta { get; set; }

    /// <summary>
    /// Categoria del movimiento. Es opcional porque las transferencias y los ajustes no se
    /// categorizan: no representan consumo ni ingreso.
    /// </summary>
    public Guid? CategoriaId { get; set; }

    /// <summary>Categoria del movimiento.</summary>
    public Categoria? Categoria { get; set; }

    /// <summary>Importe, siempre positivo, en la moneda del movimiento.</summary>
    public decimal Monto { get; set; }

    /// <summary>
    /// Direccion del movimiento sobre el saldo: <c>+1</c> suma, <c>-1</c> resta.
    /// </summary>
    /// <remarks>
    /// Se persiste, en lugar de calcularlo al vuelo, para que el saldo de una cuenta sea una
    /// suma directa en SQL (<c>SUM(Monto * Signo)</c>) sin condicionales por tipo. Lo asigna el
    /// servicio de movimientos a partir del tipo; nunca lo fija el cliente.
    /// </remarks>
    public int Signo { get; set; }

    /// <summary>Codigo ISO-4217 de la moneda en que ocurrio el movimiento.</summary>
    public required string Moneda { get; set; }

    /// <summary>Importe equivalente en la moneda base del espacio.</summary>
    /// <remarks>
    /// Se calcula y se CONGELA en el momento de registrar, usando la tasa vigente en
    /// <see cref="FechaMovimiento"/>. Los informes agregan por este campo, de modo que un mes
    /// ya cerrado no cambia de resultado si manana se mueve el tipo de cambio.
    /// </remarks>
    public decimal MontoEnMonedaBase { get; set; }

    /// <summary>Tasa utilizada para obtener <see cref="MontoEnMonedaBase"/>.</summary>
    /// <remarks>
    /// Se guarda para poder explicar el numero: sin ella, un importe convertido es un dato sin
    /// justificacion.
    /// </remarks>
    public decimal TasaCambioAplicada { get; set; } = 1m;

    /// <summary>
    /// Indica que no habia tasa exacta para esa fecha y se uso la anterior mas cercana.
    /// </summary>
    public bool TasaEsAproximada { get; set; }

    /// <summary>Fecha contable del movimiento.</summary>
    /// <remarks>
    /// Es <c>DateOnly</c> y no una fecha con hora: un gasto del dia 30 a las 11 de la noche
    /// pertenece al dia 30, no al dia 1 del mes siguiente por efecto de la zona horaria.
    /// </remarks>
    public DateOnly FechaMovimiento { get; set; }

    /// <summary>Descripcion corta, por ejemplo "Supermercado Nacional".</summary>
    public required string Descripcion { get; set; }

    /// <summary>Notas libres y opcionales.</summary>
    public string? Notas { get; set; }

    /// <summary>Medio de pago utilizado.</summary>
    public MetodoPago? MetodoPago { get; set; }

    /// <summary>Si el movimiento es propio de una persona o del hogar.</summary>
    public TipoReparto Reparto { get; set; } = TipoReparto.Personal;

    /// <summary>Persona que puso el dinero.</summary>
    /// <remarks>
    /// Junto con <see cref="Reparto"/> permite saber quien ha aportado mas al hogar: una cena
    /// compartida que pago Juan es gasto del hogar, pero el dinero salio de Juan.
    /// </remarks>
    public Guid? PagadoPorUsuarioId { get; set; }

    /// <summary>Meta de ahorro a la que se destina, si aplica.</summary>
    public Guid? MetaId { get; set; }

    /// <summary>Meta de ahorro a la que se destina.</summary>
    public Meta? Meta { get; set; }

    /// <summary>Viaje al que pertenece el gasto, si aplica.</summary>
    public Guid? ViajeId { get; set; }

    /// <summary>Viaje al que pertenece el gasto.</summary>
    public Viaje? Viaje { get; set; }

    /// <summary>Transferencia de la que este movimiento es una de las dos patas.</summary>
    public Guid? TransferenciaId { get; set; }

    /// <summary>Transferencia de la que forma parte.</summary>
    public Transferencia? Transferencia { get; set; }

    /// <summary>Gasto recurrente que origino este movimiento, si aplica.</summary>
    public Guid? GastoRecurrenteId { get; set; }

    /// <summary>Deuda a la que corresponde el pago, si aplica.</summary>
    public Guid? DeudaId { get; set; }

    /// <summary>Indica si el movimiento afecta a los totales de ingresos o gastos.</summary>
    /// <returns>
    /// <c>false</c> para transferencias y ajustes, <c>true</c> para ingresos y gastos.
    /// </returns>
    /// <remarks>
    /// Existe para que la regla quede escrita una sola vez y en el dominio, en lugar de
    /// repetirse en cada informe con el riesgo de que alguno la olvide.
    /// </remarks>
    public bool CuentaParaIngresosYGastos() =>
        Tipo is TipoMovimiento.Ingreso or TipoMovimiento.Gasto;
}
