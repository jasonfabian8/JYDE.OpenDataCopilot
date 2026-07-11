using JYDE.OpenDataCopilot.Application.Conversation;

namespace JYDE.OpenDataCopilot.Application.Tests.Conversation;

/// <summary>Doble de <see cref="IChatCompletion"/> que falla (simula que el agente no está disponible).</summary>
internal sealed class ThrowingChatCompletion : IChatCompletion
{
    private readonly Exception _error;

    public ThrowingChatCompletion(Exception? error = null) => _error = error ?? new HttpRequestException("fallo simulado");

    public Task<ChatResult> CompleteAsync(ChatPrompt prompt, CancellationToken cancellationToken = default) => throw _error;
}
