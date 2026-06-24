using System.Text;
using JYDE.OpenDataCopilot.Application.Search;

namespace JYDE.OpenDataCopilot.Infrastructure.Embeddings;

/// <summary>
/// Adaptador de <see cref="IEmbeddingGenerator"/> determinista y sin costo para desarrollo/pruebas
/// (ver [ADR-0013]). Construye un vector denso por *hashing* de los términos del texto (bag-of-words)
/// y lo normaliza (L2). No requiere red; da similitud por términos compartidos.
/// </summary>
public sealed class LocalHashingEmbeddingGenerator : IEmbeddingGenerator
{
    /// <summary>Dimensión por defecto del vector.</summary>
    public const int DefaultDimensions = 256;

    private readonly int _dimensions;

    /// <summary>Crea el generador local.</summary>
    /// <param name="dimensions">Tamaño del vector (por defecto <see cref="DefaultDimensions"/>).</param>
    /// <exception cref="ArgumentOutOfRangeException">Si <paramref name="dimensions"/> es menor que 1.</exception>
    public LocalHashingEmbeddingGenerator(int dimensions = DefaultDimensions)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(dimensions, 1);
        _dimensions = dimensions;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<float>> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        float[] vector = new float[_dimensions];
        foreach (string token in Tokenize(text))
        {
            uint bucket = Fnv1a(token) % (uint)_dimensions;
            vector[bucket] += 1f;
        }

        Normalize(vector);
        return Task.FromResult<IReadOnlyList<float>>(vector);
    }

    /// <summary>Divide el texto en términos en minúsculas (separadores no alfanuméricos).</summary>
    private static IEnumerable<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        StringBuilder current = new();
        foreach (char character in text)
        {
            if (char.IsLetterOrDigit(character))
            {
                current.Append(char.ToLowerInvariant(character));
            }
            else if (current.Length > 0)
            {
                yield return current.ToString();
                current.Clear();
            }
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }

    /// <summary>Hash FNV-1a (32 bits) estable entre ejecuciones.</summary>
    private static uint Fnv1a(string token)
    {
        const uint OffsetBasis = 2166136261;
        const uint Prime = 16777619;

        uint hash = OffsetBasis;
        foreach (byte b in Encoding.UTF8.GetBytes(token))
        {
            hash ^= b;
            hash *= Prime;
        }

        return hash;
    }

    /// <summary>Normaliza el vector a norma L2 = 1 (si no es cero).</summary>
    private static void Normalize(float[] vector)
    {
        double sumSquares = 0d;
        foreach (float value in vector)
        {
            sumSquares += value * value;
        }

        if (sumSquares == 0d)
        {
            return;
        }

        float norm = (float)Math.Sqrt(sumSquares);
        for (int i = 0; i < vector.Length; i++)
        {
            vector[i] /= norm;
        }
    }
}
