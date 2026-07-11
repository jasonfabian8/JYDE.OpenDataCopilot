using System.Runtime.CompilerServices;
using JYDE.OpenDataCopilot.Application.Conversation;

namespace JYDE.OpenDataCopilot.Api.Tests.Conversation;

/// <summary>Agente de prueba que emite una lista fija de eventos (para probar el mapeo SSE).</summary>
internal sealed class FixedEventsAgent : IConversationAgent
{
    private readonly IReadOnlyList<ConversationEvent> _events;

    public FixedEventsAgent(IReadOnlyList<ConversationEvent> events) => _events = events;

    public string Name => "fixed-events-agent";

    public string Description => "Agente de prueba que emite eventos fijos.";

    public bool CanHandle(string question) => true;

    public async IAsyncEnumerable<ConversationEvent> HandleAsync(
        ConversationContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (ConversationEvent conversationEvent in _events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return conversationEvent;
            await Task.CompletedTask;
        }
    }
}
