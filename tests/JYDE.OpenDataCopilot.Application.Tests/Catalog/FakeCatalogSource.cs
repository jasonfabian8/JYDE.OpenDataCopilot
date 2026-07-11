using System.Runtime.CompilerServices;
using JYDE.OpenDataCopilot.Application.Catalog;
using JYDE.OpenDataCopilot.Domain.Catalog;

namespace JYDE.OpenDataCopilot.Application.Tests.Catalog;

/// <summary>Doble de prueba de <see cref="ICatalogSource"/> que emite una lista predefinida.</summary>
public sealed class FakeCatalogSource : ICatalogSource
{
    private readonly IReadOnlyList<Dataset> _datasets;

    /// <summary>Crea la fuente con los datasets a emitir.</summary>
    public FakeCatalogSource(IReadOnlyList<Dataset> datasets) => _datasets = datasets;

    /// <summary>Último filtro recibido en <see cref="FetchAsync"/>.</summary>
    public CatalogFilter? LastFilter { get; private set; }

    /// <summary>Categorías a devolver en <see cref="GetCategoriesAsync"/>.</summary>
    public IReadOnlyList<CatalogCategory> Categories { get; init; } = [];

    /// <inheritdoc />
    public async IAsyncEnumerable<Dataset> FetchAsync(
        CatalogFilter filter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LastFilter = filter;
        foreach (Dataset dataset in _datasets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return dataset;
            await Task.Yield();
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CatalogCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Categories);
}
