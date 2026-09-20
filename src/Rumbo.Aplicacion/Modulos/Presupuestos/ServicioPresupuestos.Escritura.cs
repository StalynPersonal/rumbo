using Microsoft.EntityFrameworkCore;

using Rumbo.Contratos.Presupuestos;
using Rumbo.Dominio.Entidades.Planificacion;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;

namespace Rumbo.Aplicacion.Modulos.Presupuestos;

/// <summary>
/// Parte del servicio de presupuestos que crea, modifica y elimina.
/// </summary>
public partial class ServicioPresupuestos
{
    /// <inheritdoc />
    public async Task<PresupuestoDetalle> CrearAsync(
        SolicitudGuardarPresupuesto solicitud,
        CancellationToken cancelacion = default)
    {
        var presupuesto = new Presupuesto { Nombre = string.Empty, Moneda = string.Empty };

        await AplicarAsync(presupuesto, solicitud, cancelacion);

        contexto.Presupuestos.Add(presupuesto);
        await contexto.SaveChangesAsync(cancelacion);

        return await ObtenerAsync(presupuesto.Id, cancelacion);
    }

    /// <inheritdoc />
    public async Task<PresupuestoDetalle> ActualizarAsync(
        Guid presupuestoId,
        SolicitudGuardarPresupuesto solicitud,
        CancellationToken cancelacion = default)
    {
        var presupuesto = await contexto.Presupuestos
            .Include(p => p.Lineas)
            .FirstOrDefaultAsync(p => p.Id == presupuestoId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("el presupuesto");

        await AplicarAsync(presupuesto, solicitud, cancelacion);
        await contexto.SaveChangesAsync(cancelacion);

        return await ObtenerAsync(presupuestoId, cancelacion);
    }

    /// <inheritdoc />
    public async Task EliminarAsync(Guid presupuestoId, CancellationToken cancelacion = default)
    {
        var presupuesto = await contexto.Presupuestos
            .FirstOrDefaultAsync(p => p.Id == presupuestoId, cancelacion)
            ?? throw new ExcepcionNoEncontrado("el presupuesto");

        // A diferencia de una cuenta o una meta, un presupuesto se borra sin mas: es una
        // intencion de gasto, no dinero, y ningun movimiento apunta a el.
        contexto.Presupuestos.Remove(presupuesto);
        await contexto.SaveChangesAsync(cancelacion);
    }

    /// <summary>Vuelca la solicitud sobre la entidad y reconstruye sus partidas.</summary>
    /// <param name="presupuesto">Entidad que se rellena.</param>
    /// <param name="solicitud">Datos recibidos.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando se aplican los datos.</returns>
    private async Task AplicarAsync(
        Presupuesto presupuesto,
        SolicitudGuardarPresupuesto solicitud,
        CancellationToken cancelacion)
    {
        var nombre = solicitud.Nombre?.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ExcepcionDominio("El presupuesto necesita un nombre.");
        }

        if (!Enum.TryParse<TipoPeriodoPresupuesto>(
                solicitud.TipoPeriodo, ignoreCase: true, out var tipoPeriodo))
        {
            throw new ExcepcionDominio(
                "El tipo de periodo debe ser Mensual, Trimestral, Anual o Personalizado.");
        }

        if (solicitud.FinPeriodo < solicitud.InicioPeriodo)
        {
            throw new ExcepcionDominio("El periodo no puede terminar antes de empezar.");
        }

        if (solicitud.Lineas is null || solicitud.Lineas.Count == 0)
        {
            throw new ExcepcionDominio(
                "Un presupuesto sin partidas no controla nada. Añade al menos una categoría.");
        }

        var repetidas = solicitud.Lineas
            .GroupBy(l => l.CategoriaId)
            .Any(g => g.Count() > 1);

        if (repetidas)
        {
            // Con una categoria repetida, el consumo se compararia contra un limite ambiguo.
            throw new ExcepcionDominio(
                "Hay categorías repetidas. Cada categoría debe aparecer una sola vez.");
        }

        await ValidarLineasAsync(solicitud.Lineas, cancelacion);

        presupuesto.Nombre = nombre;
        presupuesto.TipoPeriodo = tipoPeriodo;
        presupuesto.InicioPeriodo = solicitud.InicioPeriodo;
        presupuesto.FinPeriodo = solicitud.FinPeriodo;
        presupuesto.Notas = solicitud.Notas?.Trim();

        presupuesto.Moneda = string.IsNullOrWhiteSpace(solicitud.Moneda)
            ? await ObtenerMonedaBaseAsync(cancelacion)
            : solicitud.Moneda.Trim().ToUpperInvariant();

        // Las partidas se reemplazan en bloque. Casarlas una a una con las existentes
        // complicaria el codigo sin ganar nada: no guardan historial propio, el gasto real
        // vive en el libro mayor.
        presupuesto.Lineas.Clear();

        foreach (var linea in solicitud.Lineas)
        {
            presupuesto.Lineas.Add(new LineaPresupuesto
            {
                CategoriaId = linea.CategoriaId,
                MontoAsignado = linea.MontoAsignado,
                UmbralAviso = linea.UmbralAviso,
                UmbralCritico = linea.UmbralCritico,
                UmbralExcedido = linea.UmbralExcedido,
            });
        }
    }

    /// <summary>Comprueba las partidas antes de guardarlas.</summary>
    /// <param name="lineas">Partidas propuestas.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Tarea que finaliza cuando termina la validacion.</returns>
    private async Task ValidarLineasAsync(
        IReadOnlyList<LineaPresupuestoSolicitud> lineas,
        CancellationToken cancelacion)
    {
        foreach (var linea in lineas)
        {
            if (linea.MontoAsignado < 0m)
            {
                throw new ExcepcionDominio("Los importes asignados no pueden ser negativos.");
            }

            var categoria = await contexto.Categorias
                .FirstOrDefaultAsync(c => c.Id == linea.CategoriaId, cancelacion)
                ?? throw new ExcepcionNoEncontrado("una de las categorías");

            if (categoria.Tipo == TipoCategoria.Ingreso)
            {
                throw new ExcepcionDominio(
                    $"«{categoria.Nombre}» es una categoría de ingreso. Un presupuesto limita "
                    + "gastos, no ingresos.");
            }

            if (linea.UmbralAviso.HasValue && linea.UmbralCritico.HasValue
                && linea.UmbralAviso > linea.UmbralCritico)
            {
                throw new ExcepcionDominio(
                    $"En «{categoria.Nombre}», el umbral de aviso no puede superar al crítico.");
            }

            if (linea.UmbralCritico.HasValue && linea.UmbralExcedido.HasValue
                && linea.UmbralCritico > linea.UmbralExcedido)
            {
                throw new ExcepcionDominio(
                    $"En «{categoria.Nombre}», el umbral crítico no puede superar al de exceso.");
            }
        }
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
