using JYDE.OpenDataCopilot.Application.Catalog;
using JYDE.OpenDataCopilot.Domain.Catalog;
using Shouldly;

namespace JYDE.OpenDataCopilot.Application.Tests.Catalog;

/// <summary>Pruebas del caso de uso <see cref="IngestCatalogService"/>.</summary>
public sealed class IngestCatalogServiceTests
{
    private static Dataset Sample(string id) => new(new DatasetId(id), $"Dataset {id}");

    private static IReadOnlyList<Dataset> Samples(params string[] ids)
        => ids.Select(Sample).ToList();

    [Fact]
    public async Task ExecuteAsync_PersisteTodosLosDatasets_YDevuelveConteo()
    {
        FakeCatalogSource source = new(Samples("aaaa-0001", "aaaa-0002", "aaaa-0003"));
        InMemoryCatalogRepository repository = new();
        IngestCatalogService service = new(source, repository);

        IngestCatalogResult result = await service.ExecuteAsync(CatalogFilter.All);

        result.DatasetsIngested.ShouldBe(3);
        (await repository.CountAsync()).ShouldBe(3);
    }

    [Fact]
    public async Task ExecuteAsync_GuardaPorLotes_SegunBatchSize()
    {
        FakeCatalogSource source = new(Samples("aaaa-0001", "aaaa-0002", "aaaa-0003", "aaaa-0004", "aaaa-0005"));
        InMemoryCatalogRepository repository = new();
        IngestCatalogService service = new(source, repository, batchSize: 2);

        await service.ExecuteAsync(CatalogFilter.All);

        // 5 datasets en lotes de 2 => 3 llamadas a SaveAsync (2 + 2 + 1).
        repository.SaveCallCount.ShouldBe(3);
        (await repository.CountAsync()).ShouldBe(5);
    }

    [Fact]
    public async Task ExecuteAsync_SinDatasets_NoLlamaSave_YDevuelveCero()
    {
        FakeCatalogSource source = new([]);
        InMemoryCatalogRepository repository = new();
        IngestCatalogService service = new(source, repository);

        IngestCatalogResult result = await service.ExecuteAsync(CatalogFilter.All);

        result.DatasetsIngested.ShouldBe(0);
        repository.SaveCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_PropagaElFiltroALaFuente()
    {
        FakeCatalogSource source = new(Samples("aaaa-0001"));
        InMemoryCatalogRepository repository = new();
        IngestCatalogService service = new(source, repository);
        CatalogFilter filter = new(["Movilidad"], Limit: 10);

        await service.ExecuteAsync(filter);

        source.LastFilter.ShouldBe(filter);
    }

    [Fact]
    public void Constructor_ConArgumentosNulos_LanzaArgumentNullException()
    {
        InMemoryCatalogRepository repository = new();

        Should.Throw<ArgumentNullException>(() => new IngestCatalogService(null!, repository));
        Should.Throw<ArgumentNullException>(() => new IngestCatalogService(new FakeCatalogSource([]), null!));
    }

    [Fact]
    public void Constructor_ConBatchSizeInvalido_LanzaArgumentOutOfRange()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => new IngestCatalogService(new FakeCatalogSource([]), new InMemoryCatalogRepository(), batchSize: 0));
    }
}
