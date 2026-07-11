using System.Text.Json;

namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Enrutador conversacional basado en LLM: envía al modelo la consulta y la lista de agentes
/// disponibles (nombre + descripción) y este decide cuál debe atenderla (JSON con el nombre). Si el
/// enrutador falla o no devuelve un nombre válido, degrada a una selección por reglas
/// (<see cref="IConversationAgent.CanHandle"/>).
/// </summary>
public sealed class LlmAgentRouter : IAgentRouter
{
    /// <summary>Nombre por defecto del agente enrutador (en Foundry).</summary>
    public const string DefaultRouterAgent = "router-agent";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IChatCompletion _chat;
    private readonly string _routerAgent;

    /// <summary>Crea el enrutador LLM.</summary>
    /// <param name="chat">Modelo de chat (LLM) que hospeda el agente enrutador.</param>
    /// <param name="routerAgent">Nombre del agente enrutador (por defecto <see cref="DefaultRouterAgent"/>).</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="chat"/> es nulo.</exception>
    public LlmAgentRouter(IChatCompletion chat, string? routerAgent = null)
    {
        ArgumentNullException.ThrowIfNull(chat);
        _chat = chat;
        _routerAgent = string.IsNullOrWhiteSpace(routerAgent) ? DefaultRouterAgent : routerAgent;
    }

    /// <inheritdoc />
    public async Task<IConversationAgent> RouteAsync(
        string question,
        IReadOnlyList<IConversationAgent> agents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agents);
        if (agents.Count == 0)
        {
            throw new InvalidOperationException("No hay agentes registrados para atender la conversación.");
        }

        if (agents.Count == 1)
        {
            return agents[0];
        }

        IConversationAgent? chosen = await TryRouteAsync(question, agents, cancellationToken);
        return chosen
            ?? agents.FirstOrDefault(agent => agent.CanHandle(question))
            ?? agents[0];
    }

    private async Task<IConversationAgent?> TryRouteAsync(
        string question,
        IReadOnlyList<IConversationAgent> agents,
        CancellationToken cancellationToken)
    {
        try
        {
            ChatResult result = await _chat.CompleteAsync(
                new ChatPrompt(_routerAgent, BuildInput(question, agents)),
                cancellationToken);

            string? name = ParseAgentName(result.Text);
            return name is null
                ? null
                : agents.FirstOrDefault(agent => string.Equals(agent.Name, name, StringComparison.OrdinalIgnoreCase));
        }
        catch (HttpRequestException)
        {
            // El agente enrutador no está disponible (p. ej. no creado en Foundry): degrada a reglas.
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static string? ParseAgentName(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }

        try
        {
            RouterReply? reply = JsonSerializer.Deserialize<RouterReply>(text[start..(end + 1)], JsonOptions);
            return string.IsNullOrWhiteSpace(reply?.Agente) ? null : reply.Agente.Trim();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string BuildInput(string question, IReadOnlyList<IConversationAgent> agents)
    {
        // Solo datos: la regla de selección y el esquema JSON viven en la instrucción del enrutador en Foundry.
        string nl = Environment.NewLine;
        string catalog = string.Join(nl, agents.Select(agent => $"- {agent.Name}: {agent.Description}"));

        return
            $"Consulta del usuario: {question}{nl}{nl}" +
            $"Agentes disponibles:{nl}{catalog}";
    }
}
