using JYDE.OpenDataCopilot.Application.Search;
using JYDE.OpenDataCopilot.Infrastructure.Search;
using Shouldly;
using Xunit;

namespace JYDE.OpenDataCopilot.Infrastructure.IntegrationTests.Search;

/// <summary>Pruebas del adaptador <see cref="InMemorySearchIndex"/>.</summary>
public sealed class InMemorySearchIndexTests
{
    private static DatasetVector Vector(string id, params float[] embedding) =>
        new(id, $"Dataset {id}", "Cat", null, embedding);

    [Fact]
    public async Task SearchAsync_OrdenaPorSimilitud_YRespetaTopK()
    {
        InMemorySearchIndex index = new();
        await index.IndexAsync(
        [
            Vector("aaaa-0001", 1f, 0f),   // idéntico a la consulta
            Vector("aaaa-0002", 0f, 1f),   // ortogonal
            Vector("aaaa-0003", 0.9f, 0.1f), // parecido
        ], TestContext.Current.CancellationToken);

        IReadOnlyList<DatasetSearchHit> hits = await index.SearchAsync([1f, 0f], topK: 2, cancellationToken: TestContext.Current.CancellationToken);

        hits.Count.ShouldBe(2);
        hits[0].Id.ShouldBe("aaaa-0001");
        hits[0].Score.ShouldBeGreaterThan(hits[1].Score);
    }

    [Fact]
    public async Task IndexAsync_ConMismoId_Reemplaza()
    {
        InMemorySearchIndex index = new();
        await index.IndexAsync([Vector("aaaa-0001", 1f, 0f)], TestContext.Current.CancellationToken);
        await index.IndexAsync([Vector("aaaa-0001", 0f, 1f)], TestContext.Current.CancellationToken);

        IReadOnlyList<DatasetSearchHit> hits = await index.SearchAsync([0f, 1f], topK: 5, cancellationToken: TestContext.Current.CancellationToken);

        hits.ShouldHaveSingleItem().Score.ShouldBeGreaterThan(0.99);
    }

    [Fact]
    public async Task SearchAsync_ConIndiceVacio_DevuelveVacio()
    {
        InMemorySearchIndex index = new();

        (await index.SearchAsync([1f, 0f], topK: 5, cancellationToken: TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_ConLongitudDistintaOVectorCero_PuntajeCero()
    {
        InMemorySearchIndex index = new();
        await index.IndexAsync(
        [
            Vector("aaaa-0001", 1f, 0f, 0f), // longitud distinta a la consulta (2)
            Vector("aaaa-0002", 0f, 0f),     // vector cero
        ], TestContext.Current.CancellationToken);

        IReadOnlyList<DatasetSearchHit> hits = await index.SearchAsync([1f, 1f], topK: 5, cancellationToken: TestContext.Current.CancellationToken);

        hits.ShouldAllBe(hit => hit.Score == 0d);
    }

    [Fact]
    public async Task Metodos_ConArgumentosInvalidos_Lanzan()
    {
        InMemorySearchIndex index = new();

        await Should.ThrowAsync<ArgumentNullException>(() => index.IndexAsync(null!));
        await Should.ThrowAsync<ArgumentNullException>(() => index.SearchAsync(null!, 5));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => index.SearchAsync([1f], 0));
    }
}
