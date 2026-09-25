using Rumbo.Movil.Comun;
using Rumbo.Movil.Vistas;

namespace Rumbo.Movil.ModelosVista;

/// <summary>
/// El menu de las pantallas que no son del dia a dia.
/// </summary>
/// <remarks>
/// Estas pantallas se abren con navegacion normal, no como pestana: se apilan encima y el
/// boton Atras del telefono devuelve aqui. Es lo que la gente espera al entrar en una
/// seccion secundaria.
/// </remarks>
public class MasModeloVista : ModeloVistaBase
{
    /// <summary>Abre el presupuesto.</summary>
    public ComandoSimple IrAPresupuestosComando { get; } =
        new(() => Shell.Current.GoToAsync(Rutas.Presupuestos));

    /// <summary>Abre las metas de ahorro.</summary>
    public ComandoSimple IrAMetasComando { get; } =
        new(() => Shell.Current.GoToAsync(Rutas.Metas));

    /// <summary>Abre los viajes.</summary>
    public ComandoSimple IrAViajesComando { get; } =
        new(() => Shell.Current.GoToAsync(Rutas.Viajes));

    /// <summary>Abre las deudas.</summary>
    public ComandoSimple IrADeudasComando { get; } =
        new(() => Shell.Current.GoToAsync(Rutas.Deudas));

    /// <summary>Abre los avisos.</summary>
    public ComandoSimple IrAAvisosComando { get; } =
        new(() => Shell.Current.GoToAsync(Rutas.Notificaciones));

    /// <summary>Abre los informes.</summary>
    public ComandoSimple IrAReportesComando { get; } =
        new(() => Shell.Current.GoToAsync(Rutas.Reportes));

    /// <summary>Abre los ajustes.</summary>
    public ComandoSimple IrAAjustesComando { get; } =
        new(() => Shell.Current.GoToAsync(Rutas.Ajustes));
}
