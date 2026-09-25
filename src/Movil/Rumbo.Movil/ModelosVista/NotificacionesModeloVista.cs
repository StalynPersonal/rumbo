using System.Collections.ObjectModel;

using Rumbo.Movil.Comun;
using Rumbo.Movil.Servicios;

namespace Rumbo.Movil.ModelosVista;

/// <summary>Un aviso tal como se ve en la lista.</summary>
/// <param name="Titulo">Titulo corto.</param>
/// <param name="Cuerpo">Texto del aviso.</param>
/// <param name="Fecha">Cuando se genero.</param>
/// <param name="SinLeer">Si todavia no se ha leido.</param>
/// <param name="Peso">
/// Grosor de la letra del titulo: en negrita si esta sin leer.
/// </param>
/// <remarks>
/// El grosor se decide AQUI y no en el XAML con un convertidor. En XAML no se puede escribir
/// una condicion, y anadir un convertidor por cada decision visual acaba llenando el
/// proyecto de clases de una linea que hay que ir a buscar para entender la pantalla.
/// Devolver el valor ya resuelto es mas facil de seguir.
/// </remarks>
public record AvisoEnLista(
    string Titulo,
    string Cuerpo,
    string Fecha,
    bool SinLeer,
    string Peso);

/// <summary>
/// La pantalla de avisos.
/// </summary>
/// <param name="notificaciones">Servicio de avisos.</param>
public class NotificacionesModeloVista(ServicioApiNotificaciones notificaciones)
    : ModeloVistaBase
{
    private string _resumen = string.Empty;

    /// <summary>Los avisos.</summary>
    public ObservableCollection<AvisoEnLista> Avisos { get; } = [];

    /// <summary>Cuantos hay sin leer, en texto.</summary>
    public string Resumen
    {
        get => _resumen;
        private set => Establecer(ref _resumen, value);
    }

    /// <summary>Recarga los avisos, generando primero los que procedan.</summary>
    public ComandoSimple CargarComando => _cargarComando ??= new ComandoSimple(CargarAsync);

    /// <summary>Marca todos como leidos.</summary>
    public ComandoSimple MarcarLeidosComando => _marcarComando ??=
        new ComandoSimple(MarcarLeidosAsync);

    private ComandoSimple? _cargarComando;
    private ComandoSimple? _marcarComando;

    /// <summary>Genera los avisos que procedan y los trae.</summary>
    /// <returns>Tarea que finaliza cuando termina la carga.</returns>
    /// <remarks>
    /// Se genera ANTES de listar, y aqui, porque no hay nada programado que lo haga: sin
    /// push ni tareas en segundo plano, el momento natural para revisar el hogar es cuando
    /// alguien abre esta pantalla. El servidor no duplica los que ya existen.
    /// </remarks>
    public async Task CargarAsync() =>
        await EjecutarAsync(async () =>
        {
            try
            {
                await notificaciones.GenerarAsync();
            }
            catch
            {
                // Si la generacion falla se muestran los que ya hubiera. Quedarse sin ver
                // los avisos existentes porque no se pudo buscar nuevos seria peor.
            }

            var lista = await notificaciones.ListarAsync();

            Avisos.Clear();

            foreach (var aviso in lista)
            {
                var sinLeerEste = aviso.FechaLectura is null;

                Avisos.Add(new AvisoEnLista(
                    aviso.Titulo,
                    aviso.Cuerpo,
                    aviso.ProgramadaPara.ToLocalTime().ToString("dd/MM HH:mm", Dinero.Cultura),
                    sinLeerEste,
                    sinLeerEste ? "Bold" : "None"));
            }

            var sinLeer = Avisos.Count(a => a.SinLeer);

            Resumen = sinLeer switch
            {
                0 => "Estás al día.",
                1 => "Tienes 1 aviso sin leer.",
                _ => $"Tienes {sinLeer} avisos sin leer.",
            };
        });

    /// <summary>Marca todos como leidos y recarga.</summary>
    /// <returns>Tarea que finaliza cuando termina.</returns>
    private async Task MarcarLeidosAsync() =>
        await EjecutarAsync(async () =>
        {
            await notificaciones.MarcarTodosComoLeidosAsync();

            await CargarAsync();
        });
}
