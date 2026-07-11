namespace JYDE.OpenDataCopilot.Application.Search;

/// <summary>
/// Puerto de salida: índice de búsqueda de datasets (semántico). Permite indexar vectores y
/// recuperar los más similares a una consulta.
/// </summary>
public interface IDatasetSearchIndex
{
    /// <summary>Inserta o actualiza un lote de datasets en el índice.</summary>
    /// <param name="datasets">Datasets con su embedding.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task IndexAsync(IReadOnlyCollection<DatasetVector> datasets, CancellationToken cancellationToken = default);

    /// <summary>Recupera los <paramref name="topK"/> datasets más similares al embedding de consulta.</summary>
    /// <param name="queryEmbedding">Embedding de la consulta.</param>
    /// <param name="topK">Número máximo de resultados.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<DatasetSearchHit>> SearchAsync(
        IReadOnlyList<float> queryEmbedding,
        int topK,
        CancellationToken cancellationToken = default);
}
