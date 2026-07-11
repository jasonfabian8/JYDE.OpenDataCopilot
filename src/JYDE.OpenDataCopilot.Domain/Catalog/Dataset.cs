namespace JYDE.OpenDataCopilot.Domain.Catalog;

/// <summary>
/// Agregado raíz del bounded context <c>Catalog</c>: representa un dataset publicado en
/// <c>datos.gov.co</c> junto con sus metadatos descriptivos (para descubrimiento e indexación).
/// </summary>
public sealed class Dataset
{
    private readonly DatasetMetadata _metadata;

    /// <summary>Identificador único del dataset (formato 4x4 de Socrata).</summary>
    public DatasetId Id { get; }

    /// <summary>Nombre del dataset.</summary>
    public string Name { get; }

    /// <summary>Descripción del dataset (puede ser nula).</summary>
    public string? Description => _metadata.Description;

    /// <summary>Categoría temática asignada en el portal (puede ser nula).</summary>
    public string? Category => _metadata.Category;

    /// <summary>Etiquetas asociadas al dataset.</summary>
    public IReadOnlyList<string> Tags => _metadata.Tags;

    /// <summary>Columnas (metadatos) del dataset.</summary>
    public IReadOnlyList<DatasetColumn> Columns => _metadata.Columns;

    /// <summary>URL pública del dataset en el portal (fuente para la cita).</summary>
    public Uri? SourceUrl => _metadata.SourceUrl;

    /// <summary>Fecha de última actualización de los datos, si se conoce.</summary>
    public DateTimeOffset? UpdatedAt => _metadata.UpdatedAt;

    /// <summary>Crea un dataset validando sus invariantes mínimas.</summary>
    /// <param name="id">Identificador del dataset.</param>
    /// <param name="name">Nombre del dataset (obligatorio).</param>
    /// <param name="metadata">Metadatos descriptivos (opcional; vacíos si es nulo).</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="id"/> es nulo.</exception>
    /// <exception cref="ArgumentException">Si <paramref name="name"/> está vacío.</exception>
    public Dataset(DatasetId id, string name, DatasetMetadata? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre del dataset es obligatorio.", nameof(name));
        }

        Id = id;
        Name = name.Trim();
        _metadata = metadata ?? DatasetMetadata.Empty;
    }
}
