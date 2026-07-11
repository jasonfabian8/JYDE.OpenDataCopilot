namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Enrutador por defecto: elige el primer agente que declara poder atender la pregunta
/// (<see cref="IConversationAgent.CanHandle"/>); si ninguno, usa el primero como reserva. Estrategia
/// determinista suficiente mientras hay pocos agentes; se reemplazará por una basada en LLM.
/// </summary>
public sealed class DefaultAgentRouter : IAgentRouter
{
    /// <inheritdoc />
    public IConversationAgent Route(string question, IReadOnlyList<IConversationAgent> agents)
    {
        ArgumentNullException.ThrowIfNull(agents);
        if (agents.Count == 0)
        {
            throw new InvalidOperationException("No hay agentes registrados para atender la conversación.");
        }

        return agents.FirstOrDefault(agent => agent.CanHandle(question)) ?? agents[0];
    }
}
