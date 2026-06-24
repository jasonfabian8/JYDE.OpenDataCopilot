using System.Runtime.CompilerServices;
using JYDE.OpenDataCopilot.Application.Catalog;
using JYDE.OpenDataCopilot.Domain.Catalog;

namespace JYDE.OpenDataCopilot.Application.Tests.Catalog;

/// <summary>Doble de prueba en memoria de <see cref="ICatalogRepository"/>.</summary>
public sealed class InMemoryCatalogRepository : ICatalogRepository
{
    private readonly Dictionary<string, Dataset> _store = [];

    /// <summary>Número de veces que se invocó <see cref="SaveAsync"/> (para verificar lotes).</summary>
    public int SaveCallCount { get; private set; }

    /// <inheritdoc />
    public Task SaveAsync(IReadOnlyCollection<Dataset> datasets, CancellationToken cancellationToken = default)
    {
        SaveCallCount++;
        foreach (Dataset dataset in datasets)
        {
            _store[dataset.Id.Value] = dataset;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Dataset?> GetByIdAsync(DatasetId id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id.Value, out Dataset? dataset);
        return Task.FromResult(dataset);
    }

    /// <inheritdoc />
    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_store.Count);

    /// <inheritdoc />
    public async IAsyncEnumerable<Dataset> GetAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (Dataset dataset in _store.Values)
        {
            yield return dataset;
            await Task.CompletedTask;
        }
    }
}
