using JYDE.OpenDataCopilot.Api.Controllers;
using JYDE.OpenDataCopilot.Api.Conversation;
using JYDE.OpenDataCopilot.Api.Tests.Conversation;
using JYDE.OpenDataCopilot.Application.Conversation;
using JYDE.OpenDataCopilot.Infrastructure.Chat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace JYDE.OpenDataCopilot.Api.Tests.Controllers;

/// <summary>Pruebas del <see cref="ChatController"/> (streaming SSE).</summary>
public sealed class ChatControllerTests
{
    private static ConversationEvent[] AllEventKinds() =>
    [
        ConversationEvent.ForAgent("dataset-recommender-agent"),
        ConversationEvent.ForSources([new Citation("aaaa-0001", "Cobertura de salud", "https://datos.gov.co/d/aaaa-0001", 0.9)]),
        ConversationEvent.ForCategories("salud", [new CategoryRecommendation("Salud y Protección Social", 1312, false, 0.9)]),
        ConversationEvent.ForObjective("analizar la mortalidad"),
        ConversationEvent.ForAudit([new AgentInteraction("router-agent", "entrada", "salida")]),
        ConversationEvent.ForTable(new TableArtifact("Mortalidad", ["genero", "total"], [["Masculino", "120"]])),
        ConversationEvent.ForChart(new ChartArtifact("Mortalidad", "bar", "genero", "total")),
        ConversationEvent.ForToken("hola"),
        ConversationEvent.ForConversation("resp-1"),
        ConversationEvent.Completed(),
    ];

    private static ChatController BuildController(MemoryStream responseBody, params ConversationEvent[] events)
    {
        FixedEventsAgent agent = new(events.Length > 0 ? events : AllEventKinds());
        CopilotOrchestrator orchestrator = new(
            [agent], new DefaultAgentRouter(), new ObjectiveTracker(new FakeChatCompletion()), new InteractionRecorder());

        DefaultHttpContext httpContext = new();
        httpContext.Response.Body = responseBody;
        return new ChatController(orchestrator)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
    }

    [Fact]
    public async Task Ask_MapeaTodosLosTiposDeEventoSSE()
    {
        using MemoryStream body = new();
        ChatController controller = BuildController(body);

        IActionResult result = await controller.Ask(new ChatRequest("salud", 3), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<EmptyResult>();
        body.Position = 0;
        string sse = await new StreamReader(body).ReadToEndAsync(TestContext.Current.CancellationToken);
        sse.ShouldContain("event: agent");
        sse.ShouldContain("event: sources");
        sse.ShouldContain("event: categories");
        sse.ShouldContain("event: objective");
        sse.ShouldContain("event: audit");
        sse.ShouldContain("event: table");
        sse.ShouldContain("event: chart");
        sse.ShouldContain("event: token");
        sse.ShouldContain("event: conversation");
        sse.ShouldContain("event: done");
        sse.ShouldContain("Salud y Protección Social");
    }

    [Fact]
    public async Task Ask_ConPreguntaVacia_DevuelveBadRequest()
    {
        using MemoryStream body = new();
        ChatController controller = BuildController(body);

        (await controller.Ask(new ChatRequest("   "), TestContext.Current.CancellationToken))
            .ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Ask_ConCuerpoNulo_DevuelveBadRequest()
    {
        using MemoryStream body = new();
        ChatController controller = BuildController(body);

        (await controller.Ask(null, TestContext.Current.CancellationToken)).ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Ask_ConPreguntaNula_DevuelveBadRequest()
    {
        using MemoryStream body = new();
        ChatController controller = BuildController(body);

        (await controller.Ask(new ChatRequest(null), TestContext.Current.CancellationToken))
            .ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Ask_ConTopNoPositivo_UsaPorDefecto_YResponde()
    {
        using MemoryStream body = new();
        ChatController controller = BuildController(body);

        (await controller.Ask(new ChatRequest("salud", 0), TestContext.Current.CancellationToken))
            .ShouldBeOfType<EmptyResult>();
    }

    [Fact]
    public async Task Ask_ConConversationId_ContinuaElHilo_YResponde()
    {
        using MemoryStream body = new();
        ChatController controller = BuildController(body);

        IActionResult result = await controller.Ask(
            new ChatRequest("salud", 3, "resp-previo"), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<EmptyResult>();
        body.Position = 0;
        string sse = await new StreamReader(body).ReadToEndAsync(TestContext.Current.CancellationToken);
        sse.ShouldContain("event: done");
    }
}
