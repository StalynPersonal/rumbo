namespace Rumbo.Dominio.Enums;

/// <summary>
/// Naturaleza de una deuda.
/// </summary>
public enum TipoDeuda
{
    /// <summary>Saldo pendiente de una tarjeta.</summary>
    TarjetaCredito = 1,

    /// <summary>Prestamo bancario generico.</summary>
    Prestamo = 2,

    /// <summary>Dinero prestado por una persona.</summary>
    PrestamoPersonal = 3,

    /// <summary>Financiamiento de un vehiculo.</summary>
    Vehiculo = 4,

    /// <summary>Prestamo sobre una vivienda.</summary>
    Hipoteca = 5,

    /// <summary>Cualquier otra obligacion.</summary>
    Otra = 6,
}
