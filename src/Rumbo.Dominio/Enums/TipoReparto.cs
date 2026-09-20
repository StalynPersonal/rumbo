namespace Rumbo.Dominio.Enums;

/// <summary>
/// Indica si un movimiento afecta solo a quien lo hizo o al conjunto del hogar.
/// </summary>
/// <remarks>
/// Se combina con <c>PagadoPorUsuarioId</c>. Una cena compartida pagada por Juan es
/// Compartido con PagadoPor = Juan: eso permite calcular despues quien ha aportado mas al
/// hogar. Sin los dos datos no se puede distinguir un gasto propio de Juan de un gasto del
/// hogar que Juan adelanto.
/// </remarks>
public enum TipoReparto
{
    /// <summary>Gasto o ingreso propio de una persona.</summary>
    Personal = 1,

    /// <summary>Corresponde al hogar, aunque lo haya pagado una sola persona.</summary>
    Compartido = 2,
}
