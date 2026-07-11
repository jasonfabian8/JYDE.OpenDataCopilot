using System.Text.Json.Serialization;

namespace JYDE.OpenDataCopilot.Infrastructure.Socrata.Contracts;

/// <summary>Respuesta del endpoint <c>domain_categories</c> de Socrata (DTO de transporte).</summary>
internal sealed class SocrataDomainCategoriesResponse
{
    [JsonPropertyName("results")]
    public List<SocrataDomainCategory> Results { get; init; } = [];
}
