using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using JYDE.OpenDataCopilot.Application.Catalog;
using JYDE.OpenDataCopilot.Domain.Catalog;
using JYDE.OpenDataCopilot.Infrastructure.Socrata.Contracts;

namespace JYDE.OpenDataCopilot.Infrastructure.Socrata;

/// <summary>
/// Adaptador de <see cref="ICatalogSource"/> sobre la API de catálogo de Socrata
/// (<c>/api/catalog/v1</c>) de <c>datos.gov.co</c>. Pagina el catálogo y mapea los metadatos al
/// dominio. No expone tipos del transporte hacia las capas internas.
/// </summary>
public sealed class SocrataCatalogClient : ICatalogSource
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly SocrataCatalogOptions _options;

    /// <summary>Crea el cliente del catálogo de Socrata.</summary>
    /// <param name="httpClient">Cliente HTTP (inyectado).</param>
    /// <param name="options">Opciones del adaptador.</param>
    /// <exception cref="ArgumentNullException">Si algún argumento es nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Si <see cref="SocrataCatalogOptions.PageSize"/> es menor que 1.</exception>
    public SocrataCatalogClient(HttpClient httpClient, SocrataCatalogOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.PageSize, 1);

        _httpClient = httpClient;
        _options = options;

        if (!string.IsNullOrWhiteSpace(options.AppToken))
        {
            _httpClient.DefaultRequestHeaders.Add("X-App-Token", options.AppToken);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Dataset> FetchAsync(
        CatalogFilter filter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        int emitted = 0;
        int offset = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int remaining = filter.Limit.HasValue ? filter.Limit.Value - emitted : int.MaxValue;
            if (remaining <= 0)
            {
                yield break;
            }

            int pageLimit = Math.Min(_options.PageSize, remaining);
            Uri url = BuildRequestUri(filter, offset, pageLimit);

            SocrataCatalogResponse? response =
                await _httpClient.GetFromJsonAsync<SocrataCatalogResponse>(url, JsonOptions, cancellationToken);

            if (response is null || response.Results.Count == 0)
            {
                yield break;
            }

            foreach (SocrataResult result in response.Results)
            {
                Dataset? dataset = MapToDataset(result);
                if (dataset is null)
                {
                    continue;
                }

                yield return dataset;
                emitted++;

                if (filter.Limit.HasValue && emitted >= filter.Limit.Value)
                {
                    yield break;
                }
            }

            offset += response.Results.Count;
            if (offset >= response.ResultSetSize)
            {
                yield break;
            }
        }
    }

    private Uri BuildRequestUri(CatalogFilter filter, int offset, int limit)
    {
        List<string> query =
        [
            "only=dataset",
            $"offset={offset}",
            $"limit={limit}",
        ];

        if (filter.Categories is { Count: > 0 } categories)
        {
            foreach (string category in categories)
            {
                query.Add($"categories={Uri.EscapeDataString(category)}");
            }
        }

        return new Uri(_options.BaseAddress, $"api/catalog/v1?{string.Join('&', query)}");
    }

    private static Dataset? MapToDataset(SocrataResult result)
    {
        SocrataResource? resource = result.Resource;
        if (resource is null || string.IsNullOrWhiteSpace(resource.Id) || string.IsNullOrWhiteSpace(resource.Name))
        {
            return null;
        }

        DatasetId? id = TryCreateId(resource.Id);
        if (id is null)
        {
            return null;
        }

        return new Dataset(
            id,
            resource.Name,
            description: resource.Description,
            category: result.Classification?.DomainCategory,
            tags: result.Classification?.DomainTags,
            columns: MapColumns(resource),
            sourceUrl: TryCreateUri(result.Permalink ?? result.Link),
            updatedAt: TryParseDate(resource.UpdatedAt));
    }

    private static DatasetId? TryCreateId(string value)
    {
        try
        {
            return new DatasetId(value);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static IReadOnlyList<DatasetColumn> MapColumns(SocrataResource resource)
    {
        List<string>? names = resource.ColumnsName;
        if (names is null || names.Count == 0)
        {
            return [];
        }

        List<DatasetColumn> columns = new(names.Count);
        for (int i = 0; i < names.Count; i++)
        {
            columns.Add(new DatasetColumn(
                names[i],
                ValueAt(resource.ColumnsFieldName, i) ?? names[i],
                ValueAt(resource.ColumnsDataType, i) ?? "unknown",
                ValueAt(resource.ColumnsDescription, i)));
        }

        return columns;
    }

    private static string? ValueAt(List<string>? values, int index)
        => values is not null && index < values.Count ? values[index] : null;

    private static Uri? TryCreateUri(string? value)
        => Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) ? uri : null;

    private static DateTimeOffset? TryParseDate(string? value)
        => DateTimeOffset.TryParse(value, out DateTimeOffset parsed) ? parsed : null;
}
