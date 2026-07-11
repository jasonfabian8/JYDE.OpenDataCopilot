using JYDE.OpenDataCopilot.Application.Catalog;

namespace JYDE.OpenDataCopilot.Api.Catalog;

/// <summary>Cuerpo opcional para solicitar una ingesta del catálogo.</summary>
/// <param name="Categories">Categorías a incluir; nula o vacía = todas.</param>
/// <param name="Limit">Máximo de datasets a ingerir; nulo = sin límite.</param>
public sealed record CatalogIngestRequest(IReadOnlyList<string>? Categories = null, int? Limit = null)
{
    // Cotas de servidor: datos del usuario no deben gobernar los bucles aguas abajo (CWE-834 / S6680).
    private const int MaxLimit = 10000;
    private const int MaxCategories = 50;

    /// <summary>
    /// Mapea el request a un <see cref="CatalogFilter"/> saneado en la frontera: acota el límite y el
    /// número de categorías para que un cliente no pueda inflar los bucles de paginación o de consulta.
    /// </summary>
    /// <returns>Filtro con valores acotados y seguros para las capas internas.</returns>
    public CatalogFilter ToFilter()
    {
        IReadOnlyList<string>? categories =
            Categories is { Count: > 0 } items ? [.. items.Take(MaxCategories)] : null;
        int? limit = Limit is int value ? Math.Clamp(value, 0, MaxLimit) : null;
        return new CatalogFilter(categories, limit);
    }
}
