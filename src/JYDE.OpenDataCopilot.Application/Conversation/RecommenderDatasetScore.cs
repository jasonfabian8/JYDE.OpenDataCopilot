namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>Relevancia (0-1) que el LLM recalcula para un candidato, identificado por su id.</summary>
/// <param name="Id">Identificador del dataset candidato.</param>
/// <param name="Relevancia">Relevancia estimada para la consulta (0.0 a 1.0).</param>
internal sealed record RecommenderDatasetScore(string? Id, double Relevancia);
