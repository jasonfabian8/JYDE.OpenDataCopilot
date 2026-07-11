namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>Relevancia (0-1) que el LLM asigna a una categoría del catálogo.</summary>
/// <param name="Nombre">Nombre de la categoría.</param>
/// <param name="Relevancia">Relevancia estimada para la necesidad del usuario (0.0 a 1.0).</param>
internal sealed record CategoryRelevance(string? Nombre, double Relevancia);
