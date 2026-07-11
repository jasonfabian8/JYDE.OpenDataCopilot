using JYDE.OpenDataCopilot.Application.Search;

namespace JYDE.OpenDataCopilot.Application.Tests.Search;

/// <summary>Doble de prueba de <see cref="IDatasetSearchIndex"/> que captura lo indexado/consultado.</summary>
internal sealed class CapturingSearchIndex : IDatasetSearchIndex
{
    /// <summary>Todos los datasets indexados.</summary>
    public List<DatasetVector> Indexed { get; } = [];

    /// <summary>Número de llamadas a <see cref="IndexAsync"/> (para verificar lotes).</summary>
    public int IndexCallCount { get; private set; }

    /// <summary>Embedding de consulta recibido en la última búsqueda.</summary>
    public IReadOnlyList<float>? LastQuery { get; private set; }

    /// <summary>Resultados que devolverá <see cref="SearchAsync"/>.</summary>
    public IReadOnlyList<DatasetSearchHit> NextResults { get; set; } = [];

    public Task IndexAsync(IReadOnlyCollection<DatasetVector> datasets, CancellationToken cancellationToken = default)
    {
        IndexCallCount++;
        Indexed.AddRange(datasets);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DatasetSearchHit>> SearchAsync(
        IReadOnlyList<float> queryEmbedding,
        int topK,
        CancellationToken cancellationToken = default)
    {
        LastQuery = queryEmbedding;
        return Task.FromResult(NextResults);
    }
}
