using JYDE.OpenDataCopilot.Application.Search;
using Microsoft.AspNetCore.Mvc;

namespace JYDE.OpenDataCopilot.Api.Controllers;

/// <summary>Endpoints HTTP del bounded context <c>Search</c> (descubrimiento de datasets).</summary>
[ApiController]
[Route("search")]
public sealed class SearchController : ControllerBase
{
    private readonly IndexCatalogService _indexService;
    private readonly SearchDatasetsService _searchService;

    /// <summary>Crea el controlador de búsqueda.</summary>
    /// <param name="indexService">Caso de uso de indexación del catálogo.</param>
    /// <param name="searchService">Caso de uso de búsqueda de datasets.</param>
    public SearchController(IndexCatalogService indexService, SearchDatasetsService searchService)
    {
        _indexService = indexService;
        _searchService = searchService;
    }

    /// <summary>Construye el índice de búsqueda a partir del catálogo almacenado.</summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Cantidad de datasets indexados.</returns>
    [HttpPost("index")]
    public async Task<IActionResult> BuildIndex(CancellationToken cancellationToken)
    {
        int indexed = await _indexService.ExecuteAsync(cancellationToken);
        return Ok(new { indexed });
    }

    /// <summary>Busca los datasets más relevantes para una consulta en lenguaje natural.</summary>
    /// <param name="q">Consulta en lenguaje natural.</param>
    /// <param name="top">Número máximo de resultados (por defecto 5).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] int top = SearchDatasetsService.DefaultTopK,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IReadOnlyList<DatasetSearchHit> hits = await _searchService.ExecuteAsync(q ?? string.Empty, top, cancellationToken);
            return Ok(hits);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
