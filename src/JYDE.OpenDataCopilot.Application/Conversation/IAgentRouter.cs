namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Estrategia de enrutamiento del Copilot: elige qué agente atiende una pregunta. La estrategia es
/// intercambiable (por reglas para desarrollo; basada en LLM en producción).
/// </summary>
public interface IAgentRouter
{
    /// <summary>Selecciona el agente para la pregunta entre los disponibles.</summary>
    /// <param name="question">Pregunta del usuario.</param>
    /// <param name="agents">Agentes disponibles (no vacío).</param>
    /// <param name="context">Contexto reciente (p. ej. la respuesta anterior del Copilot) para desambiguar
    /// confirmaciones cortas como "sí"; puede ser nulo.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El agente seleccionado.</returns>
    Task<IConversationAgent> RouteAsync(
        string question,
        IReadOnlyList<IConversationAgent> agents,
        string? context = null,
        CancellationToken cancellationToken = default);
}
