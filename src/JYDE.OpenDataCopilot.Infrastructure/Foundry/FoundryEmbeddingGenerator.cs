using System.Net.Http.Json;
using JYDE.OpenDataCopilot.Application.Search;
using JYDE.OpenDataCopilot.Infrastructure.Foundry.Contracts;

namespace JYDE.OpenDataCopilot.Infrastructure.Foundry;

/// <summary>
/// Adaptador de <see cref="IEmbeddingGenerator"/> sobre Azure AI Foundry / Azure OpenAI (REST).
/// Genera embeddings reales (p. ej. <c>text-embedding-3-small</c>). No expone tipos de transporte.
/// </summary>
public sealed class FoundryEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly HttpClient _httpClient;
    private readonly FoundryOptions _options;

    /// <summary>Crea el generador de embeddings de Foundry.</summary>
    /// <param name="httpClient">Cliente HTTP (inyectado).</param>
    /// <param name="options">Opciones de Foundry (endpoint, clave, deployment).</param>
    /// <exception cref="ArgumentNullException">Si algún argumento es nulo.</exception>
    public FoundryEmbeddingGenerator(HttpClient httpClient, FoundryOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        _httpClient = httpClient;
        _options = options;

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("api-key", options.ApiKey);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<float>> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IReadOnlyList<float>> embeddings = await GenerateBatchAsync([text], cancellationToken);
        return embeddings[0];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IReadOnlyList<float>>> GenerateBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texts);
        if (texts.Count == 0)
        {
            return [];
        }

        Uri url = new($"{ResourceBase()}/openai/v1/embeddings");
        var payload = new
        {
            model = _options.Embeddings.Deployment,
            input = texts,
            dimensions = _options.Embeddings.Dimensions,
        };

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        FoundryEmbeddingResponse? result =
            await response.Content.ReadFromJsonAsync<FoundryEmbeddingResponse>(cancellationToken);

        if (result is null || result.Data.Count == 0)
        {
            throw new InvalidOperationException("Foundry no devolvió embeddings para los textos solicitados.");
        }

        // Se conserva el orden de la entrada usando el índice que devuelve la API.
        return [.. result.Data.OrderBy(item => item.Index).Select(item => (IReadOnlyList<float>)item.Embedding)];
    }

    /// <summary>
    /// Deriva la raíz del RECURSO (los embeddings viven a nivel de recurso, no de proyecto): quita el
    /// sufijo "/api/projects/...". Se usa la API v1 (OpenAI-compatible).
    /// </summary>
    private string ResourceBase()
    {
        string resourceBase = _options.Endpoint;
        int projectIndex = resourceBase.IndexOf("/api/projects", StringComparison.OrdinalIgnoreCase);
        if (projectIndex >= 0)
        {
            resourceBase = resourceBase[..projectIndex];
        }

        return resourceBase.TrimEnd('/');
    }
}
