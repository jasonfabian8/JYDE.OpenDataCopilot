using System.Text.Json.Serialization;

namespace JYDE.OpenDataCopilot.Infrastructure.Foundry.Contracts;

/// <summary>Un elemento de la respuesta de embeddings de Foundry (DTO de transporte).</summary>
internal sealed class FoundryEmbeddingItem
{
    [JsonPropertyName("embedding")]
    public float[] Embedding { get; init; } = [];
}
