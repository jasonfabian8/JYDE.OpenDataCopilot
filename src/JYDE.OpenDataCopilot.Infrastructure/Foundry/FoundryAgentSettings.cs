namespace JYDE.OpenDataCopilot.Infrastructure.Foundry;

/// <summary>Configuración de un agente publicado en Azure AI Foundry (referenciado por nombre+versión).</summary>
public sealed class FoundryAgentSettings
{
    /// <summary>Nombre del agente tal como está publicado en Foundry (puede diferir del código lógico).</summary>
    public string? Name { get; set; }

    /// <summary>Versión del agente en Foundry.</summary>
    public string? Version { get; set; }

    /// <summary>Modelo específico del agente (override del modelo de chat por defecto).</summary>
    public string? Model { get; set; }
}
