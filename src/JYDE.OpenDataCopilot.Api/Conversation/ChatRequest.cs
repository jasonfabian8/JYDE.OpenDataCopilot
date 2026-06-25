namespace JYDE.OpenDataCopilot.Api.Conversation;

/// <summary>Cuerpo de una solicitud de chat al Copilot.</summary>
/// <param name="Question">Pregunta del usuario en lenguaje natural.</param>
/// <param name="Top">Número máximo de datasets relevantes a considerar (opcional).</param>
/// <param name="ConversationId">Id del turno anterior para continuar el hilo (nulo si es nuevo).</param>
public sealed record ChatRequest(string? Question, int? Top = null, string? ConversationId = null);
