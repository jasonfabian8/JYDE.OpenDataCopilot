using JYDE.OpenDataCopilot.Application.Conversation;

namespace JYDE.OpenDataCopilot.Application.Tests.Conversation;

/// <summary>Doble de prueba de <see cref="IChatCompletion"/> que devuelve un resultado predefinido.</summary>
internal sealed class StubChatCompletion : IChatCompletion
{
    private readonly string _text;
    private readonly string? _responseId;

    public StubChatCompletion(string text = "respuesta de prueba", string? responseId = "stub-response-id")
    {
        _text = text;
        _responseId = responseId;
    }

    /// <summary>Último prompt recibido.</summary>
    public ChatPrompt? LastPrompt { get; private set; }

    public Task<ChatResult> CompleteAsync(ChatPrompt prompt, CancellationToken cancellationToken = default)
    {
        LastPrompt = prompt;
        return Task.FromResult(new ChatResult(_text, _responseId));
    }
}
