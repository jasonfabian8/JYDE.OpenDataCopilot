namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Enrutador por reglas (sin LLM): elige el primer agente que declara poder atender la pregunta
/// (<see cref="IConversationAgent.CanHandle"/>); si ninguno, usa el primero como reserva. Se usa en
/// desarrollo/local (proveedor de chat de demostración), donde no hay un enrutador LLM disponible.
/// </summary>
public sealed class DefaultAgentRouter : IAgentRouter
{
    /// <inheritdoc />
    public Task<IConversationAgent> RouteAsync(
        string question,
        IReadOnlyList<IConversationAgent> agents,
        string? context = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agents);
        if (agents.Count == 0)
        {
            throw new InvalidOperationException("No hay agentes registrados para atender la conversación.");
        }

        IConversationAgent selected = agents.FirstOrDefault(agent => agent.CanHandle(question)) ?? agents[0];
        return Task.FromResult(selected);
    }
}
