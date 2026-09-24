using Microsoft.Extensions.Logging;

using Rumbo.Movil.ModelosVista;
using Rumbo.Movil.Servicios;
using Rumbo.Movil.Vistas;

namespace Rumbo.Movil;

/// <summary>
/// El arranque de la aplicacion. Es el equivalente a Program.cs del backend.
/// </summary>
/// <remarks>
/// Aqui se registra todo lo que despues llega SOLO a los constructores. Cuando MAUI crea
/// PanelPagina, ve que su constructor pide un PanelModeloVista, lo crea, ve que este pide
/// un ServicioApiPanel, y asi hasta abajo. No se construye nada a mano con "new".
/// </remarks>
public static class MauiProgram
{
    /// <summary>Construye la aplicacion.</summary>
    /// <returns>La aplicacion lista para arrancar.</returns>
    public static MauiApp CreateMauiApp()
    {
        var constructor = MauiApp.CreateBuilder();

        constructor
            .UseMauiApp<App>()
            .ConfigureFonts(fuentes =>
            {
                fuentes.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fuentes.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // --- Servicios de la aplicacion ---------------------------------------
        //
        // Singleton: UNA instancia mientras la aplicacion viva. Para la sesion y los
        // servicios de API, que no guardan estado de pantalla.
        constructor.Services.AddSingleton<AlmacenSesion>();
        constructor.Services.AddSingleton<ServicioSesion>();
        constructor.Services.AddSingleton<ServicioApiPanel>();
        constructor.Services.AddSingleton<ServicioApiFinanzas>();
        constructor.Services.AddSingleton<ServicioApiPlanificacion>();
        constructor.Services.AddSingleton<ServicioApiVersion>();
        constructor.Services.AddSingleton<AppShell>();

        // --- Cliente HTTP ------------------------------------------------------
        //
        // El ManejadorDeToken se mete EN MEDIO del cliente y la red: pone el token en cada
        // peticion y lo renueva cuando caduca. Registrado aqui una vez, ningun servicio
        // tiene que acordarse de nada.
        constructor.Services.AddTransient<ManejadorDeToken>();

        constructor.Services
            .AddHttpClient<ClienteApi>(cliente =>
            {
                cliente.BaseAddress = new Uri(ConfiguracionApi.DireccionBase);

                // Un minuto de espera. Por defecto son cien segundos, que en un movil con
                // mala cobertura significa la persona mirando una ruedita sin saber si ha
                // pasado algo.
                cliente.Timeout = TimeSpan.FromSeconds(60);
            })
            .AddHttpMessageHandler<ManejadorDeToken>();

        // --- Pantallas y sus modelos de vista ---------------------------------
        //
        // Transient: una NUEVA cada vez que se pide. Asi, al volver a entrar en una pantalla
        // esta limpia y no con los datos de la vez anterior.
        constructor.Services.AddTransient<IniciarSesionPagina>();
        constructor.Services.AddTransient<IniciarSesionModeloVista>();
        constructor.Services.AddTransient<PanelPagina>();
        constructor.Services.AddTransient<PanelModeloVista>();
        constructor.Services.AddTransient<MovimientosPagina>();
        constructor.Services.AddTransient<MovimientosModeloVista>();
        constructor.Services.AddTransient<CuentasPagina>();
        constructor.Services.AddTransient<CuentasModeloVista>();
        constructor.Services.AddTransient<MasPagina>();
        constructor.Services.AddTransient<MasModeloVista>();
        constructor.Services.AddTransient<PresupuestosPagina>();
        constructor.Services.AddTransient<PresupuestosModeloVista>();
        constructor.Services.AddTransient<MetasPagina>();
        constructor.Services.AddTransient<MetasModeloVista>();
        constructor.Services.AddTransient<ViajesPagina>();
        constructor.Services.AddTransient<ViajesModeloVista>();
        constructor.Services.AddTransient<ReportesPagina>();
        constructor.Services.AddTransient<ReportesModeloVista>();
        constructor.Services.AddTransient<AjustesPagina>();
        constructor.Services.AddTransient<AjustesModeloVista>();

#if DEBUG
        constructor.Logging.AddDebug();
#endif

        return constructor.Build();
    }
}
