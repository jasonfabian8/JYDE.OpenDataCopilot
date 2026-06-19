using JYDE.OpenDataCopilot.Domain.Catalog;

namespace JYDE.OpenDataCopilot.Application.Catalog;

/// <summary>DTO de aplicación para un dataset (frontera de salida; no expone el dominio).</summary>
/// <param name="Id">Identificador 4x4 del dataset.</param>
/// <param name="Name">Nombre del dataset.</param>
/// <param name="Description">Descripción.</param>
/// <param name="Category">Categoría temática.</param>
/// <param name="Tags">Etiquetas.</param>
/// <param name="Columns">Columnas (metadatos).</param>
/// <param name="SourceUrl">URL pública (fuente para la cita).</param>
/// <param name="UpdatedAt">Última actualización.</param>
public sealed record DatasetDto(
    string Id,
    string Name,
    string? Description,
    string? Category,
    IReadOnlyList<string> Tags,
    IReadOnlyList<DatasetColumnDto> Columns,
    string? SourceUrl,
    DateTimeOffset? UpdatedAt)
{
    /// <summary>Crea el DTO a partir del agregado de dominio.</summary>
    /// <param name="dataset">Dataset de dominio.</param>
    public static DatasetDto From(Dataset dataset)
        => new(
            dataset.Id.Value,
            dataset.Name,
            dataset.Description,
            dataset.Category,
            dataset.Tags,
            [.. dataset.Columns.Select(DatasetColumnDto.From)],
            dataset.SourceUrl?.ToString(),
            dataset.UpdatedAt);
}
