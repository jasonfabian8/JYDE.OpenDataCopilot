using JYDE.OpenDataCopilot.Application.Conversation;

namespace JYDE.OpenDataCopilot.Api.Conversation;

/// <summary>Cuerpo de una solicitud de chat al Copilot.</summary>
/// <param name="Question">Pregunta del usuario en lenguaje natural.</param>
/// <param name="Top">Número máximo de datasets relevantes a considerar (opcional).</param>
/// <param name="ConversationId">Id del turno anterior para continuar el hilo (nulo si es nuevo).</param>
/// <param name="Objective">Objetivo acumulado de la conversación (memoria); nulo/vacío si es nuevo.</param>
/// <param name="SelectedDatasets">Datasets (id + nombre) que el usuario mantiene fijados.</param>
/// <param name="Context">Respuesta anterior del Copilot, para desambiguar confirmaciones como "sí".</param>
public sealed record ChatRequest(
    string? Question,
    int? Top = null,
    string? ConversationId = null,
    string? Objective = null,
    IReadOnlyList<SelectedDataset>? SelectedDatasets = null,
    string? Context = null);
