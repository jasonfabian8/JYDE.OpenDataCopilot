using JYDE.OpenDataCopilot.Application.Conversation;
using JYDE.OpenDataCopilot.Application.Search;
using JYDE.OpenDataCopilot.Application.Tests.Search;
using Shouldly;

namespace JYDE.OpenDataCopilot.Application.Tests.Conversation;

/// <summary>Pruebas del <see cref="DatasetRecommenderAgent"/>.</summary>
public sealed class DatasetRecommenderAgentTests
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
    public async Task HandleAsync_ConDatasetsRelevantes_EmiteAgenteFuentesYTokens()
    {
        StubEmbeddingGenerator embeddings = new();
        CapturingSearchIndex index = new()
        {
            NextResults = [new DatasetSearchHit("aaaa-0001", "Cobertura de salud", "Salud", "https://datos.gov.co/d/aaaa-0001", 0.92)],
        };
        StubChatCompletion chat = new("Te recomiendo ese dataset.");
        DatasetRecommenderAgent agent = new(embeddings, index, chat);

        List<ConversationEvent> events = await CollectAsync(
            agent.HandleAsync(new ConversationContext("cobertura de salud", 3), TestContext.Current.CancellationToken));

        events[0].Kind.ShouldBe(ConversationEventKind.Agent);
        events[0].Agent.ShouldBe("dataset-recommender-agent");
        events[1].Kind.ShouldBe(ConversationEventKind.Sources);
        events[1].Sources.ShouldNotBeNull().ShouldHaveSingleItem().DatasetId.ShouldBe("aaaa-0001");
        events.Where(e => e.Kind == ConversationEventKind.Token).ShouldNotBeEmpty();
        events.ShouldContain(e => e.Kind == ConversationEventKind.Conversation && e.ConversationId == "stub-response-id");
        events[^1].Kind.ShouldBe(ConversationEventKind.Done);
        chat.LastPrompt.ShouldNotBeNull().Input.ShouldContain("cobertura de salud");
    }

    [Fact]
    public async Task HandleAsync_ContinuaElHilo_PasandoElIdPrevio()
    {
        CapturingSearchIndex index = new()
        {
            NextResults = [new DatasetSearchHit("aaaa-0001", "Uno", "Cat", "https://x", 0.9)],
        };
        StubChatCompletion chat = new("ok", responseId: "nuevo-id");
        DatasetRecommenderAgent agent = new(new StubEmbeddingGenerator(), index, chat);

        await CollectAsync(agent.HandleAsync(
            new ConversationContext("sigue", 3, "id-previo"), TestContext.Current.CancellationToken));

        chat.LastPrompt.ShouldNotBeNull().PreviousResponseId.ShouldBe("id-previo");
    }

    [Fact]
    public async Task HandleAsync_SinDatasetsRelevantes_DeclaraQueNoHay_SinFuentes()
    {
        DatasetRecommenderAgent agent = new(new StubEmbeddingGenerator(), new CapturingSearchIndex(), new StubChatCompletion());

        List<ConversationEvent> events = await CollectAsync(
            agent.HandleAsync(new ConversationContext("algo sin resultados", 3), TestContext.Current.CancellationToken));

        events.ShouldNotContain(e => e.Kind == ConversationEventKind.Sources);
        events.ShouldContain(e => e.Kind == ConversationEventKind.Token);
        events[^1].Kind.ShouldBe(ConversationEventKind.Done);
    }

    [Fact]
    public async Task HandleAsync_ConDatasetSinCategoriaNiUrl_IgualConstruyeRespuesta()
    {
        CapturingSearchIndex index = new()
        {
            NextResults = [new DatasetSearchHit("aaaa-0002", "Dataset sin metadatos", null, null, 0.4)],
        };
        StubChatCompletion chat = new("ok");
        DatasetRecommenderAgent agent = new(new StubEmbeddingGenerator(), index, chat);

        List<ConversationEvent> events = await CollectAsync(
            agent.HandleAsync(new ConversationContext("consulta", 3), TestContext.Current.CancellationToken));

        events.ShouldContain(e => e.Kind == ConversationEventKind.Sources);
        chat.LastPrompt.ShouldNotBeNull().Input.ShouldContain("n/d");
    }

    [Fact]
    public async Task HandleAsync_ConContextoNulo_Lanza()
    {
        DatasetRecommenderAgent agent = new(new StubEmbeddingGenerator(), new CapturingSearchIndex(), new StubChatCompletion());

        await Should.ThrowAsync<ArgumentNullException>(
            async () => await CollectAsync(agent.HandleAsync(null!, TestContext.Current.CancellationToken)));
    }

    [Fact]
    public void Metadatos_YCanHandle_SonValidos()
    {
        DatasetRecommenderAgent agent = new(new StubEmbeddingGenerator(), new CapturingSearchIndex(), new StubChatCompletion());

        agent.Name.ShouldBe("dataset-recommender-agent");
        agent.Description.ShouldNotBeNullOrWhiteSpace();
        agent.CanHandle("cualquier cosa").ShouldBeTrue();
    }

    [Fact]
    public void Constructor_ConArgumentosNulos_Lanza()
    {
        StubEmbeddingGenerator embeddings = new();
        CapturingSearchIndex index = new();
        StubChatCompletion chat = new();

        Should.Throw<ArgumentNullException>(() => new DatasetRecommenderAgent(null!, index, chat));
        Should.Throw<ArgumentNullException>(() => new DatasetRecommenderAgent(embeddings, null!, chat));
        Should.Throw<ArgumentNullException>(() => new DatasetRecommenderAgent(embeddings, index, null!));
    }
}
