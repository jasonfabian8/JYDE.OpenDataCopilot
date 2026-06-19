namespace JYDE.OpenDataCopilot.Application.Catalog;

/// <summary>Resultado de una ejecución de ingesta del catálogo.</summary>
/// <param name="DatasetsIngested">Cantidad de datasets leídos de la fuente y persistidos.</param>
public sealed record IngestCatalogResult(int DatasetsIngested);
