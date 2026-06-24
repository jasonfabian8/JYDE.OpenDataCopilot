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

        await repository.SaveAsync([Sample("aaaa-0001"), Sample("aaaa-0002")]);

        (await repository.CountAsync()).ShouldBe(2);
    }

    [Fact]
    public async Task SaveAsync_ConMismoId_Reemplaza()
    {
        InMemoryCatalogRepository repository = new();

        await repository.SaveAsync([Sample("aaaa-0001")]);
        await repository.SaveAsync([Sample("aaaa-0001")]);

        (await repository.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task GetByIdAsync_DevuelveElDatasetGuardado()
    {
        InMemoryCatalogRepository repository = new();
        await repository.SaveAsync([Sample("aaaa-0007")]);

        Dataset? found = await repository.GetByIdAsync(new DatasetId("aaaa-0007"));

        found.ShouldNotBeNull();
        found.Id.Value.ShouldBe("aaaa-0007");
    }

    [Fact]
    public async Task GetByIdAsync_CuandoNoExiste_DevuelveNull()
    {
        InMemoryCatalogRepository repository = new();

        (await repository.GetByIdAsync(new DatasetId("zzzz-9999"))).ShouldBeNull();
    }

    [Fact]
    public async Task GetAllAsync_DevuelveTodosLosDatasets()
    {
        InMemoryCatalogRepository repository = new();
        await repository.SaveAsync([Sample("aaaa-0001"), Sample("aaaa-0002")]);

        List<string> ids = [];
        await foreach (Dataset dataset in repository.GetAllAsync())
        {
            ids.Add(dataset.Id.Value);
        }

        ids.ShouldBe(["aaaa-0001", "aaaa-0002"], ignoreOrder: true);
    }

    [Fact]
    public async Task Metodos_ConArgumentosNulos_Lanzan()
    {
        InMemoryCatalogRepository repository = new();

        await Should.ThrowAsync<ArgumentNullException>(() => repository.SaveAsync(null!));
        await Should.ThrowAsync<ArgumentNullException>(() => repository.GetByIdAsync(null!));
    }
}
