using JYDE.OpenDataCopilot.Domain.Catalog;

namespace JYDE.OpenDataCopilot.Application.Catalog;

/// <summary>
/// Puerto de salida: fuente externa del catálogo de datasets (p. ej. la API de catálogo de Socrata).
/// Devuelve los metadatos de los datasets de forma perezosa para soportar catálogos grandes.
/// </summary>
public interface ICatalogSource
{
    /// <summary>
    /// Recupera los datasets del catálogo que cumplen el <paramref name="filter"/>, transmitidos a
    /// medida que se obtienen (paginación interna a cargo del adaptador).
    /// </summary>
    /// <param name="filter">Criterios para acotar la búsqueda.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Secuencia asíncrona de datasets.</returns>
    IAsyncEnumerable<Dataset> FetchAsync(CatalogFilter filter, CancellationToken cancellationToken = default);
}
