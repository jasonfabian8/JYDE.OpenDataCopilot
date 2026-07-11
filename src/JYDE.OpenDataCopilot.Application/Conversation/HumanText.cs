using System.Text.RegularExpressions;

namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Rescata el texto legible cuando el JSON del LLM no se pudo parsear, para NUNCA mostrarle al usuario
/// el JSON crudo. Si el texto parece JSON (aunque esté malformado) intenta extraer la
/// <c>respuesta</c>/<c>explicacion</c>; si no, devuelve el texto tal cual (es prosa) o un mensaje limpio.
/// </summary>
internal static partial class HumanText
{
    [GeneratedRegex("\"respuesta\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RespuestaField();

    [GeneratedRegex("\"explicacion\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex ExplicacionField();

    /// <summary>Devuelve texto legible: prosa tal cual, la <c>respuesta</c> rescatada, o el respaldo.</summary>
    public static string Salvage(string text, string fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return fallback;
        }

        string trimmed = text.Trim();
        if (!trimmed.StartsWith('{'))
        {
            return trimmed;
        }

        string? value = Extract(RespuestaField(), trimmed) ?? Extract(ExplicacionField(), trimmed);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static string? Extract(Regex regex, string text)
    {
        Match match = regex.Match(text);
        if (!match.Success)
        {
            return null;
        }

        string value = match.Groups[1].Value.Replace("\\\"", "\"").Replace("\\n", "\n").Trim();
        return value.Length == 0 ? null : value;
    }
}
