using System.Text.Json.Serialization;

namespace JYDE.OpenDataCopilot.Infrastructure.Foundry.Contracts;

/// <summary>Respuesta del endpoint de embeddings de Foundry/Azure OpenAI (DTO de transporte).</summary>
internal sealed class FoundryEmbeddingResponse
{
    [JsonPropertyName("data")]
    public List<FoundryEmbeddingItem> Data { get; init; } = [];
}
