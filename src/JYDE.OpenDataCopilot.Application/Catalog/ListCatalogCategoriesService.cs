namespace JYDE.OpenDataCopilot.Application.Catalog;

/// <summary>
/// Caso de uso: listar las categorías temáticas del catálogo (con su conteo) para que la interfaz
/// de operación pueda acotar la ingesta por temas.
/// </summary>
public sealed class ListCatalogCategoriesService
{
    private readonly ICatalogSource _source;

    /// <summary>Crea el servicio de listado de categorías.</summary>
    /// <param name="source">Fuente del catálogo.</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="source"/> es nulo.</exception>
    public ListCatalogCategoriesService(ICatalogSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
    }

    /// <summary>Devuelve las categorías del catálogo, ordenadas por conteo (según la fuente).</summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    public Task<IReadOnlyList<CatalogCategory>> ExecuteAsync(CancellationToken cancellationToken = default)
        => _source.GetCategoriesAsync(cancellationToken);
}
