using System.Collections.ObjectModel;

using Rumbo.Contratos.Aplicacion;
using Rumbo.Contratos.Autenticacion;
using Rumbo.Movil.Comun;
using Rumbo.Movil.Servicios;
using Rumbo.Movil.Vistas;

namespace Rumbo.Movil.ModelosVista;

/// <summary>
/// La pantalla de ajustes: quién eres, en qué hogar estás y cómo salir.
/// </summary>
/// <param name="sesion">Sesion activa.</param>
/// <param name="version">Servicio que consulta la version publicada.</param>
public class AjustesModeloVista(ServicioSesion sesion, ServicioApiVersion version)
    : ModeloVistaBase
{
    private EspacioResumen? _espacioSeleccionado;
    private string _avisoVersion = string.Empty;

    /// <summary>Nombre de la persona que ha entrado.</summary>
    public string NombreUsuario => sesion.Usuario?.NombreCompleto ?? "—";

    /// <summary>Correo de acceso.</summary>
    public string CorreoUsuario => sesion.Usuario?.Correo ?? "—";

    /// <summary>Version instalada de la aplicacion.</summary>
    public string VersionInstalada => ConfiguracionApi.VersionInstalada;

    /// <summary>Espacios a los que puede cambiar.</summary>
    public ObservableCollection<EspacioResumen> Espacios { get; } = [];

    /// <summary>Espacio sobre el que se opera.</summary>
    public EspacioResumen? EspacioSeleccionado
    {
        get => _espacioSeleccionado;
        set
        {
            var anterior = _espacioSeleccionado;

            if (Establecer(ref _espacioSeleccionado, value)
                && value is not null
                && anterior is not null
                && anterior.Id != value.Id)
            {
                // Cambiar de espacio pide un token NUEVO al servidor, porque el espacio
                // activo viaja dentro del token firmado. No es un ajuste del movil: si lo
                // fuera, bastaria con cambiarlo aqui para leer las finanzas de otro hogar.
                _ = CambiarEspacioAsync(value.Id);
            }
        }
    }

    /// <summary>Aviso de actualizacion, si lo hay.</summary>
    public string AvisoVersion
    {
        get => _avisoVersion;
        private set
        {
            Establecer(ref _avisoVersion, value);
            Avisar(nameof(HayAvisoVersion));
        }
    }

    /// <summary>Indica si hay un aviso de version que mostrar.</summary>
    public bool HayAvisoVersion => !string.IsNullOrWhiteSpace(AvisoVersion);

    /// <summary>Carga los datos de la pantalla.</summary>
    public ComandoSimple CargarComando => _cargarComando ??= new ComandoSimple(CargarAsync);

    /// <summary>Cierra la sesion.</summary>
    public ComandoSimple CerrarSesionComando => _cerrarComando ??=
        new ComandoSimple(CerrarSesionAsync);

    private ComandoSimple? _cargarComando;
    private ComandoSimple? _cerrarComando;

    /// <summary>Rellena los espacios y comprueba la version.</summary>
    /// <returns>Tarea que finaliza cuando termina la carga.</returns>
    public async Task CargarAsync()
    {
        Espacios.Clear();

        foreach (var espacio in sesion.EspaciosDisponibles)
        {
            Espacios.Add(espacio);
        }

        _espacioSeleccionado = sesion.EspacioActivo;
        Avisar(nameof(EspacioSeleccionado));
        Avisar(nameof(NombreUsuario));
        Avisar(nameof(CorreoUsuario));

        await ComprobarVersionAsync();
    }

    /// <summary>Pregunta al servidor si hay una version mas reciente.</summary>
    /// <returns>Tarea que finaliza cuando termina la consulta.</returns>
    /// <remarks>
    /// El APK se instala a mano, sin tienda, asi que nadie avisa de una version nueva si no
    /// avisa la propia aplicacion. Si la consulta falla no se dice nada: no poder comprobar
    /// la version no es un problema que la persona pueda resolver.
    /// </remarks>
    private async Task ComprobarVersionAsync()
    {
        try
        {
            var resultado = await version.ConsultarAsync(ConfiguracionApi.VersionInstalada);

            AvisoVersion = resultado.Mensaje ?? string.Empty;
        }
        catch
        {
            AvisoVersion = string.Empty;
        }
    }

    /// <summary>Cambia el espacio activo.</summary>
    /// <param name="espacioId">Espacio al que se cambia.</param>
    /// <returns>Tarea que finaliza cuando termina el cambio.</returns>
    private async Task CambiarEspacioAsync(Guid espacioId) =>
        await EjecutarAsync(async () =>
        {
            await sesion.CambiarEspacioAsync(espacioId);

            // Se vuelve al inicio: los datos de la pantalla anterior eran del otro hogar y
            // dejarlos en pantalla, aunque sea un instante, seria enseñar cifras que ya no
            // corresponden.
            await Shell.Current.GoToAsync(Rutas.Panel);
        });

    /// <summary>Cierra la sesion y vuelve a la pantalla de acceso.</summary>
    /// <returns>Tarea que finaliza cuando termina.</returns>
    private async Task CerrarSesionAsync() =>
        await EjecutarAsync(async () =>
        {
            await sesion.CerrarSesionAsync();

            await Shell.Current.GoToAsync(Rutas.Acceso);
        });
}
