namespace JYDE.OpenDataCopilot.Application.Search;

/// <summary>
/// Caso de uso: dada una consulta en lenguaje natural, recuperar los datasets más relevantes
/// (genera el embedding de la consulta y lo busca en el índice).
/// </summary>
public sealed class SearchDatasetsService
{
    /// <summary>Número de resultados por defecto.</summary>
    public const int DefaultTopK = 5;

    private readonly IEmbeddingGenerator _embeddings;
    private readonly IDatasetSearchIndex _index;

    /// <summary>Crea el servicio de búsqueda.</summary>
    /// <param name="embeddings">Generador de embeddings.</param>
    /// <param name="index">Índice de búsqueda.</param>
    /// <exception cref="ArgumentNullException">Si alguna dependencia es nula.</exception>
    public SearchDatasetsService(IEmbeddingGenerator embeddings, IDatasetSearchIndex index)
    {
        ArgumentNullException.ThrowIfNull(embeddings);
        ArgumentNullException.ThrowIfNull(index);

        _embeddings = embeddings;
        _index = index;
    }

    /// <summary>Busca datasets relevantes para la consulta.</summary>
    /// <param name="query">Consulta en lenguaje natural.</param>
    /// <param name="topK">Número máximo de resultados.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Datasets ordenados por relevancia.</returns>
    /// <exception cref="ArgumentException">Si la consulta está vacía.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Si <paramref name="topK"/> es menor que 1.</exception>
    public async Task<IReadOnlyList<DatasetSearchHit>> ExecuteAsync(
        string query,
        int topK = DefaultTopK,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("La consulta no puede estar vacía.", nameof(query));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(topK, 1);

        IReadOnlyList<float> queryEmbedding = await _embeddings.GenerateAsync(query, cancellationToken);
        return await _index.SearchAsync(queryEmbedding, topK, cancellationToken);
    }
}
