namespace JYDE.OpenDataCopilot.Application.Search;

/// <summary>
/// Puerto de salida: genera el vector (embedding) de un texto para la búsqueda semántica.
/// </summary>
public interface IEmbeddingGenerator
{
    /// <summary>Genera el embedding del texto dado.</summary>
    /// <param name="text">Texto a vectorizar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Vector de características (longitud fija para un mismo generador).</returns>
    Task<IReadOnlyList<float>> GenerateAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Genera los embeddings de varios textos en una sola operación (por lote). Permite indexar
    /// catálogos grandes sin una llamada por dataset. El resultado conserva el orden de la entrada.
    /// </summary>
    /// <param name="texts">Textos a vectorizar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Un vector por cada texto, en el mismo orden que la entrada.</returns>
    Task<IReadOnlyList<IReadOnlyList<float>>> GenerateBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);
}
