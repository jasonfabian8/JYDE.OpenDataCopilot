using JYDE.OpenDataCopilot.Domain.Catalog;
using MongoDB.Bson.Serialization.Attributes;

namespace JYDE.OpenDataCopilot.Infrastructure.Mongo;

/// <summary>Modelo de persistencia (MongoDB) de un dataset del catálogo.</summary>
[BsonIgnoreExtraElements]
internal sealed class DatasetDocument
{
    /// <summary>Identificador (clave del documento), igual al <see cref="DatasetId"/>.</summary>
    [BsonId]
    public string Id { get; set; } = string.Empty;

    /// <summary>Nombre del dataset.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Descripción.</summary>
    public string? Description { get; set; }

    /// <summary>Categoría temática.</summary>
    public string? Category { get; set; }

    /// <summary>Etiquetas.</summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>Columnas (metadatos).</summary>
    public List<DatasetColumnDocument> Columns { get; set; } = [];

    /// <summary>URL pública del dataset.</summary>
    public string? SourceUrl { get; set; }

    /// <summary>Fecha de última actualización.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Crea el documento a partir del agregado de dominio.</summary>
    public static DatasetDocument FromDomain(Dataset dataset) => new()
    {
        Id = dataset.Id.Value,
        Name = dataset.Name,
        Description = dataset.Description,
        Category = dataset.Category,
        Tags = [.. dataset.Tags],
        Columns = [.. dataset.Columns.Select(DatasetColumnDocument.FromDomain)],
        SourceUrl = dataset.SourceUrl?.ToString(),
        UpdatedAt = dataset.UpdatedAt,
    };

    /// <summary>Reconstruye el agregado de dominio.</summary>
    public Dataset ToDomain() => new(
        new DatasetId(Id),
        Name,
        Description,
        Category,
        Tags,
        [.. Columns.Select(column => column.ToDomain())],
        SourceUrl is null ? null : new Uri(SourceUrl),
        UpdatedAt);
}
