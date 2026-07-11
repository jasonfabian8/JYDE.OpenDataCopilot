using JYDE.OpenDataCopilot.Application.Conversation;
using Shouldly;

namespace JYDE.OpenDataCopilot.Application.Tests.Conversation;

/// <summary>Pruebas del <see cref="CopilotOrchestrator"/>.</summary>
public sealed class CopilotOrchestratorTests
{
    private static ObjectiveTracker Tracker(string chatText = "sin json") => new(new StubChatCompletion(chatText));

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
        CopilotOrchestrator orchestrator = new([agent], new DefaultAgentRouter(), Tracker(), new InteractionRecorder());

        List<ConversationEvent> events = await CollectAsync(
            orchestrator.AskAsync("¿qué datasets de salud hay?", cancellationToken: TestContext.Current.CancellationToken));

        events.Select(e => e.Kind).ShouldBe(
            [ConversationEventKind.Agent, ConversationEventKind.Token, ConversationEventKind.Done]);
        events[0].Agent.ShouldBe("recomendador");
    }

    [Fact]
    public async Task AskAsync_ActualizaYEmiteElObjetivo_AntesDelDone()
    {
        StubAgent agent = new(
            "reco", canHandle: true,
            ConversationEvent.ForAgent("reco"), ConversationEvent.ForToken("hola"), ConversationEvent.Completed());
        CopilotOrchestrator orchestrator = new(
            [agent], new DefaultAgentRouter(), Tracker("{\"objetivo\":\"analizar la mortalidad\"}"), new InteractionRecorder());

        List<ConversationEvent> events = await CollectAsync(orchestrator.AskAsync(
            "mortalidad", objective: "algo previo", cancellationToken: TestContext.Current.CancellationToken));

        ConversationEvent objective = events.Single(e => e.Kind == ConversationEventKind.Objective);
        objective.Objective.ShouldBe("analizar la mortalidad");
        events[^1].Kind.ShouldBe(ConversationEventKind.Done);
    }

    [Fact]
    public async Task AskAsync_AnexaLaAuditoriaDeLasInteracciones()
    {
        InteractionRecorder recorder = new();
        ObjectiveTracker tracker = new(new AuditingChatCompletion(new StubChatCompletion("{\"objetivo\":\"analizar\"}"), recorder));
        StubAgent agent = new(
            "reco", canHandle: true,
            ConversationEvent.ForAgent("reco"), ConversationEvent.ForToken("hola"), ConversationEvent.Completed());
        CopilotOrchestrator orchestrator = new([agent], new DefaultAgentRouter(), tracker, recorder);

        List<ConversationEvent> events = await CollectAsync(
            orchestrator.AskAsync("mortalidad", cancellationToken: TestContext.Current.CancellationToken));

        ConversationEvent audit = events.Single(e => e.Kind == ConversationEventKind.Audit);
        AgentInteraction interaction = audit.Interactions.ShouldNotBeNull().ShouldHaveSingleItem();
        interaction.Agent.ShouldBe("objective-tracker-agent");
    }

    [Fact]
    public async Task AskAsync_ConPreguntaVacia_Lanza()
    {
        CopilotOrchestrator orchestrator = new([new StubAgent("a")], new DefaultAgentRouter(), Tracker(), new InteractionRecorder());

        await Should.ThrowAsync<ArgumentException>(
            async () => await CollectAsync(orchestrator.AskAsync("   ", cancellationToken: TestContext.Current.CancellationToken)));
    }

    [Fact]
    public async Task AskAsync_ConTopKInvalido_Lanza()
    {
        CopilotOrchestrator orchestrator = new([new StubAgent("a")], new DefaultAgentRouter(), Tracker(), new InteractionRecorder());

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            async () => await CollectAsync(orchestrator.AskAsync("hola", topK: 0, cancellationToken: TestContext.Current.CancellationToken)));
    }

    [Fact]
    public void Constructor_ConArgumentosNulos_Lanza()
    {
        ObjectiveTracker tracker = Tracker();
        InteractionRecorder recorder = new();

        Should.Throw<ArgumentNullException>(() => new CopilotOrchestrator(null!, new DefaultAgentRouter(), tracker, recorder));
        Should.Throw<ArgumentNullException>(() => new CopilotOrchestrator([new StubAgent("a")], null!, tracker, recorder));
        Should.Throw<ArgumentNullException>(() => new CopilotOrchestrator([new StubAgent("a")], new DefaultAgentRouter(), null!, recorder));
        Should.Throw<ArgumentNullException>(() => new CopilotOrchestrator([new StubAgent("a")], new DefaultAgentRouter(), tracker, null!));
    }
}
