namespace JYDE.OpenDataCopilot.Application.Catalog;

/// <summary>Categoría temática del catálogo con su conteo de datasets en la fuente.</summary>
/// <param name="Name">Nombre de la categoría (p. ej. "Transporte").</param>
/// <param name="Count">Cantidad de datasets clasificados en la categoría.</param>
public sealed record CatalogCategory(string Name, int Count);
