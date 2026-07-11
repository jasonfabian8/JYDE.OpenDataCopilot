using System.Globalization;
using JYDE.OpenDataCopilot.Application.Conversation;
using JYDE.OpenDataCopilot.Application.Search;
using JYDE.OpenDataCopilot.Application.Tests.Catalog;
using JYDE.OpenDataCopilot.Application.Tests.Search;
using JYDE.OpenDataCopilot.Domain.Catalog;
using Shouldly;

namespace JYDE.OpenDataCopilot.Application.Tests.Conversation;

/// <summary>Pruebas del <see cref="DatasetAnalystAgent"/> (describir columnas + evaluar cruces).</summary>
public sealed class DatasetAnalystAgentTests
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

    private static string ReplyJson(string respuesta, params (string Id, double Relevancia)[] datasets)
    {
        string items = string.Join(
            ",",
            datasets.Select(dataset =>
                $"{{\"id\":\"{dataset.Id}\",\"relevancia\":{dataset.Relevancia.ToString(CultureInfo.InvariantCulture)}}}"));
        return $"{{\"respuesta\":\"{respuesta}\",\"datasets\":[{items}]}}";
    }

    private static DatasetColumn Col(string name, string type, string? description) =>
        new(name, name.ToLowerInvariant(), type, description);

    private static Dataset DatasetWith(string id, string name, string category, params DatasetColumn[] columns) =>
        new(new DatasetId(id), name, new DatasetMetadata(
            category: category,
            columns: columns,
            sourceUrl: new Uri($"https://datos.gov.co/d/{id}")));

    private static async Task<(DatasetAnalystAgent Agent, StubChatCompletion Chat)> BuildAsync(
        string chatText,
        IReadOnlyList<DatasetSearchHit> hits,
        params Dataset[] stored)
    {
        InMemoryCatalogRepository repository = new();
        await repository.SaveAsync(stored, CancellationToken.None);
        CapturingSearchIndex index = new() { NextResults = hits };
        StubChatCompletion chat = new(chatText);
        return (new DatasetAnalystAgent(new StubEmbeddingGenerator(), index, repository, chat), chat);
    }

    private static DatasetSearchHit Hit(string id) => new(id, $"Dataset {id}", "Cat", $"https://datos.gov.co/d/{id}", 0.6);

    [Fact]
    public async Task HandleAsync_DescribeColumnas_CitaElDataset_YEnviaElEsquemaAlLlm()
    {
        Dataset mortalidad = DatasetWith("aaaa-0001", "Causas de mortalidad 2020", "Salud y Protección Social",
            Col("Género", "Text", "Si es Masculino o Femenino"),
            Col("Causas de mortalidad", "Text", null),
            Col("Edad", "Number", "Expresas en años"));
        (DatasetAnalystAgent agent, StubChatCompletion chat) = await BuildAsync(
            ReplyJson("Estas son las columnas del dataset.", ("aaaa-0001", 0.9)), [Hit("aaaa-0001")], mortalidad);

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("listame las columnas de Causas de mortalidad 2020", 5), TestContext.Current.CancellationToken));

        events.Single(e => e.Kind == ConversationEventKind.Sources)
            .Sources.ShouldNotBeNull().ShouldHaveSingleItem().DatasetId.ShouldBe("aaaa-0001");
        events.Where(e => e.Kind == ConversationEventKind.Token).ShouldNotBeEmpty();
        chat.LastPrompt.ShouldNotBeNull();
        chat.LastPrompt.Input.ShouldContain("columnas:");
        chat.LastPrompt.Input.ShouldContain("Género (Text) — Si es Masculino o Femenino");
        chat.LastPrompt.Input.ShouldContain("Causas de mortalidad (Text) — sin descripción");
    }

    [Fact]
    public async Task HandleAsync_EvaluaCruce_ConDosDatasets_CitaAmbos_YEnviaSusColumnas()
    {
        Dataset mortalidad = DatasetWith("aaaa-0003", "Mortalidad por municipio", "Salud",
            Col("Municipio", "Text", "Nombre del municipio"), Col("Muertes", "Number", "Total"));
        Dataset desercion = DatasetWith("aaaa-0004", "Deserción por municipio", "Educación",
            Col("Municipio", "Text", "Nombre del municipio"), Col("Desertores", "Number", "Total"));
        (DatasetAnalystAgent agent, StubChatCompletion chat) = await BuildAsync(
            ReplyJson("Comparten la columna Municipio, puedes cruzarlos.", ("aaaa-0003", 0.9), ("aaaa-0004", 0.85)),
            [Hit("aaaa-0003"), Hit("aaaa-0004")], mortalidad, desercion);

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("puedo cruzar mortalidad con deserción escolar", 5), TestContext.Current.CancellationToken));

        IReadOnlyList<Citation> sources = events.Single(e => e.Kind == ConversationEventKind.Sources).Sources.ShouldNotBeNull();
        sources.Select(c => c.DatasetId).ShouldBe(["aaaa-0003", "aaaa-0004"]);
        chat.LastPrompt.ShouldNotBeNull().Input.ShouldContain("Municipio (Text)");
    }

    [Fact]
    public async Task HandleAsync_OmiteHitsConIdInvalidoONoEnElRepositorio()
    {
        Dataset stored = DatasetWith("aaaa-0001", "Uno", "Cat", Col("A", "Text", "a"));
        (DatasetAnalystAgent agent, StubChatCompletion chat) = await BuildAsync(
            ReplyJson("ok", ("aaaa-0001", 0.9)),
            [Hit("aaaa-0001"), Hit("zzzz-9999"), Hit("bad")], // en repo, válido-no-en-repo, id inválido
            stored);

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("columnas", 5), TestContext.Current.CancellationToken));

        events.Single(e => e.Kind == ConversationEventKind.Sources)
            .Sources.ShouldNotBeNull().ShouldHaveSingleItem().DatasetId.ShouldBe("aaaa-0001");
        chat.LastPrompt.ShouldNotBeNull().Input.ShouldContain("[id=aaaa-0001]");
        chat.LastPrompt.Input.ShouldNotContain("zzzz-9999");
    }

    [Fact]
    public async Task HandleAsync_SinCandidatos_LoDeclara_SinFuentes()
    {
        // JSON sin la clave "datasets" (Datasets = null) e índice vacío.
        (DatasetAnalystAgent agent, StubChatCompletion chat) = await BuildAsync(
            "{\"respuesta\":\"No encontré datasets para describir.\"}", []);

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("columnas de algo inexistente", 5), TestContext.Current.CancellationToken));

        events.ShouldNotContain(e => e.Kind == ConversationEventKind.Sources);
        events.Where(e => e.Kind == ConversationEventKind.Token).ShouldNotBeEmpty();
        chat.LastPrompt.ShouldNotBeNull().Input.ShouldContain("ninguno encontrado para esta consulta");
    }

    [Fact]
    public async Task HandleAsync_DatasetSinColumnas_LoIndica()
    {
        Dataset sinColumnas = new(new DatasetId("aaaa-0005"), "Sin columnas");
        (DatasetAnalystAgent agent, StubChatCompletion chat) = await BuildAsync(
            ReplyJson("ok", ("aaaa-0005", 0.9)), [Hit("aaaa-0005")], sinColumnas);

        await CollectAsync(agent.HandleAsync(new ConversationContext("columnas", 5), TestContext.Current.CancellationToken));

        chat.LastPrompt.ShouldNotBeNull().Input.ShouldContain("el catálogo no expone columnas");
    }

    [Fact]
    public async Task HandleAsync_ConRespuestaVaciaYScoreIdVacio_DegradaYCitaSoloElValido()
    {
        Dataset stored = DatasetWith("aaaa-0001", "Uno", "Cat", Col("A", "Text", "a"));
        (DatasetAnalystAgent agent, _) = await BuildAsync(
            "{\"respuesta\":\"\",\"datasets\":[{\"id\":\"\",\"relevancia\":0.9},{\"id\":\"aaaa-0001\",\"relevancia\":0.9}]}",
            [Hit("aaaa-0001")], stored);

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("columnas", 5), TestContext.Current.CancellationToken));

        events.Single(e => e.Kind == ConversationEventKind.Sources)
            .Sources.ShouldNotBeNull().ShouldHaveSingleItem().DatasetId.ShouldBe("aaaa-0001");
        events.Where(e => e.Kind == ConversationEventKind.Token).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ConRespuestaNoJson_DegradaSinCitar()
    {
        Dataset stored = DatasetWith("aaaa-0001", "Uno", "Cat", Col("A", "Text", "a"));
        (DatasetAnalystAgent agent, _) = await BuildAsync("esto no es json", [Hit("aaaa-0001")], stored);

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("columnas", 5), TestContext.Current.CancellationToken));

        events.ShouldNotContain(e => e.Kind == ConversationEventKind.Sources);
        events.Where(e => e.Kind == ConversationEventKind.Token).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ContinuaElHilo_PasandoElIdPrevio()
    {
        Dataset stored = DatasetWith("aaaa-0001", "Uno", "Cat", Col("A", "Text", "a"));
        (DatasetAnalystAgent agent, StubChatCompletion chat) = await BuildAsync(
            ReplyJson("ok", ("aaaa-0001", 0.9)), [Hit("aaaa-0001")], stored);

        await CollectAsync(agent.HandleAsync(
            new ConversationContext("columnas", 5, "id-previo"), TestContext.Current.CancellationToken));

        chat.LastPrompt.ShouldNotBeNull().PreviousResponseId.ShouldBe("id-previo");
    }

    [Fact]
    public void CanHandle_DetectaColumnasYCruces()
    {
        DatasetAnalystAgent agent = new(
            new StubEmbeddingGenerator(), new CapturingSearchIndex(), new InMemoryCatalogRepository(), new StubChatCompletion());

        agent.CanHandle("listame las columnas de X").ShouldBeTrue();
        agent.CanHandle("puedo cruzar A con B").ShouldBeTrue();
        agent.CanHandle("hay correlación entre A y B").ShouldBeTrue();
        agent.CanHandle("recomiéndame datasets de salud").ShouldBeFalse();
        agent.CanHandle("hola, cómo estás").ShouldBeFalse();
    }

    [Fact]
    public async Task Metadatos_YConstructor()
    {
        StubEmbeddingGenerator embeddings = new();
        CapturingSearchIndex index = new();
        InMemoryCatalogRepository repository = new();
        StubChatCompletion chat = new();

        DatasetAnalystAgent agent = new(embeddings, index, repository, chat);
        agent.Name.ShouldBe("dataset-analyst-agent");
        agent.Description.ShouldNotBeNullOrWhiteSpace();

        Should.Throw<ArgumentNullException>(() => new DatasetAnalystAgent(null!, index, repository, chat));
        Should.Throw<ArgumentNullException>(() => new DatasetAnalystAgent(embeddings, null!, repository, chat));
        Should.Throw<ArgumentNullException>(() => new DatasetAnalystAgent(embeddings, index, null!, chat));
        Should.Throw<ArgumentNullException>(() => new DatasetAnalystAgent(embeddings, index, repository, null!));
        await Should.ThrowAsync<ArgumentNullException>(
            async () => await CollectAsync(agent.HandleAsync(null!, TestContext.Current.CancellationToken)));
    }
}
