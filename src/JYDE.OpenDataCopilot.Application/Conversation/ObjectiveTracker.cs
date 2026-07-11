using System.Text.Json;

namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Rastreador del objetivo de la conversación (memoria): dado el objetivo actual y el último mensaje
/// del ciudadano, pide al LLM una versión actualizada y concisa del objetivo, para no perder el hilo
/// en conversaciones largas. Degrada al objetivo actual si el modelo falla o no devuelve JSON válido.
/// </summary>
public sealed class ObjectiveTracker
{
    /// <summary>Nombre por defecto del agente rastreador (en Foundry).</summary>
    public const string DefaultAgent = "objective-tracker-agent";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IChatCompletion _chat;
    private readonly string _agentName;

    /// <summary>Crea el rastreador de objetivo.</summary>
    /// <param name="chat">Modelo de chat (LLM) que hospeda el agente rastreador.</param>
    /// <param name="agentName">Nombre del agente (por defecto <see cref="DefaultAgent"/>).</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="chat"/> es nulo.</exception>
    public ObjectiveTracker(IChatCompletion chat, string? agentName = null)
    {
        ArgumentNullException.ThrowIfNull(chat);
        _chat = chat;
        _agentName = string.IsNullOrWhiteSpace(agentName) ? DefaultAgent : agentName;
    }

    /// <summary>Actualiza el objetivo a partir del actual y el último mensaje del ciudadano.</summary>
    /// <param name="currentObjective">Objetivo acumulado hasta ahora (puede ser nulo/vacío).</param>
    /// <param name="userMessage">Último mensaje del ciudadano.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El objetivo actualizado; el actual si no se pudo actualizar.</returns>
    public async Task<string> UpdateAsync(
        string? currentObjective,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        string current = currentObjective?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return current;
        }

        try
        {
            ChatResult result = await _chat.CompleteAsync(new ChatPrompt(_agentName, BuildInput(current, userMessage)), cancellationToken);
            string updated = ExtractObjective(result.Text);
            return string.IsNullOrWhiteSpace(updated) ? current : updated;
        }
        catch (HttpRequestException)
        {
            return current;
        }
        catch (InvalidOperationException)
        {
            return current;
        }
    }

    private static string BuildInput(string currentObjective, string userMessage)
    {
        string nl = Environment.NewLine;
        string objective = string.IsNullOrWhiteSpace(currentObjective) ? "(aún no hay objetivo)" : currentObjective;
        return
            $"Objetivo actual: {objective}{nl}{nl}" +
            $"Último mensaje del ciudadano: {userMessage}";
    }

    private static string ExtractObjective(string text)
    {
        string? json = JsonText.FirstJsonObject(text);
        if (json is null)
        {
            return string.Empty;
        }

        try
        {
            ObjectiveReply? reply = JsonSerializer.Deserialize<ObjectiveReply>(json, JsonOptions);
            return string.IsNullOrWhiteSpace(reply?.Objetivo) ? string.Empty : reply.Objetivo.Trim();
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }
}
