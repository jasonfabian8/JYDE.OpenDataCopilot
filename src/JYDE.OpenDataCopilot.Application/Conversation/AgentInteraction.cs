namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>Una interacción cruda con un agente/LLM en un turno (para auditoría/entrenamiento).</summary>
/// <param name="Agent">Nombre del agente invocado (router, recomendador, cifras, etc.).</param>
/// <param name="Request">Mensaje enviado al agente (el input tal cual).</param>
/// <param name="Response">Respuesta cruda del agente (el texto tal cual).</param>
public sealed record AgentInteraction(string Agent, string Request, string Response);
