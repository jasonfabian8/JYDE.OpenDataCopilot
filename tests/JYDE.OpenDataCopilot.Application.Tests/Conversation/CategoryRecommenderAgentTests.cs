using System.Globalization;
using JYDE.OpenDataCopilot.Application.Catalog;
using JYDE.OpenDataCopilot.Application.Conversation;
using JYDE.OpenDataCopilot.Application.Tests.Catalog;
using JYDE.OpenDataCopilot.Domain.Catalog;
using Shouldly;

namespace JYDE.OpenDataCopilot.Application.Tests.Conversation;

/// <summary>Pruebas del <see cref="CategoryRecommenderAgent"/>.</summary>
public sealed class CategoryRecommenderAgentTests
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

    private static string CategoryJson(string respuesta, string consulta, params (string Nombre, double Relevancia)[] categorias)
    {
        string items = string.Join(
            ",",
            categorias.Select(category =>
                $"{{\"nombre\":\"{category.Nombre}\",\"relevancia\":{category.Relevancia.ToString(CultureInfo.InvariantCulture)}}}"));
        return $"{{\"respuesta\":\"{respuesta}\",\"consulta\":\"{consulta}\",\"categorias\":[{items}]}}";
    }

    private static FakeCatalogSource Source(params (string Name, int Count)[] categories) =>
        new([]) { Categories = [.. categories.Select(category => new CatalogCategory(category.Name, category.Count))] };

    private static async Task<InMemoryCatalogRepository> RepoWithLoadedAsync(params string[] loadedCategories)
    {
        InMemoryCatalogRepository repository = new();
        int index = 1;
        foreach (string category in loadedCategories)
        {
            await repository.SaveAsync(
                [new Dataset(new DatasetId($"aaaa-{index:D4}"), $"D{index}", new DatasetMetadata(category: category))],
                CancellationToken.None);
            index++;
        }

        return repository;
    }

    [Fact]
    public async Task HandleAsync_RecomiendaCategoriasRelevantesNoCargadas_ConConsultaYConteo()
    {
        FakeCatalogSource source = Source(("Salud y Protección Social", 1312), ("Transporte", 261));
        InMemoryCatalogRepository repository = await RepoWithLoadedAsync();
        StubChatCompletion chat = new(CategoryJson(
            "Carga Salud.", "suicidio juvenil", ("Salud y Protección Social", 0.9), ("Transporte", 0.1)));
        CategoryRecommenderAgent agent = new(source, repository, chat);

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("recomiéndame categorías para suicidio juvenil", 5), TestContext.Current.CancellationToken));

        ConversationEvent categories = events.Single(e => e.Kind == ConversationEventKind.Categories);
        categories.Query.ShouldBe("suicidio juvenil");
        CategoryRecommendation single = categories.Categories.ShouldNotBeNull().ShouldHaveSingleItem();
        single.Name.ShouldBe("Salud y Protección Social");
        single.Count.ShouldBe(1312);
        single.Loaded.ShouldBeFalse();
        events.Where(e => e.Kind == ConversationEventKind.Token).ShouldNotBeEmpty();
        events[^1].Kind.ShouldBe(ConversationEventKind.Done);
    }

    [Fact]
    public async Task HandleAsync_MarcaComoCargadaLaCategoriaYaPresente()
    {
        FakeCatalogSource source = Source(("Transporte", 261));
        InMemoryCatalogRepository repository = await RepoWithLoadedAsync("Transporte");
        StubChatCompletion chat = new(CategoryJson("Ya tienes Transporte.", "accidentalidad vial", ("Transporte", 0.9)));
        CategoryRecommenderAgent agent = new(source, repository, chat);

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("categorías para accidentalidad vial", 5), TestContext.Current.CancellationToken));

        CategoryRecommendation single = events.Single(e => e.Kind == ConversationEventKind.Categories)
            .Categories.ShouldNotBeNull().ShouldHaveSingleItem();
        single.Name.ShouldBe("Transporte");
        single.Loaded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_EnviaLaListaConEstadoCargada_AlLlm()
    {
        FakeCatalogSource source = Source(("Transporte", 261), ("Salud", 1312));
        InMemoryCatalogRepository repository = await RepoWithLoadedAsync("Transporte");
        StubChatCompletion chat = new(CategoryJson("ok", "x", ("Salud", 0.9)));
        CategoryRecommenderAgent agent = new(source, repository, chat);

        await CollectAsync(agent.HandleAsync(new ConversationContext("categorías", 5), TestContext.Current.CancellationToken));

        chat.LastPrompt.ShouldNotBeNull();
        chat.LastPrompt.Input.ShouldContain("Transporte");
        chat.LastPrompt.Input.ShouldContain("CARGADA");
        chat.LastPrompt.Input.ShouldContain("sin cargar");
    }

    [Fact]
    public async Task HandleAsync_IgnoraCategoriasBajoUmbralODesconocidas()
    {
        FakeCatalogSource source = Source(("Salud", 1312));
        InMemoryCatalogRepository repository = await RepoWithLoadedAsync();
        // Salud (bajo umbral), "Inexistente" (fuera de la lista) y "" (nombre vacío) → sin recomendaciones.
        StubChatCompletion chat = new(CategoryJson("nada relevante", "x", ("Salud", 0.2), ("Inexistente", 0.9), ("", 0.9)));
        CategoryRecommenderAgent agent = new(source, repository, chat);

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("categorías", 5), TestContext.Current.CancellationToken));

        events.ShouldNotContain(e => e.Kind == ConversationEventKind.Categories);
        events.Where(e => e.Kind == ConversationEventKind.Token).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ConRespuestaYConsultaVacias_UsaTextoCrudoYLaPregunta()
    {
        FakeCatalogSource source = Source(("Salud", 1312));
        InMemoryCatalogRepository repository = await RepoWithLoadedAsync();
        StubChatCompletion chat = new(
            "{\"respuesta\":\"\",\"consulta\":\"\",\"categorias\":[{\"nombre\":\"Salud\",\"relevancia\":0.9}]}");
        CategoryRecommenderAgent agent = new(source, repository, chat);

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("mi consulta original", 5), TestContext.Current.CancellationToken));

        ConversationEvent categories = events.Single(e => e.Kind == ConversationEventKind.Categories);
        categories.Query.ShouldBe("mi consulta original"); // consulta vacía → cae a la pregunta
        categories.Categories.ShouldNotBeNull().ShouldHaveSingleItem().Name.ShouldBe("Salud");
        events.Where(e => e.Kind == ConversationEventKind.Token).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ConRespuestaNoJson_DegradaSinRecomendaciones()
    {
        FakeCatalogSource source = Source(("Salud", 1312));
        InMemoryCatalogRepository repository = await RepoWithLoadedAsync();
        StubChatCompletion chat = new("esto no es json");
        CategoryRecommenderAgent agent = new(source, repository, chat);

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("categorías", 5), TestContext.Current.CancellationToken));

        events.ShouldNotContain(e => e.Kind == ConversationEventKind.Categories);
        events.Where(e => e.Kind == ConversationEventKind.Token).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_SinCategoriasEnLaFuente_YSinClaveCategorias_NoRompe()
    {
        FakeCatalogSource source = Source();
        InMemoryCatalogRepository repository = await RepoWithLoadedAsync();
        // JSON sin "categorias" (Categorias = null) y catálogo vacío.
        StubChatCompletion chat = new("{\"respuesta\":\"no hay lista\",\"consulta\":\"x\"}");
        CategoryRecommenderAgent agent = new(source, repository, chat);

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("categorías", 5), TestContext.Current.CancellationToken));

        events.ShouldNotContain(e => e.Kind == ConversationEventKind.Categories);
        chat.LastPrompt.ShouldNotBeNull().Input.ShouldContain("no se pudo obtener la lista");
    }

    [Fact]
    public async Task HandleAsync_ConCategoriasQueSoloDifierenEnMayusculas_NoLanza()
    {
        // datos.gov.co trae "Participación ciudadana" y "Participación Ciudadana": no debe romper.
        FakeCatalogSource source = Source(("Participación ciudadana", 143), ("Participación Ciudadana", 41));
        InMemoryCatalogRepository repository = await RepoWithLoadedAsync();
        StubChatCompletion chat = new(CategoryJson("Carga esa.", "participación", ("Participación ciudadana", 0.9)));
        CategoryRecommenderAgent agent = new(source, repository, chat);

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("participación", 5), TestContext.Current.CancellationToken));

        CategoryRecommendation single = events.Single(e => e.Kind == ConversationEventKind.Categories)
            .Categories.ShouldNotBeNull().ShouldHaveSingleItem();
        single.Name.ShouldBe("Participación ciudadana");
        single.Count.ShouldBe(143); // conserva la de mayor conteo
    }

    [Fact]
    public async Task HandleAsync_ConTextoVacioDelLlm_NoRompe()
    {
        CategoryRecommenderAgent agent = new(Source(("Salud", 1)), await RepoWithLoadedAsync(), new StubChatCompletion(string.Empty));

        List<ConversationEvent> events = await CollectAsync(agent.HandleAsync(
            new ConversationContext("categorías", 5), TestContext.Current.CancellationToken));

        events.ShouldNotContain(e => e.Kind == ConversationEventKind.Categories);
        events[^1].Kind.ShouldBe(ConversationEventKind.Done);
    }

    [Fact]
    public void CanHandle_DetectaIntencionDeCategorias()
    {
        CategoryRecommenderAgent agent = new(Source(), new InMemoryCatalogRepository(), new StubChatCompletion());

        agent.CanHandle("¿qué categorías me recomiendas?").ShouldBeTrue();
        agent.CanHandle("necesito cargar más datos").ShouldBeTrue();
        agent.CanHandle("hola, ¿cómo estás?").ShouldBeFalse();
    }

    [Fact]
    public async Task Metadatos_YConstructor()
    {
        FakeCatalogSource source = Source();
        InMemoryCatalogRepository repository = new();
        StubChatCompletion chat = new();

        CategoryRecommenderAgent agent = new(source, repository, chat);
        agent.Name.ShouldBe("category-recommender-agent");
        agent.Description.ShouldNotBeNullOrWhiteSpace();

        Should.Throw<ArgumentNullException>(() => new CategoryRecommenderAgent(null!, repository, chat));
        Should.Throw<ArgumentNullException>(() => new CategoryRecommenderAgent(source, null!, chat));
        Should.Throw<ArgumentNullException>(() => new CategoryRecommenderAgent(source, repository, null!));
        await Should.ThrowAsync<ArgumentNullException>(
            async () => await CollectAsync(agent.HandleAsync(null!, TestContext.Current.CancellationToken)));
    }
}
