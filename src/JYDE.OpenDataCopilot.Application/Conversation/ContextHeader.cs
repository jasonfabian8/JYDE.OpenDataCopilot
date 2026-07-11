namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Compone el encabezado de contexto (memoria) que los agentes anteponen a su input: el objetivo
/// acumulado de la conversación y los datasets que el usuario mantiene seleccionados. Así el agente
/// no pierde el hilo aunque la conversación sea larga.
/// </summary>
internal static class ContextHeader
{
    /// <summary>Devuelve el encabezado de contexto, o cadena vacía si no hay memoria que anteponer.</summary>
    public static string For(ConversationContext context)
    {
        List<string> lines = [];
        if (!string.IsNullOrWhiteSpace(context.Objective))
        {
            lines.Add($"Objetivo del usuario (memoria de la conversación): {context.Objective}");
        }

        if (context.SelectedDatasets is { Count: > 0 } selected)
        {
            lines.Add($"Datasets seleccionados por el usuario: {string.Join(", ", selected.Select(dataset => dataset.Name))}");
        }

        return lines.Count == 0
            ? string.Empty
            : $"{string.Join(Environment.NewLine, lines)}{Environment.NewLine}{Environment.NewLine}";
    }
}
