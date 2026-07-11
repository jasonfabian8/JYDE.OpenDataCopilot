namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>Entrada de auditoría persistida: el mensaje del usuario y las interacciones de los agentes.</summary>
/// <param name="Id">Identificador estable de la entrada (un turno).</param>
/// <param name="UserMessage">Mensaje del usuario que originó el turno.</param>
/// <param name="Interactions">Interacciones crudas con los agentes de ese turno.</param>
public sealed record ConversationAuditEntryRecord(
    string Id,
    string UserMessage,
    IReadOnlyList<AgentInteraction> Interactions);
