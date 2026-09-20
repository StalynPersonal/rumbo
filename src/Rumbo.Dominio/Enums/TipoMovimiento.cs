namespace Rumbo.Dominio.Enums;

/// <summary>
/// Naturaleza de un asiento del libro mayor.
/// </summary>
/// <remarks>
/// REGLA CRITICA: una Transferencia NO es un gasto. Se registra como DOS movimientos de este
/// tipo (la salida y la entrada) unidos por un mismo <c>TransferenciaId</c>, y todos los
/// informes de ingresos y gastos la excluyen. Contarla como gasto duplicaria el dinero que
/// parece salir del hogar y arruinaria los reportes.
/// </remarks>
public enum TipoMovimiento
{
    /// <summary>Entra dinero al espacio desde fuera. Suma al saldo.</summary>
    Ingreso = 1,

    /// <summary>Sale dinero del espacio hacia fuera. Resta del saldo.</summary>
    Gasto = 2,

    /// <summary>El dinero cambia de cuenta sin entrar ni salir del espacio.</summary>
    Transferencia = 3,

    /// <summary>Correccion manual del saldo para cuadrarlo con la realidad.</summary>
    Ajuste = 4,
}
