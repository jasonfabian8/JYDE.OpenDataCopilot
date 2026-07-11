namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Respuesta estructurada (JSON) del LLM recomendador: el texto para el ciudadano y la relevancia
/// recalculada de cada candidato. El agente la usa para decidir qué datasets citar.
/// </summary>
/// <param name="Respuesta">Recomendación en lenguaje natural para el ciudadano.</param>
/// <param name="Datasets">Relevancia (0-1) que el LLM asigna a cada candidato.</param>
internal sealed record RecommenderReply(string? Respuesta, IReadOnlyList<RecommenderDatasetScore>? Datasets);
