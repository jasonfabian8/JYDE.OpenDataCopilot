using JYDE.OpenDataCopilot.Infrastructure.Foundry;
using Shouldly;
using Xunit;

namespace JYDE.OpenDataCopilot.Infrastructure.IntegrationTests.Foundry;

/// <summary>Pruebas del adaptador <see cref="FoundryEmbeddingGenerator"/> con un handler HTTP falso.</summary>
public sealed class FoundryEmbeddingGeneratorTests
{
    private static FoundryOptions Options(string apiKey = "secret-key") => new()
    {
        Endpoint = "https://recurso.openai.azure.com",
        ApiKey = apiKey,
        EmbeddingDeployment = "text-embedding-3-small",
        ApiVersion = "2024-02-01",
        Dimensions = 3,
    };

    private static FoundryEmbeddingGenerator Create(FakeFoundryHandler handler, FoundryOptions options) =>
        new(new HttpClient(handler), options);

    [Fact]
    public async Task GenerateAsync_DevuelveElEmbedding_YLlamaElEndpointCorrecto()
    {
        FakeFoundryHandler handler = new("""{"data":[{"embedding":[0.1,0.2,0.3]}]}""");
        FoundryEmbeddingGenerator generator = Create(handler, Options());

        IReadOnlyList<float> embedding = await generator.GenerateAsync("accidentalidad vial", TestContext.Current.CancellationToken);

        embedding.Count.ShouldBe(3);
        embedding[0].ShouldBe(0.1f, 1e-5f);
        handler.LastApiKey.ShouldBe("secret-key");
        handler.LastUri!.AbsolutePath.ShouldBe("/openai/deployments/text-embedding-3-small/embeddings");
        handler.LastUri!.Query.ShouldContain("api-version=2024-02-01");
    }

    [Fact]
    public async Task GenerateAsync_SinApiKey_NoEnviaCabecera()
    {
        FakeFoundryHandler handler = new("""{"data":[{"embedding":[1.0]}]}""");
        FoundryEmbeddingGenerator generator = Create(handler, Options(apiKey: string.Empty));

        await generator.GenerateAsync("x", TestContext.Current.CancellationToken);

        handler.LastApiKey.ShouldBeNull();
    }

    [Fact]
    public async Task GenerateAsync_SinDatos_Lanza()
    {
        FakeFoundryHandler handler = new("""{"data":[]}""");
        FoundryEmbeddingGenerator generator = Create(handler, Options());

        await Should.ThrowAsync<InvalidOperationException>(() => generator.GenerateAsync("x"));
    }

    [Fact]
    public async Task GenerateAsync_RespuestaNula_Lanza()
    {
        FakeFoundryHandler handler = new("null");
        FoundryEmbeddingGenerator generator = Create(handler, Options());

        await Should.ThrowAsync<InvalidOperationException>(() => generator.GenerateAsync("x"));
    }

    [Fact]
    public void Constructor_ConArgumentosNulos_Lanza()
    {
        Should.Throw<ArgumentNullException>(() => new FoundryEmbeddingGenerator(null!, Options()));
        Should.Throw<ArgumentNullException>(() => new FoundryEmbeddingGenerator(new HttpClient(new FakeFoundryHandler("{}")), null!));
    }
}
