namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>Artefacto de tabla: resultado tabular que el frontend muestra al lado (panel de artefactos).</summary>
/// <param name="Title">Título de la tabla (p. ej. el nombre del dataset o de la consulta).</param>
/// <param name="Columns">Nombres de las columnas.</param>
/// <param name="Rows">Filas; un valor (texto) por columna.</param>
public sealed record TableArtifact(
    string Title,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows);
