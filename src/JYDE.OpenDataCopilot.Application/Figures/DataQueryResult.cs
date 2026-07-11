namespace JYDE.OpenDataCopilot.Application.Figures;

/// <summary>Resultado tabular de una consulta de datos (SoQL) sobre un dataset.</summary>
/// <param name="Columns">Nombres de las columnas, en orden.</param>
/// <param name="Rows">Filas; cada una con un valor (texto) por columna, en el mismo orden.</param>
public sealed record DataQueryResult(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows);
