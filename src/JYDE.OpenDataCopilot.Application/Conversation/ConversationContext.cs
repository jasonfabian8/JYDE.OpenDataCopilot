namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>Contexto de una consulta del usuario que se pasa al agente seleccionado.</summary>
/// <param name="Question">Pregunta del usuario en lenguaje natural.</param>
/// <param name="TopK">Número máximo de datasets relevantes a considerar.</param>
/// <param name="PreviousResponseId">Identificador del turno anterior para continuar el hilo (nulo si es nuevo).</param>
public sealed record ConversationContext(string Question, int TopK, string? PreviousResponseId = null);
