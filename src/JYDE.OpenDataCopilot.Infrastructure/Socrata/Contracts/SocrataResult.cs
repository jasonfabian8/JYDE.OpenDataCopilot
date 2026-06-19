using System.Text.Json.Serialization;

namespace JYDE.OpenDataCopilot.Infrastructure.Socrata.Contracts;

/// <summary>Un elemento de <c>results</c> del catálogo de Socrata (DTO de transporte).</summary>
internal sealed record SocrataResult
{
    [JsonPropertyName("resource")]
    public SocrataResource? Resource { get; init; }

    [JsonPropertyName("classification")]
    public SocrataClassification? Classification { get; init; }

    [JsonPropertyName("permalink")]
    public string? Permalink { get; init; }

    [JsonPropertyName("link")]
    public string? Link { get; init; }
}
