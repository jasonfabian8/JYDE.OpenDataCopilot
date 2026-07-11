namespace JYDE.OpenDataCopilot.Domain.Catalog;

/// <summary>
/// Value object con los metadatos descriptivos de un <see cref="Dataset"/> (descripción, categoría,
/// etiquetas, columnas, fuente y última actualización). Agrupa la información no identificatoria del
/// agregado para descubrimiento e indexación.
/// </summary>
public sealed class DatasetMetadata
{
    /// <summary>Descripción del dataset (puede ser nula).</summary>
    public string? Description { get; }

    /// <summary>Categoría temática asignada en el portal (puede ser nula).</summary>
    public string? Category { get; }

    /// <summary>Etiquetas asociadas al dataset.</summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>Columnas (metadatos) del dataset.</summary>
    public IReadOnlyList<DatasetColumn> Columns { get; }

    /// <summary>URL pública del dataset en el portal (fuente para la cita).</summary>
    public Uri? SourceUrl { get; }

    /// <summary>Fecha de última actualización de los datos, si se conoce.</summary>
    public DateTimeOffset? UpdatedAt { get; }

    /// <summary>Crea los metadatos de un dataset (todos los campos son opcionales).</summary>
    /// <param name="description">Descripción opcional.</param>
    /// <param name="category">Categoría temática opcional.</param>
    /// <param name="tags">Etiquetas (se normaliza a lista vacía si es nula).</param>
    /// <param name="columns">Columnas (se normaliza a lista vacía si es nula).</param>
    /// <param name="sourceUrl">URL pública del dataset.</param>
    /// <param name="updatedAt">Fecha de última actualización.</param>
    public DatasetMetadata(
        string? description = null,
        string? category = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyList<DatasetColumn>? columns = null,
        Uri? sourceUrl = null,
        DateTimeOffset? updatedAt = null)
    {
        Description = description;
        Category = category;
        Tags = tags ?? [];
        Columns = columns ?? [];
        SourceUrl = sourceUrl;
        UpdatedAt = updatedAt;
    }

    /// <summary>Metadatos vacíos (sin descripción, categoría ni columnas).</summary>
    public static DatasetMetadata Empty { get; } = new();
}
