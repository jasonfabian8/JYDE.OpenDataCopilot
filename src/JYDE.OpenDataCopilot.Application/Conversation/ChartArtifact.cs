namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Artefacto de gráfico: el frontend lo dibuja (SVG) a partir de la tabla emitida en el mismo turno,
/// usando las columnas indicadas para los ejes.
/// </summary>
/// <param name="Title">Título del gráfico.</param>
/// <param name="Type">Tipo de gráfico: <c>bar</c> o <c>line</c>.</param>
/// <param name="XColumn">Columna para el eje X (categorías/etiquetas).</param>
/// <param name="YColumn">Columna para el eje Y (valor numérico).</param>
public sealed record ChartArtifact(string Title, string Type, string XColumn, string YColumn);
