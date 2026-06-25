namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Estrategia de enrutamiento del Copilot: elige qué agente atiende una pregunta. La estrategia es
/// intercambiable (determinista ahora; basada en LLM/function-calling cuando haya más agentes).
/// </summary>
public interface IAgentRouter
{
    /// <summary>Selecciona el agente para la pregunta entre los disponibles.</summary>
    /// <param name="question">Pregunta del usuario.</param>
    /// <param name="agents">Agentes disponibles (no vacío).</param>
    /// <returns>El agente seleccionado.</returns>
    IConversationAgent Route(string question, IReadOnlyList<IConversationAgent> agents);
}
