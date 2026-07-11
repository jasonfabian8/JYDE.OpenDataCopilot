using JYDE.OpenDataCopilot.Api.Controllers;
using JYDE.OpenDataCopilot.Api.Conversation;
using JYDE.OpenDataCopilot.Application.Conversation;
using JYDE.OpenDataCopilot.Application.Search;
using JYDE.OpenDataCopilot.Infrastructure.Chat;
using JYDE.OpenDataCopilot.Infrastructure.Embeddings;
using JYDE.OpenDataCopilot.Infrastructure.Search;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace JYDE.OpenDataCopilot.Api.Tests.Controllers;

/// <summary>Pruebas del <see cref="ChatController"/> (streaming SSE).</summary>
public sealed class ChatControllerTests
{
    private static async Task<ChatController> BuildControllerAsync(MemoryStream responseBody)
    {
        LocalHashingEmbeddingGenerator embeddings = new();
        InMemorySearchIndex index = new();
        IReadOnlyList<float> vector = await embeddings.GenerateAsync("salud", TestContext.Current.CancellationToken);
        await index.IndexAsync(
            [new DatasetVector("aaaa-0001", "Cobertura de salud", "Salud", "https://datos.gov.co/d/aaaa-0001", vector)],
            TestContext.Current.CancellationToken);

        DatasetRecommenderAgent agent = new(embeddings, index, new FakeChatCompletion());
        CopilotOrchestrator orchestrator = new([agent], new DefaultAgentRouter());

        DefaultHttpContext httpContext = new();
        httpContext.Response.Body = responseBody;
        return new ChatController(orchestrator)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
    }

    [Fact]
    public async Task Ask_EmiteEventosSSE()
    {
        using MemoryStream body = new();
        ChatController controller = await BuildControllerAsync(body);

        IActionResult result = await controller.Ask(new ChatRequest("salud", 3), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<EmptyResult>();
        body.Position = 0;
        string sse = await new StreamReader(body).ReadToEndAsync(TestContext.Current.CancellationToken);
        sse.ShouldContain("event: agent");
        sse.ShouldContain("event: sources");
        sse.ShouldContain("event: token");
        sse.ShouldContain("event: conversation");
        sse.ShouldContain("event: done");
    }

    [Fact]
    public async Task Ask_ConPreguntaVacia_DevuelveBadRequest()
    {
        using MemoryStream body = new();
        ChatController controller = await BuildControllerAsync(body);

        IActionResult result = await controller.Ask(new ChatRequest("   "), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Ask_ConCuerpoNulo_DevuelveBadRequest()
    {
        using MemoryStream body = new();
        ChatController controller = await BuildControllerAsync(body);

        IActionResult result = await controller.Ask(null, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Ask_ConPreguntaNula_DevuelveBadRequest()
    {
        using MemoryStream body = new();
        ChatController controller = await BuildControllerAsync(body);

        IActionResult result = await controller.Ask(new ChatRequest(null), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Ask_ConTopNoPositivo_UsaPorDefecto_YResponde()
    {
        using MemoryStream body = new();
        ChatController controller = await BuildControllerAsync(body);

        IActionResult result = await controller.Ask(new ChatRequest("salud", 0), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<EmptyResult>();
    }

    [Fact]
    public async Task Ask_ConConversationId_ContinuaElHilo_YResponde()
    {
        using MemoryStream body = new();
        ChatController controller = await BuildControllerAsync(body);

        IActionResult result = await controller.Ask(
            new ChatRequest("salud", 3, "resp-previo"), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<EmptyResult>();
        body.Position = 0;
        string sse = await new StreamReader(body).ReadToEndAsync(TestContext.Current.CancellationToken);
        sse.ShouldContain("event: done");
    }
}
