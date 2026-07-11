using System.Text.Json.Serialization;

namespace JYDE.OpenDataCopilot.Infrastructure.Socrata.Contracts;

/// <summary>Respuesta del endpoint <c>/api/catalog/v1</c> de Socrata (DTO de transporte).</summary>
internal sealed record SocrataCatalogResponse
{
    [JsonPropertyName("results")]
    public List<SocrataResult> Results { get; init; } = [];

    [JsonPropertyName("resultSetSize")]
    public int ResultSetSize { get; init; }
}
