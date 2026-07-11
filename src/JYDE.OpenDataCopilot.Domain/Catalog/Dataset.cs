namespace JYDE.OpenDataCopilot.Domain.Catalog;

/// <summary>
/// Agregado raíz del bounded context <c>Catalog</c>: representa un dataset publicado en
/// <c>datos.gov.co</c> junto con sus metadatos descriptivos (para descubrimiento e indexación).
/// </summary>
public sealed class Dataset
{
    /// <summary>Identificador único del dataset (formato 4x4 de Socrata).</summary>
    public DatasetId Id { get; }

    /// <summary>Nombre del dataset.</summary>
    public string Name { get; }

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

    /// <summary>Crea un dataset validando sus invariantes mínimas.</summary>
    /// <param name="id">Identificador del dataset.</param>
    /// <param name="name">Nombre del dataset (obligatorio).</param>
    /// <param name="description">Descripción opcional.</param>
    /// <param name="category">Categoría temática opcional.</param>
    /// <param name="tags">Etiquetas (opcional; se normaliza a lista vacía si es nula).</param>
    /// <param name="columns">Columnas (opcional; se normaliza a lista vacía si es nula).</param>
    /// <param name="sourceUrl">URL pública del dataset.</param>
    /// <param name="updatedAt">Fecha de última actualización.</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="id"/> es nulo.</exception>
    /// <exception cref="ArgumentException">Si <paramref name="name"/> está vacío.</exception>
    public Dataset(
        DatasetId id,
        string name,
        string? description = null,
        string? category = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyList<DatasetColumn>? columns = null,
        Uri? sourceUrl = null,
        DateTimeOffset? updatedAt = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre del dataset es obligatorio.", nameof(name));
        }

        Id = id;
        Name = name.Trim();
        Description = description;
        Category = category;
        Tags = tags ?? [];
        Columns = columns ?? [];
        SourceUrl = sourceUrl;
        UpdatedAt = updatedAt;
    }
}
