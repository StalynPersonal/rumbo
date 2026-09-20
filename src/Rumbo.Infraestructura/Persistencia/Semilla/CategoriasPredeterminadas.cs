using Rumbo.Dominio.Enums;

namespace Rumbo.Infraestructura.Persistencia.Semilla;

/// <summary>
/// Arbol de categorias que se crea automaticamente al dar de alta un espacio.
/// </summary>
/// <remarks>
/// <para>
/// No se usa <c>HasData</c> como con las monedas porque las categorias pertenecen a un espacio
/// concreto: cada hogar tiene las suyas y puede renombrarlas o anadir las que quiera. Por eso
/// esto es una plantilla que el servicio de creacion de espacios materializa.
/// </para>
/// <para>
/// Que un espacio nuevo arranque con categorias sensatas importa mas de lo que parece: nadie
/// quiere crear veinte categorias a mano antes de poder anotar su primer gasto, y sin
/// categorias los informes no dicen nada.
/// </para>
/// </remarks>
public static class CategoriasPredeterminadas
{
    /// <summary>
    /// Plantilla de una categoria predeterminada.
    /// </summary>
    /// <param name="Nombre">Nombre visible.</param>
    /// <param name="Tipo">Si sirve para ingresos, gastos o ambos.</param>
    /// <param name="Icono">Icono que la representa.</param>
    /// <param name="Subcategorias">Categorias hijas.</param>
    public record Plantilla(
        string Nombre,
        TipoCategoria Tipo,
        string Icono,
        string[] Subcategorias);

    /// <summary>Categorias de GASTO que recibe todo espacio nuevo.</summary>
    public static readonly Plantilla[] Gastos =
    [
        new("Alimentacion", TipoCategoria.Gasto, "cart",
            ["Supermercado", "Restaurantes", "Delivery", "Cafeteria"]),

        new("Transporte", TipoCategoria.Gasto, "car",
            ["Combustible", "Uber", "Mantenimiento", "Parqueo", "Transporte publico", "Peajes"]),

        new("Vivienda", TipoCategoria.Gasto, "home",
            ["Alquiler", "Hipoteca", "Electricidad", "Agua", "Internet", "Gas", "Mantenimiento"]),

        new("Entretenimiento", TipoCategoria.Gasto, "film",
            ["Cine", "Streaming", "Salidas", "Eventos", "Pasatiempos"]),

        new("Salud", TipoCategoria.Gasto, "heart",
            ["Medicamentos", "Consultas", "Seguro medico", "Laboratorio", "Dentista"]),

        new("Compras", TipoCategoria.Gasto, "bag",
            ["Ropa", "Tecnologia", "Hogar", "Regalos", "Cuidado personal"]),

        new("Educacion", TipoCategoria.Gasto, "book",
            ["Colegiatura", "Cursos", "Libros", "Materiales"]),

        new("Servicios financieros", TipoCategoria.Gasto, "bank",
            ["Comisiones bancarias", "Intereses", "Seguros", "Impuestos"]),

        new("Mascotas", TipoCategoria.Gasto, "paw",
            ["Alimento", "Veterinario", "Accesorios"]),

        new("Viajes", TipoCategoria.Gasto, "plane",
            ["Vuelos", "Hospedaje", "Actividades", "Documentos"]),

        new("Otros gastos", TipoCategoria.Gasto, "dots", []),
    ];

    /// <summary>Categorias de INGRESO que recibe todo espacio nuevo.</summary>
    public static readonly Plantilla[] Ingresos =
    [
        new("Salario", TipoCategoria.Ingreso, "wallet",
            ["Sueldo", "Horas extra", "Bonificaciones", "Regalia"]),

        new("Trabajo independiente", TipoCategoria.Ingreso, "briefcase",
            ["Freelance", "Consultoria", "Comisiones"]),

        new("Negocio", TipoCategoria.Ingreso, "store",
            ["Ventas", "Servicios"]),

        new("Inversiones", TipoCategoria.Ingreso, "trending-up",
            ["Intereses", "Dividendos", "Alquileres"]),

        new("Otros ingresos", TipoCategoria.Ingreso, "gift",
            ["Regalos", "Reembolsos", "Venta de articulos"]),
    ];

    /// <summary>
    /// Categoria que reciben los ajustes de saldo, disponible para ingresos y gastos.
    /// </summary>
    /// <remarks>
    /// Existe para que un ajuste manual tenga donde clasificarse sin ensuciar una categoria
    /// real: si un ajuste fuera a "Otros gastos", los informes de consumo mentirian.
    /// </remarks>
    public static readonly Plantilla Ajustes =
        new("Ajustes de saldo", TipoCategoria.Ambos, "sliders", []);

    /// <summary>Todas las plantillas, en el orden en que se crean.</summary>
    /// <returns>Secuencia de plantillas de categoria.</returns>
    public static IEnumerable<Plantilla> Todas()
    {
        foreach (var plantilla in Ingresos)
        {
            yield return plantilla;
        }

        foreach (var plantilla in Gastos)
        {
            yield return plantilla;
        }

        yield return Ajustes;
    }
}
