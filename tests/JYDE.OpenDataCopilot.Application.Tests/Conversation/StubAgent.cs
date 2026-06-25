using System.Runtime.CompilerServices;
using JYDE.OpenDataCopilot.Application.Conversation;

namespace JYDE.OpenDataCopilot.Application.Tests.Conversation;

/// <summary>Doble de prueba de <see cref="IConversationAgent"/> con eventos y enrutamiento controlables.</summary>
internal sealed class StubAgent : IConversationAgent
{
    private readonly bool _canHandle;
    private readonly ConversationEvent[] _events;

    public StubAgent(string name, bool canHandle = true, params ConversationEvent[] events)
    {
        Name = name;
        _canHandle = canHandle;
        _events = events.Length > 0 ? events : [ConversationEvent.ForAgent(name), ConversationEvent.Completed()];
    }

    public string Name { get; }

    public string Description => $"Agente de prueba {Name}.";

    public bool CanHandle(string question) => _canHandle;

    public async IAsyncEnumerable<ConversationEvent> HandleAsync(
        ConversationContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (ConversationEvent conversationEvent in _events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return conversationEvent;
            await Task.Yield();
        }
    }
}
