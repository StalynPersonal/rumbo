using Rumbo.Contratos.Notificaciones;

namespace Rumbo.Movil.Servicios;

/// <summary>
/// Avisos del hogar: pagos que vencen y metas alcanzadas.
/// </summary>
/// <remarks>
/// Rumbo todavia no manda notificaciones push. Los avisos se guardan en el servidor y esta
/// pantalla los muestra, que es lo que permite que el historial ya exista el dia que se
/// enchufe el transporte.
/// </remarks>
/// <param name="api">Cliente HTTP.</param>
public class ServicioApiNotificaciones(ClienteApi api)
{
    /// <summary>Lista los avisos.</summary>
    /// <param name="soloSinLeer">Si solo se quieren los pendientes.</param>
    /// <returns>Los avisos, del mas reciente al mas antiguo.</returns>
    public Task<List<NotificacionDto>> ListarAsync(bool soloSinLeer = false) =>
        api.ObtenerAsync<List<NotificacionDto>>(
            $"api/v1/notificaciones?soloSinLeer={soloSinLeer.ToString().ToLowerInvariant()}");

    /// <summary>Pide al servidor que revise el hogar y genere los avisos que procedan.</summary>
    /// <returns>Cuantos se generaron y cuantos quedan sin leer.</returns>
    /// <remarks>
    /// Se pide desde el movil porque no hay nada programado que lo haga: sin push ni tareas
    /// en segundo plano, el momento natural para revisar es cuando alguien abre la pantalla.
    /// No duplica: un aviso que ya existe no se crea otra vez.
    /// </remarks>
    public Task<ResultadoGeneracionAvisos> GenerarAsync() =>
        api.EnviarAsync<object, ResultadoGeneracionAvisos>(
            "api/v1/notificaciones/generar", new { });

    /// <summary>Marca todos los avisos como leidos.</summary>
    /// <returns>Cuantos se marcaron.</returns>
    public Task<int> MarcarTodosComoLeidosAsync() =>
        api.ActualizarAsync<object, int>("api/v1/notificaciones/leidas", new { });
}
