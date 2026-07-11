namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>Sugerencia de gráfico del agente de cifras (parte del JSON de respuesta).</summary>
/// <param name="Tipo">Tipo de gráfico: <c>bar</c> o <c>line</c>.</param>
/// <param name="X">Columna del eje X (del resultado de la consulta).</param>
/// <param name="Y">Columna del eje Y (del resultado de la consulta).</param>
internal sealed record FiguresChart(string? Tipo, string? X, string? Y);
