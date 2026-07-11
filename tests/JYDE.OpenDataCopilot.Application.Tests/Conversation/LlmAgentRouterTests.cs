using JYDE.OpenDataCopilot.Application.Conversation;
using Shouldly;

namespace JYDE.OpenDataCopilot.Application.Tests.Conversation;

/// <summary>Pruebas del enrutador basado en LLM <see cref="LlmAgentRouter"/>.</summary>
public sealed class LlmAgentRouterTests
{
    [Fact]
    public async Task RouteAsync_UsaElAgenteQueEligeElLlm()
    {
        StubAgent dataset = new("dataset-recommender-agent");
        StubAgent category = new("category-recommender-agent");
        StubChatCompletion chat = new("{\"agente\":\"category-recommender-agent\"}");

        (await new LlmAgentRouter(chat).RouteAsync("qué cargo", [dataset, category], cancellationToken: TestContext.Current.CancellationToken))
            .Name.ShouldBe("category-recommender-agent");
    }

    [Fact]
    public async Task RouteAsync_IncluyeLaRespuestaAnteriorEnElInput()
    {
        StubAgent dataset = new("dataset-recommender-agent");
        StubAgent category = new("category-recommender-agent");
        StubChatCompletion chat = new("{\"agente\":\"category-recommender-agent\"}");

        IConversationAgent selected = await new LlmAgentRouter(chat).RouteAsync(
            "sí", [dataset, category], "¿Desea revisar categorías?", TestContext.Current.CancellationToken);

        selected.Name.ShouldBe("category-recommender-agent");
        chat.LastPrompt.ShouldNotBeNull().Input.ShouldContain("Respuesta anterior del Copilot: ¿Desea revisar categorías?");
        chat.LastPrompt.Input.ShouldContain("Mensaje del usuario: sí");
    }

    [Fact]
    public async Task RouteAsync_ConUnSoloAgente_LoDevuelveSinLlamarAlLlm()
    {
        StubAgent only = new("solo");
        StubChatCompletion chat = new("irrelevante");

        (await new LlmAgentRouter(chat).RouteAsync("x", [only], cancellationToken: TestContext.Current.CancellationToken)).Name.ShouldBe("solo");
        chat.LastPrompt.ShouldBeNull();
    }

    [Fact]
    public async Task RouteAsync_ConNombreDesconocido_DegradaAReglas()
    {
        StubAgent a = new("a", canHandle: false);
        StubAgent b = new("b", canHandle: true);

        (await new LlmAgentRouter(new StubChatCompletion("{\"agente\":\"inexistente\"}"))
            .RouteAsync("x", [a, b], cancellationToken: TestContext.Current.CancellationToken)).Name.ShouldBe("b");
    }

    [Fact]
    public async Task RouteAsync_ConAgenteVacioOJsonMalformado_DegradaAReglas()
    {
        StubAgent a = new("a", canHandle: false);
        StubAgent b = new("b", canHandle: true);

        (await new LlmAgentRouter(new StubChatCompletion("{\"agente\":\"\"}"))
            .RouteAsync("x", [a, b], cancellationToken: TestContext.Current.CancellationToken)).Name.ShouldBe("b");
        (await new LlmAgentRouter(new StubChatCompletion("{malformado}"))
            .RouteAsync("x", [a, b], cancellationToken: TestContext.Current.CancellationToken)).Name.ShouldBe("b");
        (await new LlmAgentRouter(new StubChatCompletion("sin llaves"))
            .RouteAsync("x", [a, b], cancellationToken: TestContext.Current.CancellationToken)).Name.ShouldBe("b");
    }

    [Fact]
    public async Task RouteAsync_SiElLlmFalla_DegradaAReglas()
    {
        StubAgent a = new("a", canHandle: false);
        StubAgent b = new("b", canHandle: true);

        (await new LlmAgentRouter(new ThrowingChatCompletion())
            .RouteAsync("x", [a, b], cancellationToken: TestContext.Current.CancellationToken)).Name.ShouldBe("b");
        (await new LlmAgentRouter(new ThrowingChatCompletion(new InvalidOperationException("x")))
            .RouteAsync("x", [a, b], cancellationToken: TestContext.Current.CancellationToken)).Name.ShouldBe("b");
    }

    [Fact]
    public async Task RouteAsync_SinAgentes_Lanza()
    {
        LlmAgentRouter router = new(new StubChatCompletion());

        await Should.ThrowAsync<InvalidOperationException>(() => router.RouteAsync("x", []));
        await Should.ThrowAsync<ArgumentNullException>(() => router.RouteAsync("x", null!));
    }

    [Fact]
    public void Constructor_ConChatNulo_Lanza()
    {
        Should.Throw<ArgumentNullException>(() => new LlmAgentRouter(null!));
    }
}
