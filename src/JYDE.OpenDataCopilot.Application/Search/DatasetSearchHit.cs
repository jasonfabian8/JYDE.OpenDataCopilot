namespace JYDE.OpenDataCopilot.Application.Search;

/// <summary>Resultado de búsqueda: un dataset relevante y su puntaje de similitud.</summary>
/// <param name="Id">Identificador del dataset.</param>
/// <param name="Name">Nombre del dataset.</param>
/// <param name="Category">Categoría temática (opcional).</param>
/// <param name="SourceUrl">URL pública (para la cita), si se conoce.</param>
/// <param name="Score">Puntaje de similitud (mayor = más relevante).</param>
public sealed record DatasetSearchHit(
    string Id,
    string Name,
    string? Category,
    string? SourceUrl,
    double Score);
