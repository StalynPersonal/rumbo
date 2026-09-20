using Rumbo.Dominio.Comun;
using Rumbo.Dominio.Enums;

namespace Rumbo.Dominio.Entidades.Financiero;

/// <summary>
/// Clasificacion de un movimiento, organizada en dos niveles: categoria y subcategoria.
/// </summary>
/// <remarks>
/// <para>
/// Ejemplo de la jerarquia: <c>Alimentacion</c> con hijas <c>Supermercado</c>,
/// <c>Restaurantes</c> y <c>Delivery</c>.
/// </para>
/// <para>
/// La profundidad se limita a dos niveles a proposito. Una jerarquia sin limite complica cada
/// informe (hay que decidir hasta donde agregar) y en la practica nadie necesita un tercer
/// nivel para llevar las cuentas de casa.
/// </para>
/// </remarks>
public class Categoria : EntidadDeEspacio
{
    /// <summary>Nombre visible, por ejemplo "Supermercado".</summary>
    public required string Nombre { get; set; }

    /// <summary>Categoria padre, o <c>null</c> si es de primer nivel.</summary>
    public Guid? CategoriaPadreId { get; set; }

    /// <summary>Categoria padre.</summary>
    public Categoria? CategoriaPadre { get; set; }

    /// <summary>Subcategorias que cuelgan de esta.</summary>
    public ICollection<Categoria> Subcategorias { get; set; } = [];

    /// <summary>En que clase de movimientos puede usarse.</summary>
    public TipoCategoria Tipo { get; set; } = TipoCategoria.Gasto;

    /// <summary>Nombre del icono que la representa en la aplicacion movil.</summary>
    public string? Icono { get; set; }

    /// <summary>Color en formato hexadecimal, por ejemplo <c>#4CAF50</c>.</summary>
    public string? Color { get; set; }

    /// <summary>
    /// Indica si la creo Rumbo al dar de alta el espacio, frente a las que anade el usuario.
    /// </summary>
    /// <remarks>
    /// Las del sistema pueden renombrarse pero no eliminarse, para que los informes
    /// predefinidos no se queden sin datos.
    /// </remarks>
    public bool EsDelSistema { get; set; }

    /// <summary>Indica si sigue disponible al registrar movimientos.</summary>
    public bool Activa { get; set; } = true;

    /// <summary>Orden en que se muestra dentro de su nivel.</summary>
    public int Orden { get; set; }

    /// <summary>Movimientos clasificados en esta categoria.</summary>
    public ICollection<Movimiento> Movimientos { get; set; } = [];
}
