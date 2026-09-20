namespace Rumbo.Contratos.Espacios;

/// <summary>Datos completos del espacio activo.</summary>
/// <param name="Id">Identificador del espacio.</param>
/// <param name="Nombre">Nombre del hogar, pareja, familia o negocio.</param>
/// <param name="Tipo">Personal, Pareja, Familia o Negocio.</param>
/// <param name="MonedaBase">Moneda en la que se consolidan los informes.</param>
/// <param name="ZonaHoraria">Zona horaria IANA que determina la fecha contable.</param>
/// <param name="Estado">Activo o Suspendido.</param>
/// <param name="FechaCreacion">Cuando se creo el espacio.</param>
/// <param name="CantidadMiembros">Cuantas personas pertenecen a el.</param>
public record EspacioDetalle(
    Guid Id,
    string Nombre,
    string Tipo,
    string MonedaBase,
    string ZonaHoraria,
    string Estado,
    DateTimeOffset FechaCreacion,
    int CantidadMiembros);

/// <summary>Preferencias configurables del espacio.</summary>
/// <param name="DiaInicioMes">
/// Dia en que empieza el periodo contable. Quien cobra el 25 puede preferir que su mes
/// financiero empiece ese dia.
/// </param>
/// <param name="UmbralAvisoPresupuesto">Porcentaje de consumo a partir del cual se avisa.</param>
/// <param name="UmbralCriticoPresupuesto">Porcentaje a partir del cual el aviso es critico.</param>
/// <param name="UmbralExcedidoPresupuesto">Porcentaje a partir del cual se considera excedido.</param>
/// <param name="MesesHistorialParaAnalisis">
/// Meses de historial que usa el motor de recomendaciones para estimar ingresos y gastos.
/// </param>
/// <param name="RecomendacionesActivas">Si el motor de recomendaciones esta activo.</param>
/// <param name="DiasAvisoPagoRecurrente">Dias de antelacion con que se avisa de un pago.</param>
public record ConfiguracionEspacioDto(
    int DiaInicioMes,
    decimal UmbralAvisoPresupuesto,
    decimal UmbralCriticoPresupuesto,
    decimal UmbralExcedidoPresupuesto,
    int MesesHistorialParaAnalisis,
    bool RecomendacionesActivas,
    int DiasAvisoPagoRecurrente);

/// <summary>Datos para cambiar las preferencias del espacio.</summary>
/// <param name="DiaInicioMes">Dia de inicio del mes contable, entre 1 y 28.</param>
/// <param name="UmbralAvisoPresupuesto">Porcentaje de aviso.</param>
/// <param name="UmbralCriticoPresupuesto">Porcentaje critico.</param>
/// <param name="UmbralExcedidoPresupuesto">Porcentaje de exceso.</param>
/// <param name="MesesHistorialParaAnalisis">Meses de historial para el analisis.</param>
/// <param name="RecomendacionesActivas">Si se generan recomendaciones.</param>
/// <param name="DiasAvisoPagoRecurrente">Dias de antelacion de los avisos de pago.</param>
public record SolicitudActualizarConfiguracion(
    int DiaInicioMes,
    decimal UmbralAvisoPresupuesto,
    decimal UmbralCriticoPresupuesto,
    decimal UmbralExcedidoPresupuesto,
    int MesesHistorialParaAnalisis,
    bool RecomendacionesActivas,
    int DiasAvisoPagoRecurrente);

/// <summary>Datos para cambiar el estado de una membresia.</summary>
/// <param name="Estado">Activa, Suspendida o Revocada.</param>
public record SolicitudCambiarEstadoMiembro(string Estado);
