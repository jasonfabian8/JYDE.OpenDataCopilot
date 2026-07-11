namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Decorador de <see cref="IChatCompletion"/> que registra cada interacción (agente, mensaje enviado,
/// respuesta cruda) en un <see cref="IInteractionRecorder"/> para la auditoría, y delega en el chat real.
/// </summary>
public sealed class AuditingChatCompletion : IChatCompletion
{
    private readonly IChatCompletion _inner;
    private readonly IInteractionRecorder _recorder;

    /// <summary>Crea el decorador de auditoría.</summary>
    /// <param name="inner">Chat real al que se delega.</param>
    /// <param name="recorder">Grabador de interacciones.</param>
    /// <exception cref="ArgumentNullException">Si algún argumento es nulo.</exception>
    public AuditingChatCompletion(IChatCompletion inner, IInteractionRecorder recorder)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(recorder);
        _inner = inner;
        _recorder = recorder;
    }

    /// <inheritdoc />
    public async Task<ChatResult> CompleteAsync(ChatPrompt prompt, CancellationToken cancellationToken = default)
    {
        ChatResult result = await _inner.CompleteAsync(prompt, cancellationToken);
        _recorder.Record(prompt.Agent, prompt.Input, result.Text);
        return result;
    }
}
