using System.Text.Json.Serialization;

namespace JYDE.OpenDataCopilot.Infrastructure.Socrata.Contracts;

/// <summary>Sección <c>resource</c> de un resultado del catálogo de Socrata (DTO de transporte).</summary>
internal sealed record SocrataResource
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; init; }

    [JsonPropertyName("columns_name")]
    public List<string>? ColumnsName { get; init; }

    [JsonPropertyName("columns_field_name")]
    public List<string>? ColumnsFieldName { get; init; }

    [JsonPropertyName("columns_datatype")]
    public List<string>? ColumnsDataType { get; init; }

    [JsonPropertyName("columns_description")]
    public List<string>? ColumnsDescription { get; init; }
}
