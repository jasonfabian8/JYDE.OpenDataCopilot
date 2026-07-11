namespace JYDE.OpenDataCopilot.Application.Figures;

/// <summary>
/// Puerto de salida: ejecuta una consulta de datos reales (SoQL) sobre un dataset de datos.gov.co y
/// devuelve el resultado tabular. Es de solo lectura (SODA). El adaptador acota el tamaño del resultado.
/// </summary>
public interface IDataQuery
{
    /// <summary>Ejecuta la consulta SoQL sobre el dataset y devuelve las filas.</summary>
    /// <param name="datasetId">Identificador 4x4 del dataset.</param>
    /// <param name="soql">Consulta SoQL (p. ej. <c>SELECT ... WHERE ... GROUP BY ... LIMIT ...</c>).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Columnas y filas del resultado.</returns>
    Task<DataQueryResult> QueryAsync(string datasetId, string soql, CancellationToken cancellationToken = default);
}
