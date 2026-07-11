namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>Respuesta estructurada (JSON) del agente de cifras.</summary>
/// <param name="DatasetId">Id del dataset elegido para la consulta.</param>
/// <param name="Soql">Consulta SoQL a ejecutar.</param>
/// <param name="Explicacion">Explicación en lenguaje natural para el ciudadano.</param>
/// <param name="Chart">Sugerencia de gráfico (opcional).</param>
internal sealed record FiguresReply(string? DatasetId, string? Soql, string? Explicacion, FiguresChart? Chart);
