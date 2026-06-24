using JYDE.OpenDataCopilot.Application.Search;
using JYDE.OpenDataCopilot.Application.Tests.Catalog;
using JYDE.OpenDataCopilot.Domain.Catalog;
using Shouldly;
using Xunit;

namespace JYDE.OpenDataCopilot.Application.Tests.Search;

/// <summary>Pruebas del caso de uso <see cref="IndexCatalogService"/>.</summary>
public sealed class IndexCatalogServiceTests
{
    private static Dataset Full(string id) => new(
        new DatasetId(id),
        $"Dataset {id}",
        description: "desc",
        category: "Movilidad",
        tags: ["t1"],
        columns: [new DatasetColumn("Col", "col", "text")]);

    [Fact]
    public async Task ExecuteAsync_IndexaTodosLosDatasets_YDevuelveConteo()
    {
        InMemoryCatalogRepository repository = new();
        await repository.SaveAsync([Full("aaaa-0001"), new Dataset(new DatasetId("aaaa-0002"), "Mínimo")], TestContext.Current.CancellationToken);
        StubEmbeddingGenerator embeddings = new();
        CapturingSearchIndex index = new();
        IndexCatalogService service = new(repository, embeddings, index);

        int indexed = await service.ExecuteAsync(TestContext.Current.CancellationToken);

        indexed.ShouldBe(2);
        index.Indexed.Count.ShouldBe(2);
        index.Indexed.Select(vector => vector.Id).ShouldBe(["aaaa-0001", "aaaa-0002"], ignoreOrder: true);
        index.Indexed.ShouldAllBe(vector => vector.Embedding.Count == 2);
    }

    [Fact]
    public async Task ExecuteAsync_IndexaPorLotes_SegunBatchSize()
    {
        InMemoryCatalogRepository repository = new();
        await repository.SaveAsync([Full("aaaa-0001"), Full("aaaa-0002"), Full("aaaa-0003")], TestContext.Current.CancellationToken);
        CapturingSearchIndex index = new();
        IndexCatalogService service = new(repository, new StubEmbeddingGenerator(), index, batchSize: 2);

        await service.ExecuteAsync(TestContext.Current.CancellationToken);

        index.IndexCallCount.ShouldBe(2); // 2 + 1
    }

    [Fact]
    public async Task ExecuteAsync_SinDatasets_NoIndexa()
    {
        CapturingSearchIndex index = new();
        IndexCatalogService service = new(new InMemoryCatalogRepository(), new StubEmbeddingGenerator(), index);

        int indexed = await service.ExecuteAsync(TestContext.Current.CancellationToken);

        indexed.ShouldBe(0);
        index.IndexCallCount.ShouldBe(0);
    }

    [Fact]
    public void Constructor_ConArgumentosInvalidos_Lanza()
    {
        InMemoryCatalogRepository repository = new();
        StubEmbeddingGenerator embeddings = new();
        CapturingSearchIndex index = new();

        Should.Throw<ArgumentNullException>(() => new IndexCatalogService(null!, embeddings, index));
        Should.Throw<ArgumentNullException>(() => new IndexCatalogService(repository, null!, index));
        Should.Throw<ArgumentNullException>(() => new IndexCatalogService(repository, embeddings, null!));
        Should.Throw<ArgumentOutOfRangeException>(
            () => new IndexCatalogService(repository, embeddings, index, batchSize: 0));
    }
}
