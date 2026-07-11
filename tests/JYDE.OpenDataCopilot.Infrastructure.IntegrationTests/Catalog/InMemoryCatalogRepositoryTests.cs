using JYDE.OpenDataCopilot.Domain.Catalog;
using JYDE.OpenDataCopilot.Infrastructure.Catalog;
using Shouldly;

namespace JYDE.OpenDataCopilot.Infrastructure.IntegrationTests.Catalog;

/// <summary>Pruebas del adaptador <see cref="InMemoryCatalogRepository"/>.</summary>
public sealed class InMemoryCatalogRepositoryTests
{
    private static Dataset Sample(string id) => new(new DatasetId(id), $"Dataset {id}");

    [Fact]
    public async Task SaveAsync_GuardaYCuenta()
    {
        InMemoryCatalogRepository repository = new();

        await repository.SaveAsync([Sample("aaaa-0001"), Sample("aaaa-0002")], TestContext.Current.CancellationToken);

        (await repository.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(2);
    }

    [Fact]
    public async Task SaveAsync_ConMismoId_Reemplaza()
    {
        InMemoryCatalogRepository repository = new();

        await repository.SaveAsync([Sample("aaaa-0001")], TestContext.Current.CancellationToken);
        await repository.SaveAsync([Sample("aaaa-0001")], TestContext.Current.CancellationToken);

        (await repository.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task GetByIdAsync_DevuelveElDatasetGuardado()
    {
        InMemoryCatalogRepository repository = new();
        await repository.SaveAsync([Sample("aaaa-0007")], TestContext.Current.CancellationToken);

        Dataset? found = await repository.GetByIdAsync(new DatasetId("aaaa-0007"), TestContext.Current.CancellationToken);

        found.ShouldNotBeNull();
        found.Id.Value.ShouldBe("aaaa-0007");
    }

    [Fact]
    public async Task GetByIdAsync_CuandoNoExiste_DevuelveNull()
    {
        InMemoryCatalogRepository repository = new();

        (await repository.GetByIdAsync(new DatasetId("zzzz-9999"), TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task GetAllAsync_DevuelveTodosLosDatasets()
    {
        InMemoryCatalogRepository repository = new();
        await repository.SaveAsync([Sample("aaaa-0001"), Sample("aaaa-0002")], TestContext.Current.CancellationToken);

        List<string> ids = [];
        await foreach (Dataset dataset in repository.GetAllAsync(TestContext.Current.CancellationToken))
        {
            ids.Add(dataset.Id.Value);
        }

        ids.ShouldBe(["aaaa-0001", "aaaa-0002"], ignoreOrder: true);
    }

    [Fact]
    public async Task GetLoadedCategoriesAsync_DevuelveCategoriasDistintas_OrdenadasSinNulas()
    {
        InMemoryCatalogRepository repository = new();
        await repository.SaveAsync(
            [
                new Dataset(new DatasetId("aaaa-0001"), "A", new DatasetMetadata(category: "Transporte")),
                new Dataset(new DatasetId("aaaa-0002"), "B", new DatasetMetadata(category: "Transporte")),
                new Dataset(new DatasetId("aaaa-0003"), "C", new DatasetMetadata(category: "Salud")),
                new Dataset(new DatasetId("aaaa-0004"), "D"),
            ],
            TestContext.Current.CancellationToken);

        IReadOnlyList<string> categories = await repository.GetLoadedCategoriesAsync(TestContext.Current.CancellationToken);

        categories.ShouldBe(["Salud", "Transporte"]);
    }

    [Fact]
    public async Task Metodos_ConArgumentosNulos_Lanzan()
    {
        InMemoryCatalogRepository repository = new();

        await Should.ThrowAsync<ArgumentNullException>(() => repository.SaveAsync(null!));
        await Should.ThrowAsync<ArgumentNullException>(() => repository.GetByIdAsync(null!));
    }
}
