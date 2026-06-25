using JYDE.OpenDataCopilot.Application.Conversation;

namespace JYDE.OpenDataCopilot.Infrastructure.Chat;

/// <summary>
/// Adaptador de <see cref="IChatCompletion"/> determinista y sin costo para desarrollo/pruebas
/// (sin credenciales). Devuelve una respuesta de marcador. Se reemplaza por el adaptador de Foundry
/// vía configuración (<c>Providers:Chat</c>).
/// </summary>
public sealed class FakeChatCompletion : IChatCompletion
{
    /// <inheritdoc />
    public Task<ChatResult> CompleteAsync(ChatPrompt prompt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        const string text =
            "(respuesta de demostración local) Revisa los conjuntos de datos listados como fuente. " +
            "Configura un agente real de Foundry para respuestas completas.";

        return Task.FromResult(new ChatResult(text, "fake-response-id"));
    }
}
