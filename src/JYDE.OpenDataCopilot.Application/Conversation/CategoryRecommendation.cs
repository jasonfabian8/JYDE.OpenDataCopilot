namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Categoría recomendada por el agente de categorías: una acción sugerida al usuario (cargarla si
/// aún no está en el catálogo). El frontend la muestra como botón.
/// </summary>
/// <param name="Name">Nombre de la categoría (p. ej. "Salud y Protección Social").</param>
/// <param name="Count">Datasets disponibles en la fuente para esa categoría.</param>
/// <param name="Loaded">Si la categoría ya está cargada en el catálogo.</param>
/// <param name="Relevance">Relevancia (0-1) para la necesidad del usuario.</param>
public sealed record CategoryRecommendation(string Name, int Count, bool Loaded, double Relevance);
