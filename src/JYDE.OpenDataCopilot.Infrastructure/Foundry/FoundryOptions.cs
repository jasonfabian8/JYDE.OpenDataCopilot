namespace JYDE.OpenDataCopilot.Infrastructure.Foundry;

/// <summary>Opciones de Azure AI Foundry / Azure OpenAI para la generación de embeddings.</summary>
public sealed class FoundryOptions
{
    /// <summary>Clave de configuración asociada a estas opciones.</summary>
    public const string SectionName = "Foundry";

    /// <summary>Endpoint del recurso (p. ej. <c>https://mi-recurso.openai.azure.com</c>).</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Clave de API (secreto; se inyecta fuera del repositorio versionado).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Nombre del <i>deployment</i> de embeddings (p. ej. <c>text-embedding-3-small</c>).</summary>
    public string EmbeddingDeployment { get; set; } = "text-embedding-3-small";

    /// <summary>Versión de la API REST.</summary>
    public string ApiVersion { get; set; } = "2024-02-01";

    /// <summary>Dimensiones del embedding (debe coincidir con el índice vectorial).</summary>
    public int Dimensions { get; set; } = 1536;
}
