namespace JYDE.OpenDataCopilot.Application.Catalog;

/// <summary>
/// Filtro para acotar la ingesta del catálogo (p. ej. por categorías temáticas o un límite máximo).
/// </summary>
/// <param name="Categories">Categorías a incluir; si es nula o vacía, se consideran todas.</param>
/// <param name="Limit">Número máximo de datasets a ingerir; si es nulo, sin límite.</param>
public sealed record CatalogFilter(IReadOnlyList<string>? Categories = null, int? Limit = null)
{
    /// <summary>Filtro que no acota nada (todo el catálogo).</summary>
    public static CatalogFilter All { get; } = new();
}
