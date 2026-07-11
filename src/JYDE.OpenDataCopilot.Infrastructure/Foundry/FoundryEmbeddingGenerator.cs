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
        Uri url = new(
            $"{_options.Endpoint.TrimEnd('/')}/openai/deployments/{_options.EmbeddingDeployment}/embeddings" +
            $"?api-version={_options.ApiVersion}");

        var payload = new { input = text, dimensions = _options.Dimensions };

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
