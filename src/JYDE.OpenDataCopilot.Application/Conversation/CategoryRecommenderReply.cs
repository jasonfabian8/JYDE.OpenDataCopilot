namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Respuesta estructurada (JSON) del agente de categorías: el texto para el usuario, la consulta a
/// reintentar tras cargar y la relevancia recalculada de cada categoría.
/// </summary>
/// <param name="Respuesta">Recomendación en lenguaje natural.</param>
/// <param name="Consulta">Consulta (tema) a reintentar una vez cargadas las categorías útiles.</param>
/// <param name="Categorias">Relevancia (0-1) que el LLM asigna a cada categoría.</param>
internal sealed record CategoryRecommenderReply(
    string? Respuesta,
    string? Consulta,
    IReadOnlyList<CategoryRelevance>? Categorias);
