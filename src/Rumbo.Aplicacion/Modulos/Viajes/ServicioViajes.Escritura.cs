using Microsoft.EntityFrameworkCore;

using Rumbo.Aplicacion.Modulos.Movimientos;
using Rumbo.Contratos.Viajes;
using Rumbo.Dominio.Entidades.Planificacion;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Viajes;

/// <summary>
/// Parte del servicio de viajes que crea, modifica y elimina.
/// </summary>
public partial class ServicioViajes
{
    /// <inheritdoc />
    public async Task<ViajeDetalle> CrearAsync(
        SolicitudGuardarViaje solicitud,
        CancellationToken cancelacion = default)
    {
        var viaje = new Viaje { Nombre = string.Empty, Moneda = string.Empty };

        await AplicarAsync(viaje, solicitud, cancelacion);

        contexto.Viajes.Add(viaje);
        await contexto.SaveChangesAsync(cancelacion);

        return await ObtenerAsync(viaje.Id, cancelacion);
    }

    /// <inheritdoc />
    public async Task<ViajeDetalle> ActualizarAsync(
        Guid viajeId,
        SolicitudGuardarViaje solicitud,
        CancellationToken cancelacion = default)
    {
        var viaje = await contexto.Viajes
            .Include(v => v.Lineas)
            .FirstOrDefaultAsync(v => v.Id == viajeId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("el viaje");

        await AplicarAsync(viaje, solicitud, cancelacion);
        await contexto.SaveChangesAsync(cancelacion);

        return await ObtenerAsync(viajeId, cancelacion);
    }

    /// <inheritdoc />
    public async Task<ViajeDetalle> CambiarEstadoAsync(
        Guid viajeId,
        SolicitudCambiarEstadoViaje solicitud,
        CancellationToken cancelacion = default)
    {
        if (!Enum.TryParse<EstadoViaje>(solicitud.Estado, ignoreCase: true, out var estado))
        {
            throw new ExcepcionDominio(
                "El estado debe ser Planificado, EnCurso, Finalizado o Cancelado.");
        }

        var viaje = await contexto.Viajes
            .FirstOrDefaultAsync(v => v.Id == viajeId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("el viaje");

        viaje.Estado = estado;
        await contexto.SaveChangesAsync(cancelacion);

        return await ObtenerAsync(viajeId, cancelacion);
    }

    /// <inheritdoc />
    public async Task EliminarAsync(Guid viajeId, CancellationToken cancelacion = default)
    {
        var viaje = await contexto.Viajes
            .Include(v => v.Lineas)
            .FirstOrDefaultAsync(v => v.Id == viajeId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("el viaje");

        var tieneGastos = await contexto.Movimientos
            .AnyAsync(m => m.ViajeId == viajeId, cancelacion);

        if (tieneGastos)
        {
            // Esos movimientos existen y apuntan al viaje. Borrarlo dejaria gastos huerfanos
            // o, peor, obligaria a borrar dinero real del libro mayor.
            throw new ExcepcionDominio(
                $"«{viaje.Nombre}» ya tiene gastos registrados y no se puede eliminar. "
                + "Cámbiale el estado a Cancelado para retirarlo de la vista.");
        }

        contexto.Viajes.Remove(viaje);
        await contexto.SaveChangesAsync(cancelacion);
    }

    /// <summary>Vuelca la solicitud sobre la entidad y reconstruye su desglose.</summary>
    /// <param name="viaje">Entidad que se rellena.</param>
    /// <param name="solicitud">Datos recibidos.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando se aplican los datos.</returns>
    private async Task AplicarAsync(
        Viaje viaje,
        SolicitudGuardarViaje solicitud,
        CancellationToken cancelacion)
    {
        var nombre = solicitud.Nombre?.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ExcepcionDominio("El viaje necesita un nombre.");
        }

        if (solicitud.FechaFin < solicitud.FechaInicio)
        {
            throw new ExcepcionDominio("El viaje no puede terminar antes de empezar.");
        }

        if (solicitud.NumeroViajeros < 1)
        {
            throw new ExcepcionDominio("Tiene que viajar al menos una persona.");
        }

        if (solicitud.Lineas is null || solicitud.Lineas.Count == 0)
        {
            throw new ExcepcionDominio(
                "Un viaje sin desglose no se puede analizar. Añade al menos una partida.");
        }

        var partidas = LeerPartidas(solicitud.Lineas);

        if (solicitud.MetaId is { } metaId)
        {
            var existe = await contexto.Metas.AnyAsync(m => m.Id == metaId, cancelacion);

            if (!existe)
            {
                throw new ExcepcionNoEncontrado("la meta de ahorro del viaje");
            }
        }

        viaje.Nombre = nombre;
        viaje.Destino = solicitud.Destino?.Trim();
        viaje.Descripcion = solicitud.Descripcion?.Trim();
        viaje.FechaInicio = solicitud.FechaInicio;
        viaje.FechaFin = solicitud.FechaFin;
        viaje.NumeroViajeros = solicitud.NumeroViajeros;
        viaje.MetaId = solicitud.MetaId;

        viaje.Moneda = string.IsNullOrWhiteSpace(solicitud.Moneda)
            ? await ObtenerMonedaBaseAsync(cancelacion)
            : solicitud.Moneda.Trim().ToUpperInvariant();

        // El presupuesto total es SIEMPRE la suma de las partidas, nunca un dato aparte.
        // Guardar los dos dejaria abierta la puerta a que no cuadraran, y entonces habria
        // que decidir cual manda: exactamente el tipo de ambiguedad que arruina un informe.
        viaje.PresupuestoTotal = partidas.Sum(p => p.Monto);

        viaje.Lineas.Clear();

        var orden = 0;

        foreach (var partida in partidas)
        {
            viaje.Lineas.Add(new LineaPresupuestoViaje
            {
                Categoria = partida.Categoria,
                MontoPlanificado = partida.Monto,
                Notas = partida.Notas,
                Orden = orden++,
            });
        }
    }

    /// <summary>Valida y traduce las partidas recibidas.</summary>
    /// <param name="lineas">Partidas propuestas.</param>
    /// <returns>Las partidas ya traducidas al enum.</returns>
    private static List<(CategoriaViaje Categoria, decimal Monto, string? Notas)> LeerPartidas(
        IReadOnlyList<LineaViajeSolicitud> lineas)
    {
        var resultado = new List<(CategoriaViaje, decimal, string?)>(lineas.Count);
        var vistas = new HashSet<CategoriaViaje>();

        foreach (var linea in lineas)
        {
            if (!Enum.TryParse<CategoriaViaje>(
                    linea.Categoria, ignoreCase: true, out var categoria))
            {
                throw new ExcepcionDominio(
                    $"«{linea.Categoria}» no es una partida de viaje válida. Usa Vuelos, "
                    + "Hospedaje, Alimentacion, Transporte, Actividades, Compras, "
                    + "Documentos, Seguro u Otros.");
            }

            if (linea.MontoPlanificado < 0m)
            {
                throw new ExcepcionDominio("Los importes previstos no pueden ser negativos.");
            }

            if (!vistas.Add(categoria))
            {
                // Con la partida repetida, el gasto real se compararia contra un importe
                // ambiguo y el desglose dejaria de sumar el total.
                throw new ExcepcionDominio(
                    $"La partida «{categoria}» aparece dos veces. Cada una va una sola vez.");
            }

            resultado.Add((categoria, linea.MontoPlanificado, linea.Notas?.Trim()));
        }

        return resultado;
    }

    /// <summary>Moneda base del espacio.</summary>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>El codigo ISO-4217.</returns>
    private async Task<string> ObtenerMonedaBaseAsync(CancellationToken cancelacion)
    {
        var espacioId = contextoEspacio.ObtenerEspacioObligatorio();

        return await contexto.Espacios
            .AsNoTracking()
            .Where(e => e.Id == espacioId)
            .Select(e => e.MonedaBase)
            .FirstOrDefaultAsync(cancelacion) ?? "DOP";
    }
}
