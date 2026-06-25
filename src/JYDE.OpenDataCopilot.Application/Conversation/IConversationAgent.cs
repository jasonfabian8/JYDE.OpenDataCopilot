namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Agente especializado del Copilot. Cada capacidad (recomendar datasets, calcular cifras, …) es
/// un agente que produce un flujo de eventos de conversación.
/// </summary>
public interface IConversationAgent
{
    /// <summary>Identificador legible del agente (se anuncia al cliente).</summary>
    string Name { get; }

    /// <summary>Descripción de la capacidad (la usa el enrutador para decidir).</summary>
    string Description { get; }

    /// <summary>Indica si el agente puede atender la pregunta dada.</summary>
    /// <param name="question">Pregunta del usuario.</param>
    bool CanHandle(string question);

    /// <summary>Atiende la consulta y emite el flujo de eventos (agente, fuentes, tokens, fin).</summary>
    /// <param name="context">Contexto de la consulta.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    IAsyncEnumerable<ConversationEvent> HandleAsync(ConversationContext context, CancellationToken cancellationToken = default);
}
