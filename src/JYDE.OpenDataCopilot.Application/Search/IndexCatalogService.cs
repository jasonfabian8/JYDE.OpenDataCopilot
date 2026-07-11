using JYDE.OpenDataCopilot.Application.Catalog;
using JYDE.OpenDataCopilot.Domain.Catalog;

namespace JYDE.OpenDataCopilot.Application.Search;

/// <summary>
/// Caso de uso: construir el índice de búsqueda a partir del catálogo almacenado. Por cada dataset
/// genera su embedding (sobre sus metadatos) y lo inserta en el índice, por lotes.
/// </summary>
public sealed class IndexCatalogService
{
    /// <summary>Tamaño de lote por defecto para indexar.</summary>
    public const int DefaultBatchSize = 200;

    private readonly ICatalogRepository _repository;
    private readonly IEmbeddingGenerator _embeddings;
    private readonly IDatasetSearchIndex _index;
    private readonly int _batchSize;

    /// <summary>Crea el servicio de indexación.</summary>
    /// <param name="repository">Repositorio del catálogo (origen de datos).</param>
    /// <param name="embeddings">Generador de embeddings.</param>
    /// <param name="index">Índice de búsqueda destino.</param>
    /// <param name="batchSize">Tamaño de lote (por defecto <see cref="DefaultBatchSize"/>).</param>
    /// <exception cref="ArgumentNullException">Si alguna dependencia es nula.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Si <paramref name="batchSize"/> es menor que 1.</exception>
    public IndexCatalogService(
        ICatalogRepository repository,
        IEmbeddingGenerator embeddings,
        IDatasetSearchIndex index,
        int batchSize = DefaultBatchSize)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(embeddings);
        ArgumentNullException.ThrowIfNull(index);
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);

        _repository = repository;
        _embeddings = embeddings;
        _index = index;
        _batchSize = batchSize;
    }

    /// <summary>Indexa todo el catálogo almacenado, generando los embeddings por lote.</summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Cantidad de datasets indexados.</returns>
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        int total = 0;
        List<Dataset> batch = new(_batchSize);

        await foreach (Dataset dataset in _repository.GetAllAsync(cancellationToken).WithCancellation(cancellationToken))
        {
            batch.Add(dataset);
            if (batch.Count >= _batchSize)
            {
                total += await IndexBatchAsync(batch, cancellationToken);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            total += await IndexBatchAsync(batch, cancellationToken);
        }

        return total;
    }

    /// <summary>Vectoriza un lote de datasets en una sola llamada de embeddings y lo indexa.</summary>
    private async Task<int> IndexBatchAsync(List<Dataset> datasets, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> texts = [.. datasets.Select(BuildText)];
        IReadOnlyList<IReadOnlyList<float>> embeddings = await _embeddings.GenerateBatchAsync(texts, cancellationToken);

        List<DatasetVector> vectors = new(datasets.Count);
        for (int i = 0; i < datasets.Count; i++)
        {
            Dataset dataset = datasets[i];
            vectors.Add(new DatasetVector(
                dataset.Id.Value,
                dataset.Name,
                dataset.Category,
                dataset.SourceUrl?.ToString(),
                embeddings[i]));
        }

        await _index.IndexAsync(vectors, cancellationToken);
        return vectors.Count;
    }

    /// <summary>Compone el texto a vectorizar a partir de los metadatos del dataset.</summary>
    private static string BuildText(Dataset dataset)
    {
        IEnumerable<string> parts =
        [
            dataset.Name,
            dataset.Description ?? string.Empty,
            dataset.Category ?? string.Empty,
            string.Join(' ', dataset.Tags),
            string.Join(' ', dataset.Columns.Select(column => column.Name)),
        ];

        return string.Join(' ', parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
