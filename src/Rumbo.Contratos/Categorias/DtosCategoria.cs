namespace Rumbo.Contratos.Categorias;

/// <summary>Categoria con sus subcategorias, tal como se muestra en el arbol.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Nombre">Nombre visible.</param>
/// <param name="Tipo">Ingreso, Gasto o Ambos.</param>
/// <param name="Icono">Nombre del icono.</param>
/// <param name="Color">Color en hexadecimal.</param>
/// <param name="EsDelSistema">
/// Si la creo Rumbo al dar de alta el espacio. Estas pueden renombrarse pero no eliminarse,
/// para que los informes predefinidos no se queden sin datos.
/// </param>
/// <param name="Activa">Si sigue disponible al registrar movimientos.</param>
/// <param name="Orden">Posicion dentro de su nivel.</param>
/// <param name="Subcategorias">Categorias hijas.</param>
public record CategoriaArbol(
    Guid Id,
    string Nombre,
    string Tipo,
    string? Icono,
    string? Color,
    bool EsDelSistema,
    bool Activa,
    int Orden,
    IReadOnlyList<CategoriaArbol> Subcategorias);

/// <summary>Datos para crear una categoria.</summary>
/// <param name="Nombre">Nombre visible.</param>
/// <param name="Tipo">Ingreso, Gasto o Ambos.</param>
/// <param name="CategoriaPadreId">
/// Categoria padre, o <c>null</c> si es de primer nivel. La jerarquia admite dos niveles.
/// </param>
/// <param name="Icono">Nombre del icono.</param>
/// <param name="Color">Color en hexadecimal.</param>
public record SolicitudCrearCategoria(
    string Nombre,
    string Tipo,
    Guid? CategoriaPadreId,
    string? Icono,
    string? Color);

/// <summary>Datos para modificar una categoria.</summary>
/// <param name="Nombre">Nombre visible.</param>
/// <param name="Icono">Nombre del icono.</param>
/// <param name="Color">Color en hexadecimal.</param>
/// <param name="Activa">Si sigue disponible.</param>
/// <param name="Orden">Posicion dentro de su nivel.</param>
/// <remarks>
/// El tipo y el padre no se pueden cambiar: hacerlo moveria de sitio movimientos ya
/// clasificados y alteraria informes de meses cerrados.
/// </remarks>
public record SolicitudActualizarCategoria(
    string Nombre,
    string? Icono,
    string? Color,
    bool Activa,
    int Orden);
