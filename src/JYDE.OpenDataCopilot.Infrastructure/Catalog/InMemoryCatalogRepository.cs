using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using JYDE.OpenDataCopilot.Application.Catalog;
using JYDE.OpenDataCopilot.Domain.Catalog;

namespace JYDE.OpenDataCopilot.Infrastructure.Catalog;

/// <summary>
/// Adaptador en memoria de <see cref="ICatalogRepository"/>. Pensado para desarrollo y demo
/// (registrar como singleton). Thread-safe; en producción se reemplaza por un adaptador persistente.
/// </summary>
public sealed class InMemoryCatalogRepository : ICatalogRepository
{
    private readonly ConcurrentDictionary<string, Dataset> _store = new();

    /// <inheritdoc />
    public Task SaveAsync(IReadOnlyCollection<Dataset> datasets, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(datasets);
        foreach (Dataset dataset in datasets)
        {
            _store[dataset.Id.Value] = dataset;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Dataset?> GetByIdAsync(DatasetId id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
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
            cancellationToken.ThrowIfCancellationRequested();
            yield return dataset;
            await Task.CompletedTask;
        }
    }
}
