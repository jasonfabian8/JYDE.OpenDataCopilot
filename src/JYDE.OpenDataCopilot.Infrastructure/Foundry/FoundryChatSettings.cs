namespace JYDE.OpenDataCopilot.Infrastructure.Foundry;

/// <summary>Configuración del chat de Foundry: modelo por defecto y catálogo de agentes.</summary>
public sealed class FoundryChatSettings
{
    /// <summary>Modelo por defecto (p. ej. <c>gpt-4o-mini</c>).</summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>Catálogo de agentes por código lógico (clave) → configuración en Foundry.</summary>
    public Dictionary<string, FoundryAgentSettings> Agents { get; set; } = [];
}
