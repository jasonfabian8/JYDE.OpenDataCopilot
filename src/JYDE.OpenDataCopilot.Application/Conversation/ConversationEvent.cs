namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>Evento del flujo de conversación (se serializa como evento SSE).</summary>
/// <param name="Kind">Tipo de evento.</param>
/// <param name="Agent">Nombre del agente (en eventos <see cref="ConversationEventKind.Agent"/>).</param>
/// <param name="Token">Fragmento de texto (en eventos <see cref="ConversationEventKind.Token"/>).</param>
/// <param name="Sources">Fuentes citadas (en eventos <see cref="ConversationEventKind.Sources"/>).</param>
/// <param name="ConversationId">Id del hilo (en eventos <see cref="ConversationEventKind.Conversation"/>).</param>
public sealed record ConversationEvent(
    ConversationEventKind Kind,
    string? Agent = null,
    string? Token = null,
    IReadOnlyList<Citation>? Sources = null,
    string? ConversationId = null)
{
    /// <summary>Crea un evento que anuncia el agente que atiende.</summary>
    public static ConversationEvent ForAgent(string agent) => new(ConversationEventKind.Agent, Agent: agent);

    /// <summary>Crea un evento con las fuentes citadas.</summary>
    public static ConversationEvent ForSources(IReadOnlyList<Citation> sources) =>
        new(ConversationEventKind.Sources, Sources: sources);

    /// <summary>Crea un evento con un fragmento de texto.</summary>
    public static ConversationEvent ForToken(string token) => new(ConversationEventKind.Token, Token: token);

    /// <summary>Crea un evento con el id del hilo de conversación.</summary>
    public static ConversationEvent ForConversation(string conversationId) =>
        new(ConversationEventKind.Conversation, ConversationId: conversationId);

    /// <summary>Evento de fin de respuesta.</summary>
    public static ConversationEvent Completed() => new(ConversationEventKind.Done);
}
