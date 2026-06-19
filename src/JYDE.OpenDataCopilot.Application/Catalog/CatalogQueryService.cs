using JYDE.OpenDataCopilot.Domain.Catalog;

namespace JYDE.OpenDataCopilot.Application.Catalog;

/// <summary>
/// Caso de uso de lectura del catálogo. Expone DTOs de aplicación, de modo que la presentación no
/// dependa del dominio ni invoque los puertos de salida directamente.
/// </summary>
public sealed class CatalogQueryService
{
    private readonly ICatalogRepository _repository;

    /// <summary>Crea el servicio de consulta del catálogo.</summary>
    /// <param name="repository">Repositorio del catálogo.</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="repository"/> es nulo.</exception>
    public CatalogQueryService(ICatalogRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <summary>Obtiene un dataset por su identificador, mapeado a DTO.</summary>
    /// <param name="id">Identificador del dataset (formato 4x4).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El DTO del dataset, o <c>null</c> si no existe.</returns>
    /// <exception cref="ArgumentException">Si <paramref name="id"/> no tiene un formato válido.</exception>
    public async Task<DatasetDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        DatasetId datasetId = new(id);
        Dataset? dataset = await _repository.GetByIdAsync(datasetId, cancellationToken);
        return dataset is null ? null : DatasetDto.From(dataset);
    }

    /// <summary>Cuenta los datasets almacenados.</summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => _repository.CountAsync(cancellationToken);
}
