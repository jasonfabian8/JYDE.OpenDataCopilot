using JYDE.OpenDataCopilot.Api.Controllers;
using JYDE.OpenDataCopilot.Application.Search;
using JYDE.OpenDataCopilot.Domain.Catalog;
using JYDE.OpenDataCopilot.Infrastructure.Catalog;
using JYDE.OpenDataCopilot.Infrastructure.Embeddings;
using JYDE.OpenDataCopilot.Infrastructure.Search;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using Xunit;

namespace JYDE.OpenDataCopilot.Api.Tests.Controllers;

/// <summary>Pruebas unitarias del <see cref="SearchController"/> con adaptadores locales reales.</summary>
public sealed class SearchControllerTests
{
    private static async Task<SearchController> BuildIndexedAsync()
    {
        InMemoryCatalogRepository repository = new();
        await repository.SaveAsync(
        [
            new Dataset(new DatasetId("aaaa-0001"), "Accidentalidad vial", category: "Movilidad",
                tags: ["accidentes", "vias", "transito"]),
            new Dataset(new DatasetId("aaaa-0002"), "Cobertura de vacunación", category: "Salud",
                tags: ["vacunacion", "salud"]),
        ]);

        LocalHashingEmbeddingGenerator embeddings = new();
        InMemorySearchIndex index = new();
        IndexCatalogService indexService = new(repository, embeddings, index);
        SearchDatasetsService searchService = new(embeddings, index);

        await indexService.ExecuteAsync();
        return new SearchController(indexService, searchService);
    }

    [Fact]
    public async Task BuildIndex_DevuelveOkConConteo()
    {
        SearchController controller = await BuildIndexedAsync();

        IActionResult result = await controller.BuildIndex(CancellationToken.None);

        OkObjectResult ok = result.ShouldBeOfType<OkObjectResult>();
        object value = ok.Value.ShouldNotBeNull();
        ((int)value.GetType().GetProperty("indexed")!.GetValue(value)!).ShouldBe(2);
    }

    [Fact]
    public async Task Search_DevuelveDatasetsRelevantesPrimero()
    {
        SearchController controller = await BuildIndexedAsync();

        IActionResult result = await controller.Search("accidentes en vias", top: 2, CancellationToken.None);

        OkObjectResult ok = result.ShouldBeOfType<OkObjectResult>();
        IReadOnlyList<DatasetSearchHit> hits = ok.Value.ShouldBeAssignableTo<IReadOnlyList<DatasetSearchHit>>()!;
        hits.ShouldNotBeEmpty();
        hits[0].Id.ShouldBe("aaaa-0001");
    }

    [Fact]
    public async Task Search_ConConsultaVacia_DevuelveBadRequest()
    {
        SearchController controller = await BuildIndexedAsync();

        IActionResult result = await controller.Search(q: "  ", top: 5, CancellationToken.None);

        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Search_ConConsultaNula_DevuelveBadRequest()
    {
        SearchController controller = await BuildIndexedAsync();

        IActionResult result = await controller.Search(q: null, top: 5, CancellationToken.None);

        result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
