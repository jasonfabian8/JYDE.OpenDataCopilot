using JYDE.OpenDataCopilot.Application.Search;

namespace JYDE.OpenDataCopilot.Application.Tests.Search;

/// <summary>Doble de prueba de <see cref="IEmbeddingGenerator"/> determinista.</summary>
internal sealed class StubEmbeddingGenerator : IEmbeddingGenerator
{
    /// <summary>Último texto recibido.</summary>
    public string? LastText { get; private set; }

    public Task<IReadOnlyList<float>> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        LastText = text;
        IReadOnlyList<float> vector = [text.Length, 1f];
        return Task.FromResult(vector);
    }
}
