using JYDE.OpenDataCopilot.Application.Search;
using Shouldly;

namespace JYDE.OpenDataCopilot.Application.Tests.Search;

/// <summary>Pruebas del caso de uso <see cref="SearchDatasetsService"/>.</summary>
public sealed class SearchDatasetsServiceTests
{
    [Fact]
    public async Task ExecuteAsync_DevuelveLosResultadosDelIndice_YEnviaElEmbeddingDeLaConsulta()
    {
        StubEmbeddingGenerator embeddings = new();
        CapturingSearchIndex index = new()
        {
            NextResults = [new DatasetSearchHit("aaaa-0001", "Uno", "Movilidad", null, 0.9)],
        };
        SearchDatasetsService service = new(embeddings, index);

        IReadOnlyList<DatasetSearchHit> hits = await service.ExecuteAsync("accidentalidad vial", topK: 3, cancellationToken: TestContext.Current.CancellationToken);

        hits.ShouldHaveSingleItem().Id.ShouldBe("aaaa-0001");
        embeddings.LastText.ShouldBe("accidentalidad vial");
        index.LastQuery.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_ConConsultaVacia_LanzaArgumentException(string query)
    {
        SearchDatasetsService service = new(new StubEmbeddingGenerator(), new CapturingSearchIndex());

        await Should.ThrowAsync<ArgumentException>(() => service.ExecuteAsync(query));
    }

    [Fact]
    public async Task ExecuteAsync_ConTopKInvalido_LanzaArgumentOutOfRange()
    {
        SearchDatasetsService service = new(new StubEmbeddingGenerator(), new CapturingSearchIndex());

        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => service.ExecuteAsync("x", topK: 0));
    }

    [Fact]
    public void Constructor_ConArgumentosNulos_Lanza()
    {
        Should.Throw<ArgumentNullException>(() => new SearchDatasetsService(null!, new CapturingSearchIndex()));
        Should.Throw<ArgumentNullException>(() => new SearchDatasetsService(new StubEmbeddingGenerator(), null!));
    }
}
