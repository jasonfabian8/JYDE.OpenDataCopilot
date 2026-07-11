using JYDE.OpenDataCopilot.Infrastructure.Embeddings;
using Shouldly;
using Xunit;

namespace JYDE.OpenDataCopilot.Infrastructure.IntegrationTests.Embeddings;

/// <summary>Pruebas del adaptador <see cref="LocalHashingEmbeddingGenerator"/>.</summary>
public sealed class LocalHashingEmbeddingGeneratorTests
{
    private static double Cosine(IReadOnlyList<float> a, IReadOnlyList<float> b)
    {
        double dot = 0d, na = 0d, nb = 0d;
        for (int i = 0; i < a.Count; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }

        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    [Fact]
    public async Task GenerateAsync_EsDeterminista_YTieneLaDimensionConfigurada()
    {
        LocalHashingEmbeddingGenerator generator = new(dimensions: 64);

        IReadOnlyList<float> first = await generator.GenerateAsync("accidentalidad vial Bogotá", TestContext.Current.CancellationToken);
        IReadOnlyList<float> second = await generator.GenerateAsync("accidentalidad vial Bogotá", TestContext.Current.CancellationToken);

        first.Count.ShouldBe(64);
        first.ShouldBe(second);
    }

    [Fact]
    public async Task GenerateAsync_NormalizaElVector()
    {
        LocalHashingEmbeddingGenerator generator = new();

        IReadOnlyList<float> vector = await generator.GenerateAsync("salud cobertura vacunación", TestContext.Current.CancellationToken);

        double norm = Math.Sqrt(vector.Sum(value => (double)value * value));
        norm.ShouldBe(1d, tolerance: 1e-5);
    }

    [Fact]
    public async Task GenerateAsync_TextoVacio_DevuelveVectorCero()
    {
        LocalHashingEmbeddingGenerator generator = new(dimensions: 16);

        IReadOnlyList<float> vector = await generator.GenerateAsync("   ", TestContext.Current.CancellationToken);

        vector.ShouldAllBe(value => value == 0f);
    }

    [Fact]
    public async Task GenerateAsync_TextosConTerminosCompartidos_SonMasSimilares()
    {
        LocalHashingEmbeddingGenerator generator = new();

        IReadOnlyList<float> a = await generator.GenerateAsync("accidentes de tránsito en vías", TestContext.Current.CancellationToken);
        IReadOnlyList<float> b = await generator.GenerateAsync("vías y accidentes de tránsito", TestContext.Current.CancellationToken);
        IReadOnlyList<float> c = await generator.GenerateAsync("cobertura de vacunación en salud", TestContext.Current.CancellationToken);

        Cosine(a, b).ShouldBeGreaterThan(Cosine(a, c));
    }

    [Fact]
    public void Constructor_ConDimensionInvalida_Lanza()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new LocalHashingEmbeddingGenerator(0));
    }
}
