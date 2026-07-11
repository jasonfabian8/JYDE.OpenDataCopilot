using System.Runtime.CompilerServices;

namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Copilot orquestador: punto de entrada de la conversación. Enruta la pregunta al agente
/// especializado adecuado (vía <see cref="IAgentRouter"/>), reemite su flujo de eventos, actualiza el
/// objetivo (memoria) con <see cref="ObjectiveTracker"/> y anexa la auditoría de las interacciones.
/// </summary>
public sealed class CopilotOrchestrator
{
    /// <summary>Número de datasets relevantes por defecto.</summary>
    public const int DefaultTopK = 5;

    private readonly IReadOnlyList<IConversationAgent> _agents;
    private readonly IAgentRouter _router;
    private readonly ObjectiveTracker _objectiveTracker;
    private readonly IInteractionRecorder _recorder;

    /// <summary>Crea el orquestador con los agentes, el enrutador, el rastreador de objetivo y el grabador.</summary>
    /// <param name="agents">Agentes registrados.</param>
    /// <param name="router">Estrategia de enrutamiento.</param>
    /// <param name="objectiveTracker">Rastreador del objetivo de la conversación (memoria).</param>
    /// <param name="recorder">Grabador de interacciones (auditoría).</param>
    /// <exception cref="ArgumentNullException">Si algún argumento es nulo.</exception>
    public CopilotOrchestrator(
        IEnumerable<IConversationAgent> agents,
        IAgentRouter router,
        ObjectiveTracker objectiveTracker,
        IInteractionRecorder recorder)
    {
        ArgumentNullException.ThrowIfNull(agents);
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(objectiveTracker);
        ArgumentNullException.ThrowIfNull(recorder);
        _agents = [.. agents];
        _router = router;
        _objectiveTracker = objectiveTracker;
        _recorder = recorder;
    }

    /// <summary>Atiende una pregunta del usuario y emite el flujo de eventos de la respuesta.</summary>
    /// <param name="question">Pregunta en lenguaje natural.</param>
    /// <param name="topK">Número máximo de datasets relevantes a considerar.</param>
    /// <param name="previousResponseId">Id del turno anterior para continuar el hilo (nulo si es nuevo).</param>
    /// <param name="objective">Objetivo acumulado hasta ahora (memoria); nulo/vacío si es nuevo.</param>
    /// <param name="selectedDatasets">Datasets (id + nombre) que el usuario mantiene fijados.</param>
    /// <param name="routeContext">Contexto reciente (respuesta anterior) para desambiguar el enrutamiento.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <exception cref="ArgumentException">Si la pregunta está vacía.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Si <paramref name="topK"/> es menor que 1.</exception>
    public IAsyncEnumerable<ConversationEvent> AskAsync(
        string question,
        int topK = DefaultTopK,
        string? previousResponseId = null,
        string? objective = null,
        IReadOnlyList<SelectedDataset>? selectedDatasets = null,
        string? routeContext = null,
        CancellationToken cancellationToken = default)
    {
        // Validación temprana (eager); la iteración perezosa vive en el método iterador privado.
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("La pregunta no puede estar vacía.", nameof(question));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(topK, 1);

        return AskIteratorAsync(question, topK, previousResponseId, objective, selectedDatasets, routeContext, cancellationToken);
    }

    private async IAsyncEnumerable<ConversationEvent> AskIteratorAsync(
        string question,
        int topK,
        string? previousResponseId,
        string? objective,
        IReadOnlyList<SelectedDataset>? selectedDatasets,
        string? routeContext,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _recorder.Begin();

        string trimmed = question.Trim();
        IConversationAgent agent = await _router.RouteAsync(trimmed, _agents, routeContext, cancellationToken);
        ConversationContext context = new(trimmed, topK, previousResponseId, objective, selectedDatasets);

        // Reemitimos los eventos del agente EXCEPTO su Done: el cierre lo controla el orquestador para
        // anexar el objetivo actualizado (memoria) y la auditoría antes de terminar.
        await foreach (ConversationEvent conversationEvent in agent.HandleAsync(context, cancellationToken).WithCancellation(cancellationToken))
        {
            if (conversationEvent.Kind != ConversationEventKind.Done)
            {
                yield return conversationEvent;
            }
        }

        string updatedObjective = await _objectiveTracker.UpdateAsync(objective, trimmed, cancellationToken);
        if (!string.IsNullOrWhiteSpace(updatedObjective))
        {
            yield return ConversationEvent.ForObjective(updatedObjective);
        }

        IReadOnlyList<AgentInteraction> interactions = _recorder.Interactions;
        if (interactions.Count > 0)
        {
            yield return ConversationEvent.ForAudit(interactions);
        }

        yield return ConversationEvent.Completed();
    }
}
