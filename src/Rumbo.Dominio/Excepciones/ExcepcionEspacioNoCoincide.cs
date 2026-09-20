namespace Rumbo.Dominio.Excepciones;

/// <summary>
/// Se intento escribir un registro que pertenece a un espacio distinto del activo.
/// </summary>
/// <remarks>
/// <para>
/// Esta excepcion NO deberia ocurrir jamas en una aplicacion correcta: significa que algo
/// intento cruzar la frontera entre dos hogares. Cuando aparece, es un fallo grave, no un
/// error de usuario.
/// </para>
/// <para>
/// Por eso el mensaje que llega al cliente debe ser generico (un 404 o un 403 sin detalles) y
/// el detalle con los identificadores debe quedarse solo en los registros del servidor:
/// confirmarle a alguien que "ese registro existe pero es de otro espacio" ya es filtrar
/// informacion.
/// </para>
/// </remarks>
public class ExcepcionEspacioNoCoincide : ExcepcionDominio
{
    private ExcepcionEspacioNoCoincide(string mensaje) : base(mensaje)
    {
    }

    /// <summary>Espacio al que pertenecia el registro.</summary>
    public Guid EspacioDelRegistro { get; private init; }

    /// <summary>Espacio activo en la peticion.</summary>
    public Guid EspacioActivo { get; private init; }

    /// <summary>Crea la excepcion para un intento de ALTA en otro espacio.</summary>
    /// <param name="entidad">Nombre de la entidad afectada.</param>
    /// <param name="espacioDelRegistro">Espacio que traia el registro.</param>
    /// <param name="espacioActivo">Espacio activo de la peticion.</param>
    /// <returns>La excepcion construida.</returns>
    public static ExcepcionEspacioNoCoincide AlCrear(
        string entidad, Guid espacioDelRegistro, Guid espacioActivo) =>
        new($"Se intento crear una entidad {entidad} en el espacio {espacioDelRegistro}, "
            + $"pero el espacio activo es {espacioActivo}.")
        {
            EspacioDelRegistro = espacioDelRegistro,
            EspacioActivo = espacioActivo,
        };

    /// <summary>Crea la excepcion para un intento de MODIFICACION en otro espacio.</summary>
    /// <param name="entidad">Nombre de la entidad afectada.</param>
    /// <param name="espacioDelRegistro">Espacio al que pertenece el registro.</param>
    /// <param name="espacioActivo">Espacio activo de la peticion.</param>
    /// <returns>La excepcion construida.</returns>
    public static ExcepcionEspacioNoCoincide AlModificar(
        string entidad, Guid espacioDelRegistro, Guid espacioActivo) =>
        new($"Se intento modificar una entidad {entidad} del espacio {espacioDelRegistro} "
            + $"desde el espacio {espacioActivo}.")
        {
            EspacioDelRegistro = espacioDelRegistro,
            EspacioActivo = espacioActivo,
        };
}
