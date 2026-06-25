using JYDE.OpenDataCopilot.Application.Conversation;
using Shouldly;

namespace JYDE.OpenDataCopilot.Application.Tests.Conversation;

/// <summary>Pruebas del <see cref="CopilotOrchestrator"/>.</summary>
public sealed class CopilotOrchestratorTests
{
    private static async Task<List<ConversationEvent>> CollectAsync(IAsyncEnumerable<ConversationEvent> source)
    {
        List<ConversationEvent> events = [];
        await foreach (ConversationEvent conversationEvent in source)
        {
            events.Add(conversationEvent);
        }

        return events;
    }

    [Fact]
    public async Task AskAsync_EnrutaYReemiteLosEventosDelAgente()
    {
        StubAgent agent = new(
            "recomendador",
            canHandle: true,
            ConversationEvent.ForAgent("recomendador"),
            ConversationEvent.ForToken("hola"),
            ConversationEvent.Completed());
        CopilotOrchestrator orchestrator = new([agent], new DefaultAgentRouter());

        List<ConversationEvent> events = await CollectAsync(
            orchestrator.AskAsync("¿qué datasets de salud hay?", cancellationToken: TestContext.Current.CancellationToken));

        events.Select(e => e.Kind).ShouldBe(
            [ConversationEventKind.Agent, ConversationEventKind.Token, ConversationEventKind.Done]);
        events[0].Agent.ShouldBe("recomendador");
    }

    [Fact]
    public async Task AskAsync_ConPreguntaVacia_Lanza()
    {
        CopilotOrchestrator orchestrator = new([new StubAgent("a")], new DefaultAgentRouter());

        await Should.ThrowAsync<ArgumentException>(
            async () => await CollectAsync(orchestrator.AskAsync("   ", cancellationToken: TestContext.Current.CancellationToken)));
    }

    [Fact]
    public async Task AskAsync_ConTopKInvalido_Lanza()
    {
        CopilotOrchestrator orchestrator = new([new StubAgent("a")], new DefaultAgentRouter());

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            async () => await CollectAsync(orchestrator.AskAsync("hola", topK: 0, cancellationToken: TestContext.Current.CancellationToken)));
    }

    [Fact]
    public void Constructor_ConArgumentosNulos_Lanza()
    {
        Should.Throw<ArgumentNullException>(() => new CopilotOrchestrator(null!, new DefaultAgentRouter()));
        Should.Throw<ArgumentNullException>(() => new CopilotOrchestrator([new StubAgent("a")], null!));
    }
}
