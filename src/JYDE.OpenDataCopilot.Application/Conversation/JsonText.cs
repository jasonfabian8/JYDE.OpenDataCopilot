namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Utilidades para leer el JSON que devuelve un LLM, que puede venir con prosa alrededor, vallas de
/// código (```), o el mismo objeto DUPLICADO (algunos modelos/gateways lo emiten dos veces).
/// </summary>
internal static class JsonText
{
    /// <summary>
    /// Extrae el PRIMER objeto JSON balanceado del texto (por conteo de llaves, ignorando las que
    /// están dentro de cadenas). Así se evita mezclar objetos duplicados o texto sobrante.
    /// </summary>
    /// <param name="text">Texto que puede contener un objeto JSON.</param>
    /// <returns>El primer objeto <c>{...}</c> completo, o <c>null</c> si no hay uno cerrado.</returns>
    public static string? FirstJsonObject(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        int start = text.IndexOf('{');
        if (start < 0)
        {
            return null;
        }

        int depth = 0;
        bool inString = false;
        bool escaped = false;
        for (int i = start; i < text.Length; i++)
        {
            char current = text[i];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (current == '\\')
                {
                    escaped = true;
                }
                else if (current == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (current == '"')
            {
                inString = true;
            }
            else if (current == '{')
            {
                depth++;
            }
            else if (current == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return text[start..(i + 1)];
                }
            }
        }

        return null;
    }
}
