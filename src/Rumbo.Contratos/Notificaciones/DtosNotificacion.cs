namespace Rumbo.Contratos.Notificaciones;

/// <summary>Aviso generado por el sistema.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Tipo">ProximoPago, PresupuestoExcedido, MetaAlcanzada...</param>
/// <param name="Titulo">Titulo corto.</param>
/// <param name="Cuerpo">Texto del aviso.</param>
/// <param name="Datos">Datos asociados en JSON, para que la app pueda navegar al origen.</param>
/// <param name="ProgramadaPara">Cuando debia mostrarse.</param>
/// <param name="FechaLectura">Cuando se leyo. Nulo si sigue sin leer.</param>
/// <param name="Estado">Pendiente, Enviada, Leida o Descartada.</param>
public record NotificacionDto(
    Guid Id,
    string Tipo,
    string Titulo,
    string Cuerpo,
    string? Datos,
    DateTimeOffset ProgramadaPara,
    DateTimeOffset? FechaLectura,
    string Estado);

/// <summary>Resultado de generar avisos.</summary>
/// <param name="Generadas">Cuantos avisos nuevos se crearon.</param>
/// <param name="SinLeer">Cuantos quedan sin leer en total.</param>
public record ResultadoGeneracionAvisos(int Generadas, int SinLeer);
