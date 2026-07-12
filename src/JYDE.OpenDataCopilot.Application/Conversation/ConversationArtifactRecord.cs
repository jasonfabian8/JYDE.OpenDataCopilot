namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Artefacto persistido (tabla o gráfico) generado en una conversación. Lleva sus datos (columnas y
/// filas) para poder redibujarse al recuperar la conversación, sin volver a consultar la fuente.
/// </summary>
/// <param name="Id">Identificador estable del artefacto.</param>
/// <param name="Kind">Tipo: <c>table</c> o <c>chart</c>.</param>
/// <param name="Title">Título del artefacto.</param>
/// <param name="Columns">Columnas de los datos.</param>
/// <param name="Rows">Filas de los datos (cada fila, una lista de celdas de texto).</param>
/// <param name="Type">Tipo de gráfico (<c>bar</c>/<c>line</c>); nulo en tablas.</param>
/// <param name="XColumn">Columna del eje X (solo gráficos).</param>
/// <param name="YColumn">Columna del eje Y (solo gráficos).</param>
public sealed record ConversationArtifactRecord(
    string Id,
    string Kind,
    string Title,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    string? Type = null,
    string? XColumn = null,
    string? YColumn = null);
