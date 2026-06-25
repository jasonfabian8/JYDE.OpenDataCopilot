namespace JYDE.OpenDataCopilot.Infrastructure.Foundry;

/// <summary>
/// Opciones de Azure AI Foundry. <see cref="Endpoint"/> y <see cref="ApiKey"/> se comparten entre
/// chat (agentes) y embeddings; cada capacidad tiene su sub-configuración.
/// </summary>
public sealed class FoundryOptions
{
    /// <summary>Clave de configuración asociada a estas opciones.</summary>
    public const string SectionName = "Foundry";

    /// <summary>
    /// Endpoint del proyecto de Foundry
    /// (p. ej. <c>https://&lt;recurso&gt;.services.ai.azure.com/api/projects/&lt;proyecto&gt;</c>).
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Clave de API (secreto; se inyecta fuera del repositorio versionado).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Configuración del chat (modelo + catálogo de agentes).</summary>
    public FoundryChatSettings Chat { get; set; } = new();

    /// <summary>Configuración de embeddings.</summary>
    public FoundryEmbeddingSettings Embeddings { get; set; } = new();
}
