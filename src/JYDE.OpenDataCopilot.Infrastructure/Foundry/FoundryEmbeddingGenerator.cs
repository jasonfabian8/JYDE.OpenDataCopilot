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
        // Los embeddings se exponen a nivel de RECURSO (no del proyecto): se deriva la raíz quitando
        // el sufijo "/api/projects/...". Se usa la API v1 (OpenAI-compatible).
        string resourceBase = _options.Endpoint;
        int projectIndex = resourceBase.IndexOf("/api/projects", StringComparison.OrdinalIgnoreCase);
        if (projectIndex >= 0)
        {
            resourceBase = resourceBase[..projectIndex];
        }

        Uri url = new($"{resourceBase.TrimEnd('/')}/openai/v1/embeddings");
        var payload = new
        {
            model = _options.Embeddings.Deployment,
            input = text,
            dimensions = _options.Embeddings.Dimensions,
        };

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        FoundryEmbeddingResponse? result =
            await response.Content.ReadFromJsonAsync<FoundryEmbeddingResponse>(cancellationToken);

        if (result is null || result.Data.Count == 0)
        {
            throw new InvalidOperationException("Foundry no devolvió embeddings para el texto solicitado.");
        }

        return result.Data[0].Embedding;
    }
}
