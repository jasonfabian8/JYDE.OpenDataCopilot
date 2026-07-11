using System.Text.Json.Serialization;

namespace JYDE.OpenDataCopilot.Infrastructure.Socrata.Contracts;

/// <summary>Una categoría del catálogo y su conteo, según Socrata (DTO de transporte).</summary>
internal sealed class SocrataDomainCategory
{
    [JsonPropertyName("domain_category")]
    public string? DomainCategory { get; init; }

    [JsonPropertyName("count")]
    public int Count { get; init; }
}
