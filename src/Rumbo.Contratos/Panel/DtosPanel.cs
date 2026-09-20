using Rumbo.Contratos.Recomendaciones;
using Rumbo.Contratos.Reportes;

namespace Rumbo.Contratos.Panel;

/// <summary>Saldo consolidado del hogar.</summary>
/// <param name="TotalDisponible">Suma de las cuentas operativas y de ahorro.</param>
/// <param name="TotalEnAhorro">Lo que hay en cuentas de ahorro e inversion.</param>
/// <param name="TotalDeudas">Lo que se debe.</param>
/// <param name="PatrimonioNeto">Disponible menos deudas.</param>
/// <param name="CantidadCuentas">Cuentas activas.</param>
public record ResumenPatrimonio(
    decimal TotalDisponible,
    decimal TotalEnAhorro,
    decimal TotalDeudas,
    decimal PatrimonioNeto,
    int CantidadCuentas);

/// <summary>Una partida de presupuesto que necesita atencion.</summary>
/// <param name="PresupuestoId">Presupuesto al que pertenece.</param>
/// <param name="NombreCategoria">Categoria afectada.</param>
/// <param name="MontoAsignado">Lo presupuestado.</param>
/// <param name="MontoGastado">Lo gastado.</param>
/// <param name="PorcentajeConsumido">Cuanto se lleva consumido.</param>
/// <param name="Nivel">Aviso, Critico o Excedido.</param>
public record AlertaPresupuesto(
    Guid PresupuestoId,
    string NombreCategoria,
    decimal MontoAsignado,
    decimal MontoGastado,
    decimal PorcentajeConsumido,
    string Nivel);

/// <summary>Un compromiso que vence pronto.</summary>
/// <param name="Tipo">GastoRecurrente o Deuda.</param>
/// <param name="Id">Identificador del compromiso.</param>
/// <param name="Nombre">Nombre legible.</param>
/// <param name="Monto">Importe previsto.</param>
/// <param name="Fecha">Cuando vence.</param>
/// <param name="DiasRestantes">Dias que faltan.</param>
public record CompromisoProximo(
    string Tipo,
    Guid Id,
    string Nombre,
    decimal Monto,
    DateOnly Fecha,
    int DiasRestantes);

/// <summary>Avance resumido de una meta.</summary>
/// <param name="Id">Meta.</param>
/// <param name="Nombre">Nombre de la meta.</param>
/// <param name="MontoObjetivo">Lo que se quiere reunir.</param>
/// <param name="MontoActual">Lo reunido.</param>
/// <param name="PorcentajeCompletado">Progreso.</param>
/// <param name="FechaObjetivo">Fecha limite.</param>
/// <param name="VaAtrasada">Si al ritmo prometido no llegaria.</param>
public record AvanceMeta(
    Guid Id,
    string Nombre,
    decimal MontoObjetivo,
    decimal MontoActual,
    decimal PorcentajeCompletado,
    DateOnly? FechaObjetivo,
    bool VaAtrasada);

/// <summary>
/// Todo lo que la pantalla de inicio necesita, en una sola peticion.
/// </summary>
/// <param name="Hoy">Fecha actual en la zona horaria del espacio.</param>
/// <param name="NombreEspacio">Nombre del hogar.</param>
/// <param name="Moneda">Moneda base en la que se consolida todo.</param>
/// <param name="Patrimonio">Saldo consolidado y deudas.</param>
/// <param name="MesEnCurso">Ingresos y gastos del mes que corre.</param>
/// <param name="MesAnterior">Lo mismo del mes pasado, para comparar.</param>
/// <param name="MayoresGastos">Las categorias donde mas se gasta este mes.</param>
/// <param name="AlertasPresupuesto">Partidas que se acercan o superan su limite.</param>
/// <param name="ProximosCompromisos">Recibos y cuotas que vencen en los proximos dias.</param>
/// <param name="Metas">Avance de las metas activas.</param>
/// <param name="Recomendaciones">Sugerencias pendientes del motor.</param>
/// <param name="NotificacionesSinLeer">Cuantos avisos hay sin leer.</param>
/// <remarks>
/// Se devuelve todo junto a proposito: la aplicacion movil se abre con una sola llamada en
/// lugar de ocho. En una conexion movil lenta, ocho peticiones son ocho oportunidades de que
/// la pantalla se quede a medias.
/// </remarks>
public record PanelInicio(
    DateOnly Hoy,
    string NombreEspacio,
    string Moneda,
    ResumenPatrimonio Patrimonio,
    ResumenPeriodo MesEnCurso,
    ResumenPeriodo MesAnterior,
    IReadOnlyList<TotalPorCategoria> MayoresGastos,
    IReadOnlyList<AlertaPresupuesto> AlertasPresupuesto,
    IReadOnlyList<CompromisoProximo> ProximosCompromisos,
    IReadOnlyList<AvanceMeta> Metas,
    IReadOnlyList<RecomendacionDto> Recomendaciones,
    int NotificacionesSinLeer);
