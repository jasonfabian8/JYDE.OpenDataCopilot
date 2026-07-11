namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Puerto de salida: modelo de lenguaje (chat) que atiende a un agente especializado. Soporta
/// continuación de conversación (threading) mediante el identificador de respuesta previo.
/// </summary>
public interface IChatCompletion
{
    /// <summary>Genera la respuesta del agente para el <paramref name="prompt"/> dado.</summary>
    /// <param name="prompt">Agente, entrada e identificador de respuesta previo (si hay hilo).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El texto de la respuesta y el identificador para continuar el hilo.</returns>
    Task<ChatResult> CompleteAsync(ChatPrompt prompt, CancellationToken cancellationToken = default);
}
