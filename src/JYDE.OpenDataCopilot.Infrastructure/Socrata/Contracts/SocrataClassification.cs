using System.Text.Json.Serialization;

namespace JYDE.OpenDataCopilot.Infrastructure.Socrata.Contracts;

/// <summary>Sección <c>classification</c> de un resultado del catálogo de Socrata (DTO de transporte).</summary>
internal sealed record SocrataClassification
{
    [JsonPropertyName("domain_category")]
    public string? DomainCategory { get; init; }

    [JsonPropertyName("domain_tags")]
    public List<string>? DomainTags { get; init; }
}
