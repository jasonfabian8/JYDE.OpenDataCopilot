using System.Globalization;
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
    /// <exception cref="ArgumentOutOfRangeException">Si <see cref="SocrataCatalogOptions.PageSize"/> o <see cref="SocrataCatalogOptions.MaxResults"/> son menores que 1.</exception>
    public SocrataCatalogClient(HttpClient httpClient, SocrataCatalogOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.PageSize, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxResults, 1);

        _httpClient = httpClient;
        _options = options;

        if (!string.IsNullOrWhiteSpace(options.AppToken))
        {
            _httpClient.DefaultRequestHeaders.Add("X-App-Token", options.AppToken);
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<Dataset> FetchAsync(CatalogFilter filter, CancellationToken cancellationToken = default)
    {
        // Validación temprana (eager); la iteración perezosa vive en el método iterador privado.
        ArgumentNullException.ThrowIfNull(filter);
        return FetchPagedAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CatalogCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        string domain = _options.BaseAddress.Host;
        Uri url = new(_options.BaseAddress, $"api/catalog/v1/domain_categories?domains={Uri.EscapeDataString(domain)}");

        SocrataDomainCategoriesResponse? response =
            await _httpClient.GetFromJsonAsync<SocrataDomainCategoriesResponse>(url, JsonOptions, cancellationToken);
        if (response is null)
        {
            return [];
        }

        return
        [
            .. response.Results
                .Where(item => !string.IsNullOrWhiteSpace(item.DomainCategory))
                .Select(item => new CatalogCategory(item.DomainCategory!, item.Count))
                .OrderByDescending(category => category.Count)
        ];
    }

    /// <summary>Itera el catálogo paginando la API de Socrata hasta agotar resultados o el límite.</summary>
    private async IAsyncEnumerable<Dataset> FetchPagedAsync(
        CatalogFilter filter,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Acota el límite pedido a una cota de servidor (MaxResults): el bucle no debe quedar
        // gobernado por datos del usuario (CWE-834). Sin límite explícito, la paginación termina
        // cuando la fuente se agota (offset >= ResultSetSize).
        int? limit = filter.Limit is int requested ? Math.Min(requested, _options.MaxResults) : null;
        int emitted = 0;
        int offset = 0;

        while (limit is null || emitted < limit)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int pageLimit = limit is null ? _options.PageSize : Math.Min(_options.PageSize, limit.Value - emitted);
            SocrataCatalogResponse? response = await FetchPageAsync(filter, offset, pageLimit, cancellationToken);
            if (response is null || response.Results.Count == 0)
            {
                yield break;
            }

            foreach (Dataset dataset in MapPage(response.Results))
            {
                yield return dataset;
                if (++emitted == limit)
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

    /// <summary>Solicita una página del catálogo a Socrata.</summary>
    private Task<SocrataCatalogResponse?> FetchPageAsync(
        CatalogFilter filter,
        int offset,
        int limit,
        CancellationToken cancellationToken)
    {
        Uri url = BuildRequestUri(filter, offset, limit);
        return _httpClient.GetFromJsonAsync<SocrataCatalogResponse>(url, JsonOptions, cancellationToken);
    }

    /// <summary>Mapea los resultados de una página al dominio, descartando los inválidos.</summary>
    private static IEnumerable<Dataset> MapPage(IReadOnlyList<SocrataResult> results)
    {
        foreach (SocrataResult result in results)
        {
            Dataset? dataset = MapToDataset(result);
            if (dataset is not null)
            {
                yield return dataset;
            }
        }
    }

    private Uri BuildRequestUri(CatalogFilter filter, int offset, int limit)
    {
        // Acota la búsqueda al dominio configurado (p. ej. www.datos.gov.co); sin esto, la API de
        // catálogo de Socrata federa datasets de toda su red (NYC, Chicago, etc.).
        string domain = _options.BaseAddress.Host;

        List<string> query =
        [
            "only=dataset",
            $"domains={Uri.EscapeDataString(domain)}",
            $"search_context={Uri.EscapeDataString(domain)}",
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
            new DatasetMetadata(
                description: resource.Description,
                category: result.Classification?.DomainCategory,
                tags: result.Classification?.DomainTags,
                columns: MapColumns(resource),
                sourceUrl: TryCreateUri(result.Permalink ?? result.Link),
                updatedAt: TryParseDate(resource.UpdatedAt)));
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

    private static List<DatasetColumn> MapColumns(SocrataResource resource)
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
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset parsed)
            ? parsed
            : null;
}
