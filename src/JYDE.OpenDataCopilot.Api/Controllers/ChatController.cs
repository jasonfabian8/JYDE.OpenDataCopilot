using System.Text.Json;
using JYDE.OpenDataCopilot.Api.Conversation;
using JYDE.OpenDataCopilot.Application.Conversation;
using Microsoft.AspNetCore.Mvc;

namespace JYDE.OpenDataCopilot.Api.Controllers;

/// <summary>Endpoint del Copilot conversacional (streaming SSE) del bounded context <c>Conversation</c>.</summary>
[ApiController]
[Route("chat")]
public sealed class ChatController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly CopilotOrchestrator _orchestrator;

    /// <summary>Crea el controlador de chat.</summary>
    /// <param name="orchestrator">Copilot orquestador.</param>
    public ChatController(CopilotOrchestrator orchestrator) => _orchestrator = orchestrator;

    /// <summary>Conversa con el Copilot. Responde como flujo de eventos SSE (agent, sources, token, done).</summary>
    /// <param name="request">Pregunta del usuario y parámetros opcionales.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] ChatRequest? request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest("La pregunta no puede estar vacía.");
        }

        string question = request.Question.Trim();
        int topK = request.Top is int value && value > 0 ? value : CopilotOrchestrator.DefaultTopK;
        string? conversationId = string.IsNullOrWhiteSpace(request.ConversationId) ? null : request.ConversationId;

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";

        await foreach (ConversationEvent conversationEvent in _orchestrator.AskAsync(question, topK, conversationId, cancellationToken))
        {
            await WriteEventAsync(conversationEvent, cancellationToken);
        }

        return new EmptyResult();
    }

    private async Task WriteEventAsync(ConversationEvent conversationEvent, CancellationToken cancellationToken)
    {
        string name = conversationEvent.Kind.ToString().ToLowerInvariant();
        object payload = conversationEvent.Kind switch
        {
            ConversationEventKind.Agent => new { agent = conversationEvent.Agent },
            ConversationEventKind.Sources => new { sources = conversationEvent.Sources },
            ConversationEventKind.Token => new { text = conversationEvent.Token },
            ConversationEventKind.Conversation => new { conversationId = conversationEvent.ConversationId },
            _ => new { },
        };

        string data = JsonSerializer.Serialize(payload, JsonOptions);
        await Response.WriteAsync($"event: {name}\ndata: {data}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
