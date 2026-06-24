namespace JYDE.OpenDataCopilot.Application.Search;

/// <summary>Un dataset listo para indexar: datos mínimos para mostrar + su embedding.</summary>
/// <param name="Id">Identificador del dataset.</param>
/// <param name="Name">Nombre del dataset.</param>
/// <param name="Category">Categoría temática (opcional).</param>
/// <param name="SourceUrl">URL pública (para la cita), si se conoce.</param>
/// <param name="Embedding">Vector de características del dataset.</param>
public sealed record DatasetVector(
    string Id,
    string Name,
    string? Category,
    string? SourceUrl,
    IReadOnlyList<float> Embedding);
