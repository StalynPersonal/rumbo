namespace Rumbo.Dominio.Enums;

/// <summary>
/// Partidas en que se desglosa el presupuesto de un viaje.
/// </summary>
/// <remarks>
/// Es una lista fija y distinta de las categorias generales del espacio, porque un viaje se
/// presupuesta con un vocabulario propio que no tiene sentido en el gasto del dia a dia.
/// </remarks>
public enum CategoriaViaje
{
    /// <summary>Billetes de avion y tasas.</summary>
    Vuelos = 1,

    /// <summary>Hotel, apartamento o alojamiento.</summary>
    Hospedaje = 2,

    /// <summary>Comidas durante el viaje.</summary>
    Alimentacion = 3,

    /// <summary>Traslados en destino.</summary>
    Transporte = 4,

    /// <summary>Excursiones, entradas y ocio.</summary>
    Actividades = 5,

    /// <summary>Souvenirs y compras personales.</summary>
    Compras = 6,

    /// <summary>Visados, pasaporte y tramites.</summary>
    Documentos = 7,

    /// <summary>Seguro de viaje o medico.</summary>
    Seguro = 8,

    /// <summary>Cualquier otro concepto.</summary>
    Otros = 9,
}
