namespace JYDE.OpenDataCopilot.Api.Catalog;

/// <summary>Cuerpo opcional para solicitar una ingesta del catálogo.</summary>
/// <param name="Categories">Categorías a incluir; nula o vacía = todas.</param>
/// <param name="Limit">Máximo de datasets a ingerir; nulo = sin límite.</param>
public sealed record CatalogIngestRequest(IReadOnlyList<string>? Categories = null, int? Limit = null);
