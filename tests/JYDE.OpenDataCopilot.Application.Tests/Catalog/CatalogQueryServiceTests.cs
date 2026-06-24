using JYDE.OpenDataCopilot.Application.Catalog;
using JYDE.OpenDataCopilot.Domain.Catalog;
using Shouldly;

namespace JYDE.OpenDataCopilot.Application.Tests.Catalog;

/// <summary>Pruebas del caso de uso de lectura <see cref="CatalogQueryService"/>.</summary>
public sealed class CatalogQueryServiceTests
{
    [Fact]
    public async Task GetByIdAsync_CuandoExiste_DevuelveDtoMapeado()
    {
        InMemoryCatalogRepository repository = new();
        Dataset dataset = new(
            new DatasetId("ddau-8cy9"),
            "Accidentalidad",
            description: "Accidentes",
            category: "Movilidad",
            tags: ["movilidad"],
            columns: [new DatasetColumn("Municipio", "municipio", "text", "Nombre")],
            sourceUrl: new Uri("https://www.datos.gov.co/d/ddau-8cy9"));
        await repository.SaveAsync([dataset], TestContext.Current.CancellationToken);
        CatalogQueryService service = new(repository);

        DatasetDto? dto = await service.GetByIdAsync("ddau-8cy9", TestContext.Current.CancellationToken);

        dto.ShouldNotBeNull();
        dto.Id.ShouldBe("ddau-8cy9");
        dto.Description.ShouldBe("Accidentes");
        dto.Category.ShouldBe("Movilidad");
        dto.Tags.ShouldBe(["movilidad"]);
        dto.SourceUrl.ShouldBe("https://www.datos.gov.co/d/ddau-8cy9");
        DatasetColumnDto column = dto.Columns.ShouldHaveSingleItem();
        column.FieldName.ShouldBe("municipio");
        column.DataType.ShouldBe("text");
    }

    [Fact]
    public async Task GetByIdAsync_DatasetMinimo_MapeaConValoresNulos()
    {
        InMemoryCatalogRepository repository = new();
        await repository.SaveAsync([new Dataset(new DatasetId("aaaa-0001"), "Mínimo")], TestContext.Current.CancellationToken);
        CatalogQueryService service = new(repository);

        DatasetDto dto = (await service.GetByIdAsync("aaaa-0001", TestContext.Current.CancellationToken)).ShouldNotBeNull();

        dto.SourceUrl.ShouldBeNull();
        dto.Description.ShouldBeNull();
        dto.Columns.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_CuandoNoExiste_DevuelveNull()
    {
        CatalogQueryService service = new(new InMemoryCatalogRepository());

        (await service.GetByIdAsync("zzzz-9999", TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ConIdInvalido_LanzaArgumentException()
    {
        CatalogQueryService service = new(new InMemoryCatalogRepository());

        await Should.ThrowAsync<ArgumentException>(() => service.GetByIdAsync("no-valido"));
    }

    [Fact]
    public async Task CountAsync_DevuelveConteo()
    {
        InMemoryCatalogRepository repository = new();
        await repository.SaveAsync([new Dataset(new DatasetId("aaaa-0001"), "Uno")], TestContext.Current.CancellationToken);
        CatalogQueryService service = new(repository);

        (await service.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public void Constructor_ConRepositorioNulo_Lanza()
    {
        Should.Throw<ArgumentNullException>(() => new CatalogQueryService(null!));
    }
}
