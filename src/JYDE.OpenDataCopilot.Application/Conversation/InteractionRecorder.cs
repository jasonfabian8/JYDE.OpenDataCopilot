namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Grabador de interacciones por turno. Se registra con alcance de petición (scoped): cada petición
/// tiene su propia instancia, así no se mezclan peticiones concurrentes y funciona correctamente con
/// el iterador asíncrono del orquestador (a diferencia de <c>AsyncLocal</c>, que no sobrevive a los
/// <c>yield</c>). Dentro de una petición el flujo es secuencial, por lo que la lista es segura.
/// </summary>
public sealed class InteractionRecorder : IInteractionRecorder
{
    private readonly List<AgentInteraction> _interactions = [];

    /// <inheritdoc />
    public void Begin() => _interactions.Clear();

    /// <inheritdoc />
    public void Record(string agent, string request, string response) =>
        _interactions.Add(new AgentInteraction(agent, request, response));

    /// <inheritdoc />
    public IReadOnlyList<AgentInteraction> Interactions => [.. _interactions];
}
