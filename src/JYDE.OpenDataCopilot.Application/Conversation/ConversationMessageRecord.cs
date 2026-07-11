namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>Mensaje persistido de una conversación (turno del usuario o del asistente).</summary>
/// <param name="Id">Identificador estable del mensaje.</param>
/// <param name="Role">Rol del emisor: <c>user</c> o <c>assistant</c>.</param>
/// <param name="Content">Contenido textual del mensaje.</param>
/// <param name="Agent">Agente que respondió (solo en mensajes del asistente).</param>
/// <param name="Sources">Fuentes citadas (solo en mensajes del asistente).</param>
public sealed record ConversationMessageRecord(
    string Id,
    string Role,
    string Content,
    string? Agent = null,
    IReadOnlyList<Citation>? Sources = null);
