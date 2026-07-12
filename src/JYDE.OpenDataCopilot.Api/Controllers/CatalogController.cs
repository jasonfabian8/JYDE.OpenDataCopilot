using JYDE.OpenDataCopilot.Api.Catalog;
using JYDE.OpenDataCopilot.Application.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace JYDE.OpenDataCopilot.Api.Controllers;

/// <summary>Endpoints HTTP del bounded context <c>Catalog</c>.</summary>
[ApiController]
[Route("catalog")]
public sealed class CatalogController : ControllerBase
{
    private readonly IngestCatalogService _ingestService;
    private readonly CatalogQueryService _queryService;
    private readonly ListCatalogCategoriesService _categoriesService;

    /// <summary>Crea el controlador del catálogo.</summary>
    /// <param name="ingestService">Caso de uso de ingesta.</param>
    /// <param name="queryService">Caso de uso de consulta (lectura).</param>
    /// <param name="categoriesService">Caso de uso de listado de categorías.</param>
    public CatalogController(
        IngestCatalogService ingestService,
        CatalogQueryService queryService,
        ListCatalogCategoriesService categoriesService)
    {
        ArgumentNullException.ThrowIfNull(ingestService);
        ArgumentNullException.ThrowIfNull(queryService);
        ArgumentNullException.ThrowIfNull(categoriesService);

        _ingestService = ingestService;
        _queryService = queryService;
        _categoriesService = categoriesService;
    }

    /// <summary>Ingiere el catálogo de datasets desde la fuente configurada.</summary>
    /// <param name="request">Filtro opcional (categorías, límite). Cuerpo vacío = catálogo completo.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Resumen con la cantidad de datasets ingeridos.</returns>
    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CatalogIngestRequest? request,
        CancellationToken cancellationToken)
    {
        // Saneamiento en la frontera: el request acota límite y categorías (defensa CWE-834).
        CatalogFilter filter = request?.ToFilter() ?? CatalogFilter.All;

        IngestCatalogResult result = await _ingestService.ExecuteAsync(filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>Devuelve la cantidad de datasets almacenados.</summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("count")]
    public async Task<IActionResult> Count(CancellationToken cancellationToken)
    {
        int count = await _queryService.CountAsync(cancellationToken);
        return Ok(new { count });
    }

    /// <summary>Lista las categorías temáticas del catálogo (con su conteo), para acotar la ingesta.</summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("categories")]
    public async Task<IActionResult> Categories(CancellationToken cancellationToken)
    {
        IReadOnlyList<CatalogCategory> categories = await _categoriesService.ExecuteAsync(cancellationToken);
        return Ok(categories);
    }

    /// <summary>Obtiene un dataset por su identificador 4x4.</summary>
    /// <param name="id">Identificador del dataset (formato <c>xxxx-xxxx</c>).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        DatasetDto? dataset;
        try
        {
            dataset = await _queryService.GetByIdAsync(id, cancellationToken);
        }
        catch (ArgumentException)
        {
            return BadRequest($"Identificador inválido: '{id}'. Debe tener el formato 4x4 (ej. 'ddau-8cy9').");
        }

        return dataset is null ? NotFound() : Ok(dataset);
    }
}
