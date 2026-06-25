using System.Runtime.CompilerServices;

namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Copilot orquestador: punto de entrada de la conversación. Enruta la pregunta al agente
/// especializado adecuado (vía <see cref="IAgentRouter"/>) y reemite su flujo de eventos.
/// </summary>
public sealed class CopilotOrchestrator
{
    /// <summary>Número de datasets relevantes por defecto.</summary>
    public const int DefaultTopK = 5;

    private readonly IReadOnlyList<IConversationAgent> _agents;
    private readonly IAgentRouter _router;

    /// <summary>Crea el orquestador con los agentes disponibles y la estrategia de enrutamiento.</summary>
    /// <param name="agents">Agentes registrados.</param>
    /// <param name="router">Estrategia de enrutamiento.</param>
    /// <exception cref="ArgumentNullException">Si algún argumento es nulo.</exception>
    public CopilotOrchestrator(IEnumerable<IConversationAgent> agents, IAgentRouter router)
    {
        ArgumentNullException.ThrowIfNull(agents);
        ArgumentNullException.ThrowIfNull(router);
        _agents = [.. agents];
        _router = router;
    }

    /// <summary>Atiende una pregunta del usuario y emite el flujo de eventos de la respuesta.</summary>
    /// <param name="question">Pregunta en lenguaje natural.</param>
    /// <param name="topK">Número máximo de datasets relevantes a considerar.</param>
    /// <param name="previousResponseId">Id del turno anterior para continuar el hilo (nulo si es nuevo).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <exception cref="ArgumentException">Si la pregunta está vacía.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Si <paramref name="topK"/> es menor que 1.</exception>
    public async IAsyncEnumerable<ConversationEvent> AskAsync(
        string question,
        int topK = DefaultTopK,
        string? previousResponseId = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("La pregunta no puede estar vacía.", nameof(question));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(topK, 1);

        IConversationAgent agent = _router.Route(question, _agents);
        ConversationContext context = new(question.Trim(), topK, previousResponseId);

        await foreach (ConversationEvent conversationEvent in agent.HandleAsync(context, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return conversationEvent;
        }
    }
}
