using System.Text.Json;
using JYDE.OpenDataCopilot.Application.Figures;

namespace JYDE.OpenDataCopilot.Infrastructure.Socrata;

/// <summary>
/// Adaptador de <see cref="IDataQuery"/> sobre la API SODA de Socrata
/// (<c>GET {dominio}/resource/{id}.json?$query=...</c>). Solo lectura; acota el número de filas del
/// resultado para no traer conjuntos enormes.
/// </summary>
public sealed class SocrataDataQuery : IDataQuery
{
    /// <summary>Máximo de filas que se conservan del resultado.</summary>
    public const int MaxRows = 200;

    private readonly HttpClient _httpClient;
    private readonly SocrataCatalogOptions _options;

    /// <summary>Crea el adaptador de consulta de datos de Socrata.</summary>
    /// <param name="httpClient">Cliente HTTP (inyectado).</param>
    /// <param name="options">Opciones del portal Socrata (dominio, app token).</param>
    /// <exception cref="ArgumentNullException">Si algún argumento es nulo.</exception>
    public SocrataDataQuery(HttpClient httpClient, SocrataCatalogOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        _httpClient = httpClient;
        _options = options;

        if (!string.IsNullOrWhiteSpace(options.AppToken))
        {
            _httpClient.DefaultRequestHeaders.Add("X-App-Token", options.AppToken);
        }
    }

    /// <inheritdoc />
    public async Task<DataQueryResult> QueryAsync(string datasetId, string soql, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(soql);

        Uri url = new(
            _options.BaseAddress,
            $"resource/{Uri.EscapeDataString(datasetId)}.json?$query={Uri.EscapeDataString(soql)}");
        HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        using JsonDocument document = JsonDocument.Parse(body);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return new DataQueryResult([], []);
        }

        List<string> columns = [];
        List<IReadOnlyList<string>> rows = [];
        foreach (JsonElement item in document.RootElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (columns.Count == 0)
            {
                foreach (JsonProperty property in item.EnumerateObject())
                {
                    columns.Add(property.Name);
                }
            }

            List<string> row = new(columns.Count);
            foreach (string column in columns)
            {
                row.Add(item.TryGetProperty(column, out JsonElement value) ? ToText(value) : string.Empty);
            }

            rows.Add(row);
            if (rows.Count >= MaxRows)
            {
                break;
            }
        }

        return new DataQueryResult(columns, rows);
    }

    private static string ToText(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => string.Empty,
        _ => value.GetRawText(),
    };
}
