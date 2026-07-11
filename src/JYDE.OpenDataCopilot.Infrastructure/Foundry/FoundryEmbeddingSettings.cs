namespace JYDE.OpenDataCopilot.Infrastructure.Foundry;

/// <summary>Configuración de embeddings de Foundry/Azure OpenAI.</summary>
public sealed class FoundryEmbeddingSettings
{
    /// <summary>Nombre del <i>deployment</i> de embeddings (p. ej. <c>text-embedding-3-small</c>).</summary>
    public string Deployment { get; set; } = "text-embedding-3-small";

    /// <summary>Versión de la API REST.</summary>
    public string ApiVersion { get; set; } = "2024-02-01";

    /// <summary>Dimensiones del embedding (debe coincidir con el índice vectorial).</summary>
    public int Dimensions { get; set; } = 1536;
}
