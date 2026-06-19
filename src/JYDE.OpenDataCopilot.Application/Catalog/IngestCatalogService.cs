using JYDE.OpenDataCopilot.Domain.Catalog;

namespace JYDE.OpenDataCopilot.Application.Catalog;

/// <summary>
/// Caso de uso: ingerir el catálogo de datasets desde una <see cref="ICatalogSource"/> y
/// persistirlo en una <see cref="ICatalogRepository"/>, guardando por lotes para soportar
/// catálogos grandes.
/// </summary>
public sealed class IngestCatalogService
{
    /// <summary>Tamaño de lote por defecto para la persistencia.</summary>
    public const int DefaultBatchSize = 500;

    private readonly ICatalogSource _source;
    private readonly ICatalogRepository _repository;
    private readonly int _batchSize;

    /// <summary>Crea el servicio de ingesta.</summary>
    /// <param name="source">Fuente del catálogo.</param>
    /// <param name="repository">Repositorio donde se persiste.</param>
    /// <param name="batchSize">Tamaño de lote para guardar (por defecto <see cref="DefaultBatchSize"/>).</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="source"/> o <paramref name="repository"/> son nulos.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Si <paramref name="batchSize"/> es menor que 1.</exception>
    public IngestCatalogService(ICatalogSource source, ICatalogRepository repository, int batchSize = DefaultBatchSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);

        _source = source;
        _repository = repository;
        _batchSize = batchSize;
    }

    /// <summary>Ejecuta la ingesta del catálogo según el <paramref name="filter"/>.</summary>
    /// <param name="filter">Criterios para acotar la ingesta.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Resumen con la cantidad de datasets ingeridos.</returns>
    public async Task<IngestCatalogResult> ExecuteAsync(CatalogFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        int total = 0;
        List<Dataset> batch = new(_batchSize);

        await foreach (Dataset dataset in _source.FetchAsync(filter, cancellationToken).WithCancellation(cancellationToken))
        {
            batch.Add(dataset);
            total++;

            if (batch.Count >= _batchSize)
            {
                await _repository.SaveAsync(batch, cancellationToken);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await _repository.SaveAsync(batch, cancellationToken);
        }

        return new IngestCatalogResult(total);
    }
}
