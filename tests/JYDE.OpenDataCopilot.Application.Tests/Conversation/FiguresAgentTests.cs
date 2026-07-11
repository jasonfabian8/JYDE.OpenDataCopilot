using JYDE.OpenDataCopilot.Application.Conversation;
using JYDE.OpenDataCopilot.Application.Figures;
using JYDE.OpenDataCopilot.Application.Search;
using JYDE.OpenDataCopilot.Application.Tests.Catalog;
using JYDE.OpenDataCopilot.Application.Tests.Search;
using JYDE.OpenDataCopilot.Domain.Catalog;
using Shouldly;

namespace JYDE.OpenDataCopilot.Application.Tests.Conversation;

/// <summary>Pruebas del <see cref="FiguresAgent"/> (SoQL → tabla + gráfico).</summary>
public sealed class FiguresAgentTests
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

    private static Dataset DatasetWith(string id, string name, params DatasetColumn[] columns) =>
        new(new DatasetId(id), name, new DatasetMetadata(
            category: "Salud", columns: columns, sourceUrl: new Uri($"https://datos.gov.co/d/{id}")));

    private static DatasetSearchHit Hit(string id, string name) => new(id, name, "Salud", $"https://datos.gov.co/d/{id}", 0.7);

    private static async Task<FiguresAgent> BuildAsync(string chatText, IDataQuery dataQuery, params Dataset[] stored)
    {
        InMemoryCatalogRepository repository = new();
        await repository.SaveAsync(stored, CancellationToken.None);
        CapturingSearchIndex index = new()
        {
            NextResults = [.. stored.Select(dataset => Hit(dataset.Id.Value, dataset.Name))],
        };
        return new FiguresAgent(new StubEmbeddingGenerator(), index, repository, new StubChatCompletion(chatText), dataQuery);
    }

    [Fact]
    public async Task HandleAsync_EjecutaSoql_EmiteTablaGraficoYCita()
    {
        Dataset dataset = DatasetWith("aaaa-0001", "Causas de mortalidad 2020",
            new DatasetColumn("Género", "genero", "text"), new DatasetColumn("Edad", "edad", "number"));
        DataQueryResult data = new(["genero", "total"], [["Masculino", "120"], ["Femenino", "98"]]);
        StubDataQuery query = new(data);
        FiguresAgent agent = await BuildAsync(
            "{\"datasetId\":\"aaaa-0001\",\"soql\":\"SELECT genero, count(*) AS total GROUP BY genero LIMIT 50\",\"explicacion\":\"Muertes por género.\",\"chart\":{\"tipo\":\"bar\",\"x\":\"genero\",\"y\":\"total\"}}",
            query, dataset);

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("cuántas muertes por género", 5), TestContext.Current.CancellationToken));

        events.Single(e => e.Kind == ConversationEventKind.Sources).Sources.ShouldNotBeNull().ShouldHaveSingleItem()
            .DatasetId.ShouldBe("aaaa-0001");
        TableArtifact table = events.Single(e => e.Kind == ConversationEventKind.Table).Table.ShouldNotBeNull();
        table.Columns.ShouldBe(["genero", "total"]);
        table.Rows.Count.ShouldBe(2);
        ChartArtifact chart = events.Single(e => e.Kind == ConversationEventKind.Chart).Chart.ShouldNotBeNull();
        chart.Type.ShouldBe("bar");
        chart.XColumn.ShouldBe("genero");
        chart.YColumn.ShouldBe("total");
        events.Where(e => e.Kind == ConversationEventKind.Token).ShouldNotBeEmpty();
        query.LastSoql.ShouldNotBeNull().ShouldContain("GROUP BY genero");
    }

    [Fact]
    public async Task HandleAsync_ConChartInvalido_NoEmiteGrafico()
    {
        Dataset dataset = DatasetWith("aaaa-0001", "Mortalidad", new DatasetColumn("Género", "genero", "text"));
        DataQueryResult data = new(["genero", "total"], [["M", "1"]]);
        // El gráfico referencia una columna inexistente (edad) → no debe emitirse.
        FiguresAgent agent = await BuildAsync(
            "{\"datasetId\":\"aaaa-0001\",\"soql\":\"SELECT genero, count(*) AS total GROUP BY genero\",\"explicacion\":\"ok\",\"chart\":{\"tipo\":\"bar\",\"x\":\"edad\",\"y\":\"total\"}}",
            new StubDataQuery(data), dataset);

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("cifra por género", 5), TestContext.Current.CancellationToken));

        events.ShouldContain(e => e.Kind == ConversationEventKind.Table);
        events.ShouldNotContain(e => e.Kind == ConversationEventKind.Chart);
    }

    [Fact]
    public async Task HandleAsync_SiLaConsultaFalla_ExplicaSinTabla()
    {
        Dataset dataset = DatasetWith("aaaa-0001", "Mortalidad", new DatasetColumn("Género", "genero", "text"));
        StubDataQuery query = new(error: new HttpRequestException("Bad Request", null, System.Net.HttpStatusCode.BadRequest));
        FiguresAgent agent = await BuildAsync(
            "{\"datasetId\":\"aaaa-0001\",\"soql\":\"SELECT columna_inexistente\",\"explicacion\":\"x\"}", query, dataset);

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("cuántas muertes", 5), TestContext.Current.CancellationToken));

        events.ShouldNotContain(e => e.Kind == ConversationEventKind.Table);
        events.ShouldNotContain(e => e.Kind == ConversationEventKind.Sources);
        string streamed = string.Concat(events.Where(e => e.Kind == ConversationEventKind.Token).Select(e => e.Token ?? string.Empty));
        streamed.ShouldContain("No pude ejecutar la consulta");
    }

    [Fact]
    public async Task HandleAsync_SinDatasets_LoDeclara()
    {
        FiguresAgent agent = await BuildAsync("{\"soql\":\"x\"}", new StubDataQuery());

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("cuántas muertes", 5), TestContext.Current.CancellationToken));

        events.ShouldNotContain(e => e.Kind == ConversationEventKind.Table);
        string streamed = string.Concat(events.Where(e => e.Kind == ConversationEventKind.Token).Select(e => e.Token ?? string.Empty));
        streamed.ShouldContain("No encontré un dataset");
    }

    [Fact]
    public async Task HandleAsync_SinSoqlOSinJson_Degrada()
    {
        Dataset dataset = DatasetWith("aaaa-0001", "Mortalidad", new DatasetColumn("Género", "genero", "text"));
        FiguresAgent agent = await BuildAsync("no es json", new StubDataQuery(), dataset);

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("cuántas", 5), TestContext.Current.CancellationToken));

        events.ShouldNotContain(e => e.Kind == ConversationEventKind.Table);
        events.Where(e => e.Kind == ConversationEventKind.Token).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ConDatasetIdDesconocido_UsaElPrimero()
    {
        Dataset first = DatasetWith("aaaa-0001", "Primero", new DatasetColumn("Género", "genero", "text"));
        StubDataQuery query = new(new DataQueryResult(["genero"], [["M"]]));
        FiguresAgent agent = await BuildAsync(
            "{\"datasetId\":\"zzzz-9999\",\"soql\":\"SELECT genero\",\"explicacion\":\"x\"}", query, first);

        await CollectAsync(agent.HandleAsync(new ConversationContext("cifra", 5), TestContext.Current.CancellationToken));

        query.LastDatasetId.ShouldBe("aaaa-0001");
    }

    [Fact]
    public async Task HandleAsync_ConSoqlVacioYExplicacion_UsaLaExplicacion()
    {
        Dataset dataset = DatasetWith("aaaa-0001", "Uno", new DatasetColumn("A", "a", "text"));
        FiguresAgent agent = await BuildAsync(
            "{\"soql\":\"\",\"explicacion\":\"Necesito que precises la variable a contar.\"}", new StubDataQuery(), dataset);

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("cifra", 5), TestContext.Current.CancellationToken));

        events.ShouldNotContain(e => e.Kind == ConversationEventKind.Table);
        string streamed = string.Concat(events.Where(e => e.Kind == ConversationEventKind.Token).Select(e => e.Token ?? string.Empty));
        streamed.ShouldContain("Necesito que precises");
    }

    [Fact]
    public async Task HandleAsync_ConExplicacionVacia_UsaTextoPorDefecto()
    {
        Dataset dataset = DatasetWith("aaaa-0001", "Uno", new DatasetColumn("A", "a", "text"));
        StubDataQuery query = new(new DataQueryResult(["a"], [["1"]]));
        FiguresAgent agent = await BuildAsync(
            "{\"datasetId\":\"aaaa-0001\",\"soql\":\"SELECT a\",\"explicacion\":\"\"}", query, dataset);

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("cifra", 5), TestContext.Current.CancellationToken));

        events.ShouldContain(e => e.Kind == ConversationEventKind.Table);
        string streamed = string.Concat(events.Where(e => e.Kind == ConversationEventKind.Token).Select(e => e.Token ?? string.Empty));
        streamed.ShouldContain("Aquí están los datos");
    }

    [Fact]
    public async Task HandleAsync_ConTipoDeGraficoInvalido_NoEmiteGrafico()
    {
        Dataset dataset = DatasetWith("aaaa-0001", "Uno", new DatasetColumn("A", "a", "text"));
        StubDataQuery query = new(new DataQueryResult(["genero", "total"], [["M", "1"]]));
        FiguresAgent agent = await BuildAsync(
            "{\"datasetId\":\"aaaa-0001\",\"soql\":\"SELECT genero\",\"explicacion\":\"ok\",\"chart\":{\"tipo\":\"pie\",\"x\":\"genero\",\"y\":\"total\"}}",
            query, dataset);

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("gráfico", 5), TestContext.Current.CancellationToken));

        events.ShouldContain(e => e.Kind == ConversationEventKind.Table);
        events.ShouldNotContain(e => e.Kind == ConversationEventKind.Chart);
    }

    [Fact]
    public async Task HandleAsync_OmiteHitsInvalidosONoEnRepo()
    {
        Dataset stored = DatasetWith("aaaa-0001", "Uno", new DatasetColumn("A", "a", "text"));
        InMemoryCatalogRepository repository = new();
        await repository.SaveAsync([stored], CancellationToken.None);
        CapturingSearchIndex index = new()
        {
            NextResults = [Hit("bad", "x"), Hit("zzzz-9999", "y"), Hit("aaaa-0001", "Uno")],
        };
        StubDataQuery query = new(new DataQueryResult(["a"], [["1"]]));
        FiguresAgent agent = new(
            new StubEmbeddingGenerator(), index, repository,
            new StubChatCompletion("{\"datasetId\":\"aaaa-0001\",\"soql\":\"SELECT a\",\"explicacion\":\"ok\"}"), query);

        await CollectAsync(agent.HandleAsync(new ConversationContext("cifra", 5), TestContext.Current.CancellationToken));

        query.LastDatasetId.ShouldBe("aaaa-0001");
    }

    [Fact]
    public void CanHandle_DetectaCifrasYGraficos()
    {
        FiguresAgent agent = new(
            new StubEmbeddingGenerator(), new CapturingSearchIndex(), new InMemoryCatalogRepository(),
            new StubChatCompletion(), new StubDataQuery());

        agent.CanHandle("cuántas muertes hay").ShouldBeTrue();
        agent.CanHandle("hazme un gráfico de barras").ShouldBeTrue();
        agent.CanHandle("tabula el total por año").ShouldBeTrue();
        agent.CanHandle("hola, cómo estás").ShouldBeFalse();
    }

    [Fact]
    public async Task Metadatos_YConstructor()
    {
        StubEmbeddingGenerator embeddings = new();
        CapturingSearchIndex index = new();
        InMemoryCatalogRepository repository = new();
        StubChatCompletion chat = new();
        StubDataQuery query = new();

        FiguresAgent agent = new(embeddings, index, repository, chat, query);
        agent.Name.ShouldBe("figures-agent");
        agent.Description.ShouldNotBeNullOrWhiteSpace();

        Should.Throw<ArgumentNullException>(() => new FiguresAgent(null!, index, repository, chat, query));
        Should.Throw<ArgumentNullException>(() => new FiguresAgent(embeddings, null!, repository, chat, query));
        Should.Throw<ArgumentNullException>(() => new FiguresAgent(embeddings, index, null!, chat, query));
        Should.Throw<ArgumentNullException>(() => new FiguresAgent(embeddings, index, repository, null!, query));
        Should.Throw<ArgumentNullException>(() => new FiguresAgent(embeddings, index, repository, chat, null!));
        await Should.ThrowAsync<ArgumentNullException>(
            async () => await CollectAsync(agent.HandleAsync(null!, TestContext.Current.CancellationToken)));
    }
}
