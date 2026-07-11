using System.Globalization;
using JYDE.OpenDataCopilot.Application.Conversation;
using JYDE.OpenDataCopilot.Application.Search;
using JYDE.OpenDataCopilot.Application.Tests.Search;
using Shouldly;

namespace JYDE.OpenDataCopilot.Application.Tests.Conversation;

/// <summary>Pruebas del <see cref="DatasetRecommenderAgent"/> (re-ranking por JSON del LLM).</summary>
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

    /// <summary>Arma la respuesta JSON que el LLM devolvería (respuesta + relevancias por id).</summary>
    private static string Json(string respuesta, params (string Id, double Relevancia)[] datasets)
    {
        string items = string.Join(
            ",",
            datasets.Select(dataset =>
                $"{{\"id\":\"{dataset.Id}\",\"relevancia\":{dataset.Relevancia.ToString(CultureInfo.InvariantCulture)}}}"));
        return $"{{\"respuesta\":\"{respuesta}\",\"datasets\":[{items}]}}";
    }

    [Fact]
    public async Task HandleAsync_ConRelevanciaAltaDelLlm_CitaSoloElRelevante_YEmiteTokens()
    {
        // Dos candidatos por embedding; el LLM solo puntúa uno por encima del umbral: solo ese se cita.
        CapturingSearchIndex index = new()
        {
            NextResults =
            [
                new DatasetSearchHit("aaaa-0001", "Cobertura de salud", "Salud", "https://datos.gov.co/d/aaaa-0001", 0.62),
                new DatasetSearchHit("aaaa-0002", "Otra cosa", "Otra", "https://datos.gov.co/d/aaaa-0002", 0.55),
            ],
        };
        StubChatCompletion chat = new(Json("Te recomiendo ese dataset.", ("aaaa-0001", 0.9)));
        DatasetRecommenderAgent agent = new(new StubEmbeddingGenerator(), index, chat);

        List<ConversationEvent> events = await CollectAsync(
            agent.HandleAsync(new ConversationContext("cobertura de salud", 3), TestContext.Current.CancellationToken));

        events[0].Kind.ShouldBe(ConversationEventKind.Agent);
        events[0].Agent.ShouldBe("dataset-recommender-agent");
        ConversationEvent sources = events.Single(e => e.Kind == ConversationEventKind.Sources);
        Citation citation = sources.Sources.ShouldNotBeNull().ShouldHaveSingleItem();
        citation.DatasetId.ShouldBe("aaaa-0001");
        citation.Score.ShouldBe(0.9, 1e-9); // relevancia recalculada por el LLM, no el coseno
        events.Where(e => e.Kind == ConversationEventKind.Token).ShouldNotBeEmpty();
        events.ShouldContain(e => e.Kind == ConversationEventKind.Conversation && e.ConversationId == "stub-response-id");
        events[^1].Kind.ShouldBe(ConversationEventKind.Done);
        chat.LastPrompt.ShouldNotBeNull().Input.ShouldContain("cobertura de salud");
    }

    [Fact]
    public async Task HandleAsync_ConRelevanciaBajaDelLlm_NoCita_AunqueElCosenoSeaAlto()
    {
        // El coseno es alto (0.62) pero el LLM juzga que no viene al caso (0.2): no debe citarse.
        CapturingSearchIndex index = new()
        {
            NextResults = [new DatasetSearchHit("aaaa-0001", "Registro de activos de información", "Salud", "https://x", 0.62)],
        };
        StubChatCompletion chat = new(Json("Ninguno trata directamente del tema.", ("aaaa-0001", 0.2)));
        DatasetRecommenderAgent agent = new(new StubEmbeddingGenerator(), index, chat);

        List<ConversationEvent> events = await CollectAsync(
            agent.HandleAsync(new ConversationContext("suicidio juvenil", 3), TestContext.Current.CancellationToken));

        events.ShouldNotContain(e => e.Kind == ConversationEventKind.Sources);
        events.Where(e => e.Kind == ConversationEventKind.Token).ShouldNotBeEmpty();
        chat.LastPrompt.ShouldNotBeNull().Input.ShouldContain("aaaa-0001"); // el candidato se envió al LLM para juzgarlo
    }

    [Fact]
    public async Task HandleAsync_ContinuaElHilo_PasandoElIdPrevio()
    {
        CapturingSearchIndex index = new()
        {
            NextResults = [new DatasetSearchHit("aaaa-0001", "Uno", "Cat", "https://x", 0.9)],
        };
        StubChatCompletion chat = new(Json("ok", ("aaaa-0001", 0.8)), responseId: "nuevo-id");
        DatasetRecommenderAgent agent = new(new StubEmbeddingGenerator(), index, chat);

        await CollectAsync(agent.HandleAsync(
            new ConversationContext("sigue", 3, "id-previo"), TestContext.Current.CancellationToken));

        chat.LastPrompt.ShouldNotBeNull().PreviousResponseId.ShouldBe("id-previo");
    }

    [Fact]
    public async Task HandleAsync_SinCandidatos_DeclaraQueNoHay_SinFuentes()
    {
        // JSON sin la clave "datasets" (Datasets = null): igual debe funcionar y no citar nada.
        StubChatCompletion chat = new("{\"respuesta\":\"No hallé datasets pertinentes; intenta reformular.\"}");
        DatasetRecommenderAgent agent = new(new StubEmbeddingGenerator(), new CapturingSearchIndex(), chat);

        List<ConversationEvent> events = await CollectAsync(
            agent.HandleAsync(new ConversationContext("algo sin resultados", 3), TestContext.Current.CancellationToken));

        events.ShouldNotContain(e => e.Kind == ConversationEventKind.Sources);
        events.ShouldContain(e => e.Kind == ConversationEventKind.Token);
        events[^1].Kind.ShouldBe(ConversationEventKind.Done);
        chat.LastPrompt.ShouldNotBeNull().Input.ShouldContain("ninguno para esta consulta");
    }

    [Fact]
    public async Task HandleAsync_ConRespuestaVaciaOTextoVacio_DegradaSinRomper()
    {
        // Respuesta JSON con "respuesta" vacía → cae al texto crudo, pero igual cita lo relevante.
        CapturingSearchIndex index = new()
        {
            NextResults = [new DatasetSearchHit("aaaa-0001", "Uno", "Cat", "https://x", 0.6)],
        };
        StubChatCompletion chat = new("{\"respuesta\":\"\",\"datasets\":[{\"id\":\"aaaa-0001\",\"relevancia\":0.9}]}");
        DatasetRecommenderAgent agent = new(new StubEmbeddingGenerator(), index, chat);

        List<ConversationEvent> events = await CollectAsync(
            agent.HandleAsync(new ConversationContext("consulta", 3), TestContext.Current.CancellationToken));

        events.ShouldContain(e => e.Kind == ConversationEventKind.Sources);
        events.Where(e => e.Kind == ConversationEventKind.Token).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_IgnoraScoresConIdVacio_YCitaElValido()
    {
        CapturingSearchIndex index = new()
        {
            NextResults = [new DatasetSearchHit("aaaa-0001", "Uno", "Cat", "https://x", 0.6)],
        };
        StubChatCompletion chat = new(
            "{\"respuesta\":\"ok\",\"datasets\":[{\"id\":\"\",\"relevancia\":0.9},{\"id\":\"aaaa-0001\",\"relevancia\":0.9}]}");
        DatasetRecommenderAgent agent = new(new StubEmbeddingGenerator(), index, chat);

        List<ConversationEvent> events = await CollectAsync(
            agent.HandleAsync(new ConversationContext("consulta", 3), TestContext.Current.CancellationToken));

        Citation citation = events.Single(e => e.Kind == ConversationEventKind.Sources)
            .Sources.ShouldNotBeNull().ShouldHaveSingleItem();
        citation.DatasetId.ShouldBe("aaaa-0001");
    }

    [Fact]
    public async Task HandleAsync_ConTextoVacioDelLlm_NoRompe_YNoCita()
    {
        StubChatCompletion chat = new(string.Empty);
        DatasetRecommenderAgent agent = new(new StubEmbeddingGenerator(), new CapturingSearchIndex(), chat);

        List<ConversationEvent> events = await CollectAsync(
            agent.HandleAsync(new ConversationContext("consulta", 3), TestContext.Current.CancellationToken));

        events.ShouldNotContain(e => e.Kind == ConversationEventKind.Sources);
        events[^1].Kind.ShouldBe(ConversationEventKind.Done);
    }

    [Fact]
    public async Task HandleAsync_ConDatasetSinCategoriaNiUrl_LoEnviaComoNd_YLoCitaSiEsRelevante()
    {
        CapturingSearchIndex index = new()
        {
            NextResults = [new DatasetSearchHit("aaaa-0002", "Dataset sin metadatos", null, null, 0.4)],
        };
        StubChatCompletion chat = new(Json("Sirve este.", ("aaaa-0002", 0.8)));
        DatasetRecommenderAgent agent = new(new StubEmbeddingGenerator(), index, chat);

        List<ConversationEvent> events = await CollectAsync(
            agent.HandleAsync(new ConversationContext("consulta", 3), TestContext.Current.CancellationToken));

        events.ShouldContain(e => e.Kind == ConversationEventKind.Sources);
        chat.LastPrompt.ShouldNotBeNull().Input.ShouldContain("n/d");
    }

    [Fact]
    public async Task HandleAsync_ConRespuestaNoJson_DegradaAlTextoSinCitar()
    {
        CapturingSearchIndex index = new()
        {
            NextResults = [new DatasetSearchHit("aaaa-0001", "Uno", "Cat", "https://x", 0.9)],
        };
        StubChatCompletion chat = new("esto no es json");
        DatasetRecommenderAgent agent = new(new StubEmbeddingGenerator(), index, chat);

        List<ConversationEvent> events = await CollectAsync(
            agent.HandleAsync(new ConversationContext("consulta", 3), TestContext.Current.CancellationToken));

        events.ShouldNotContain(e => e.Kind == ConversationEventKind.Sources);
        events.Where(e => e.Kind == ConversationEventKind.Token).ShouldNotBeEmpty();
        events[^1].Kind.ShouldBe(ConversationEventKind.Done);
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
