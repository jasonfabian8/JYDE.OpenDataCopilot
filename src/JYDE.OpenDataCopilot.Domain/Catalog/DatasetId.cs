using System.Text.RegularExpressions;

namespace JYDE.OpenDataCopilot.Domain.Catalog;

/// <summary>
/// Identificador de un dataset de Socrata (formato "4x4", p. ej. <c>ddau-8cy9</c>).
/// Value object inmutable que garantiza un identificador válido.
/// </summary>
public sealed partial record DatasetId
{
    [GeneratedRegex("^[a-z0-9]{4}-[a-z0-9]{4}$")]
    private static partial Regex IdPattern();

    /// <summary>Valor textual del identificador (formato 4x4 en minúsculas).</summary>
    public string Value { get; }

    /// <summary>Crea un <see cref="DatasetId"/> validando el formato 4x4 de Socrata.</summary>
    /// <param name="value">Identificador en formato <c>xxxx-xxxx</c>.</param>
    /// <exception cref="ArgumentException">Si el valor es nulo, vacío o no cumple el formato.</exception>
    public DatasetId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("El identificador del dataset no puede estar vacío.", nameof(value));
        }

        string normalized = value.Trim().ToLowerInvariant();
        if (!IdPattern().IsMatch(normalized))
        {
            throw new ArgumentException(
                $"El identificador '{value}' no cumple el formato 4x4 de Socrata (ej. 'ddau-8cy9').",
                nameof(value));
        }

        Value = normalized;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
