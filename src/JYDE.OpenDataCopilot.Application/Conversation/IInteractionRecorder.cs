namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Registra las interacciones crudas con los agentes durante un turno (para auditoría). El alcance es
/// por turno: <see cref="Begin"/> inicia una captura nueva y <see cref="Interactions"/> devuelve lo
/// acumulado en el flujo asíncrono actual.
/// </summary>
public interface IInteractionRecorder
{
    /// <summary>Inicia la captura de un turno (limpia lo anterior del flujo actual).</summary>
    void Begin();

    /// <summary>Registra una interacción con un agente.</summary>
    /// <param name="agent">Nombre del agente.</param>
    /// <param name="request">Mensaje enviado.</param>
    /// <param name="response">Respuesta cruda.</param>
    void Record(string agent, string request, string response);

    /// <summary>Interacciones acumuladas en el turno actual, en orden de ocurrencia.</summary>
    IReadOnlyList<AgentInteraction> Interactions { get; }
}
