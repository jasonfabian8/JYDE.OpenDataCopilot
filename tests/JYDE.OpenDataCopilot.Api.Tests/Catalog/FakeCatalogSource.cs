using System.Runtime.CompilerServices;
using JYDE.OpenDataCopilot.Application.Catalog;
using JYDE.OpenDataCopilot.Domain.Catalog;

namespace JYDE.OpenDataCopilot.Api.Tests.Catalog;

/// <summary>Doble de prueba de <see cref="ICatalogSource"/> que emite una lista predefinida.</summary>
internal sealed class FakeCatalogSource : ICatalogSource
{
    private readonly IReadOnlyList<Dataset> _datasets;

    public FakeCatalogSource(IReadOnlyList<Dataset> datasets) => _datasets = datasets;

    /// <summary>Categorías a devolver en <see cref="GetCategoriesAsync"/>.</summary>
    public IReadOnlyList<CatalogCategory> Categories { get; init; } = [];

    public async IAsyncEnumerable<Dataset> FetchAsync(
        CatalogFilter filter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int emitted = 0;
        foreach (Dataset dataset in _datasets)
        {
            if (filter.Limit.HasValue && emitted >= filter.Limit.Value)
            {
                yield break;
            }

            cancellationToken.ThrowIfCancellationRequested();
            yield return dataset;
            emitted++;
            await Task.Yield();
        }
    }

    public Task<IReadOnlyList<CatalogCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Categories);
}
