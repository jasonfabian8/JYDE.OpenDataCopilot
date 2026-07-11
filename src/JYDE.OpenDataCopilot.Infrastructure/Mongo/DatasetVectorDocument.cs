using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson.Serialization.Attributes;

namespace JYDE.OpenDataCopilot.Infrastructure.Mongo;

/// <summary>Documento de persistencia (MongoDB) de un dataset vectorizado para la búsqueda.</summary>
[BsonIgnoreExtraElements]
[ExcludeFromCodeCoverage]
internal sealed class DatasetVectorDocument
{
    /// <summary>Identificador del dataset (clave del documento).</summary>
    [BsonId]
    public string Id { get; set; } = string.Empty;

    /// <summary>Nombre del dataset.</summary>
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Categoría temática.</summary>
    [BsonElement("category")]
    public string? Category { get; set; }

    /// <summary>URL pública del dataset.</summary>
    [BsonElement("sourceUrl")]
    public string? SourceUrl { get; set; }

    /// <summary>Embedding del dataset (campo indexado por Atlas Vector Search).</summary>
    [BsonElement("embedding")]
    public float[] Embedding { get; set; } = [];
}
