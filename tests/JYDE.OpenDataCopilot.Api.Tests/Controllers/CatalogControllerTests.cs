using JYDE.OpenDataCopilot.Api.Catalog;
using JYDE.OpenDataCopilot.Api.Controllers;
using JYDE.OpenDataCopilot.Api.Tests.Catalog;
using JYDE.OpenDataCopilot.Application.Catalog;
using JYDE.OpenDataCopilot.Domain.Catalog;
using JYDE.OpenDataCopilot.Infrastructure.Catalog;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace JYDE.OpenDataCopilot.Api.Tests.Controllers;

/// <summary>Pruebas unitarias del <see cref="CatalogController"/> (sin pipeline HTTP).</summary>
public sealed class CatalogControllerTests
{
    private static Dataset Sample(string id) => new(new DatasetId(id), $"Dataset {id}");

    private static (CatalogController Controller, InMemoryCatalogRepository Repository) Build(
        params Dataset[] sourceDatasets)
    {
        InMemoryCatalogRepository repository = new();
        IngestCatalogService ingest = new(new FakeCatalogSource(sourceDatasets), repository);
        CatalogQueryService query = new(repository);
        return (new CatalogController(ingest, query), repository);
    }

    private static int CountValue(IActionResult result)
    {
        OkObjectResult ok = result.ShouldBeOfType<OkObjectResult>();
        object value = ok.Value.ShouldNotBeNull();
        return (int)value.GetType().GetProperty("count")!.GetValue(value)!;
    }

    [Fact]
    public async Task Ingest_ConLimite_DevuelveOkConConteo()
    {
        (CatalogController controller, InMemoryCatalogRepository repository) =
            Build(Sample("aaaa-0001"), Sample("aaaa-0002"), Sample("aaaa-0003"));

        IActionResult result = await controller.Ingest(new CatalogIngestRequest(Limit: 2), CancellationToken.None);

        OkObjectResult ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBeOfType<IngestCatalogResult>().DatasetsIngested.ShouldBe(2);
        (await repository.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(2);
    }

    [Fact]
    public async Task Ingest_SinCuerpo_IngiereTodo()
    {
        (CatalogController controller, _) = Build(Sample("aaaa-0001"), Sample("aaaa-0002"));

        IActionResult result = await controller.Ingest(null, CancellationToken.None);

        result.ShouldBeOfType<OkObjectResult>().Value
            .ShouldBeOfType<IngestCatalogResult>().DatasetsIngested.ShouldBe(2);
    }

    [Fact]
    public async Task Count_DevuelveCantidadAlmacenada()
    {
        (CatalogController controller, InMemoryCatalogRepository repository) = Build();
        await repository.SaveAsync([Sample("aaaa-0001"), Sample("aaaa-0002")], TestContext.Current.CancellationToken);

        IActionResult result = await controller.Count(CancellationToken.None);

        CountValue(result).ShouldBe(2);
    }

    [Fact]
    public async Task GetById_ConIdInvalido_DevuelveBadRequest()
    {
        (CatalogController controller, _) = Build();

        IActionResult result = await controller.GetById("no-valido", CancellationToken.None);

        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetById_CuandoNoExiste_DevuelveNotFound()
    {
        (CatalogController controller, _) = Build();

        IActionResult result = await controller.GetById("zzzz-9999", CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_CuandoExiste_DevuelveOkConDto()
    {
        (CatalogController controller, InMemoryCatalogRepository repository) = Build();
        Dataset dataset = new(
            new DatasetId("ddau-8cy9"),
            "Accidentalidad",
            new DatasetMetadata(
                category: "Movilidad",
                tags: ["movilidad"],
                columns: [new DatasetColumn("Municipio", "municipio", "text", "Nombre")]));
        await repository.SaveAsync([dataset], TestContext.Current.CancellationToken);

        IActionResult result = await controller.GetById("ddau-8cy9", CancellationToken.None);

        OkObjectResult ok = result.ShouldBeOfType<OkObjectResult>();
        DatasetDto dto = ok.Value.ShouldBeOfType<DatasetDto>();
        dto.Id.ShouldBe("ddau-8cy9");
        dto.Category.ShouldBe("Movilidad");
        dto.Columns.ShouldHaveSingleItem().FieldName.ShouldBe("municipio");
    }
}
