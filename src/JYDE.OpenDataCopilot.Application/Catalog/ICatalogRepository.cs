using JYDE.OpenDataCopilot.Domain.Catalog;

namespace JYDE.OpenDataCopilot.Application.Catalog;

/// <summary>
/// Puerto de salida: persistencia del catálogo de metadatos de datasets.
/// </summary>
public interface ICatalogRepository
{
    /// <summary>Guarda (inserta o actualiza) un lote de datasets.</summary>
    /// <param name="datasets">Datasets a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task SaveAsync(IReadOnlyCollection<Dataset> datasets, CancellationToken cancellationToken = default);

    /// <summary>Obtiene un dataset por su identificador, o <c>null</c> si no existe.</summary>
    /// <param name="id">Identificador del dataset.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<Dataset?> GetByIdAsync(DatasetId id, CancellationToken cancellationToken = default);

    /// <summary>Cuenta los datasets almacenados.</summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>Recupera todos los datasets almacenados, transmitidos de forma perezosa.</summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    IAsyncEnumerable<Dataset> GetAllAsync(CancellationToken cancellationToken = default);
}
