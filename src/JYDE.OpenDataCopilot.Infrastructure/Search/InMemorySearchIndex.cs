using System.Collections.Concurrent;
using JYDE.OpenDataCopilot.Application.Search;

namespace JYDE.OpenDataCopilot.Infrastructure.Search;

/// <summary>
/// Adaptador de <see cref="IDatasetSearchIndex"/> en memoria: guarda los vectores y calcula la
/// similitud de coseno en proceso. Opción de desarrollo/demo sin BD vectorial (registrar como
/// singleton). En producción se reemplaza por un índice gestionado (p. ej. Atlas Vector Search).
/// </summary>
public sealed class InMemorySearchIndex : IDatasetSearchIndex
{
    private readonly ConcurrentDictionary<string, DatasetVector> _store = new();

    /// <inheritdoc />
    public Task IndexAsync(IReadOnlyCollection<DatasetVector> datasets, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(datasets);
        foreach (DatasetVector dataset in datasets)
        {
            _store[dataset.Id] = dataset;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DatasetSearchHit>> SearchAsync(
        IReadOnlyList<float> queryEmbedding,
        int topK,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryEmbedding);
        ArgumentOutOfRangeException.ThrowIfLessThan(topK, 1);

        IReadOnlyList<DatasetSearchHit> hits = _store.Values
            .Select(dataset => new DatasetSearchHit(
                dataset.Id,
                dataset.Name,
                dataset.Category,
                dataset.SourceUrl,
                CosineSimilarity(queryEmbedding, dataset.Embedding)))
            .OrderByDescending(hit => hit.Score)
            .Take(topK)
            .ToList();

        return Task.FromResult(hits);
    }

    /// <summary>Similitud de coseno entre dos vectores; 0 si alguno es nulo/vacío o de distinta longitud.</summary>
    private static double CosineSimilarity(IReadOnlyList<float> a, IReadOnlyList<float> b)
    {
        if (a.Count == 0 || a.Count != b.Count)
        {
            return 0d;
        }

        double dot = 0d;
        double normA = 0d;
        double normB = 0d;
        for (int i = 0; i < a.Count; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0d || normB == 0d)
        {
            return 0d;
        }

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
