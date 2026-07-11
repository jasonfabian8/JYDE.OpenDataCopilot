namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>Evento del flujo de conversación (se serializa como evento SSE).</summary>
/// <param name="Kind">Tipo de evento.</param>
/// <param name="Agent">Nombre del agente (en eventos <see cref="ConversationEventKind.Agent"/>).</param>
/// <param name="Token">Fragmento de texto (en eventos <see cref="ConversationEventKind.Token"/>).</param>
/// <param name="Sources">Fuentes citadas (en eventos <see cref="ConversationEventKind.Sources"/>).</param>
/// <param name="ConversationId">Id del hilo (en eventos <see cref="ConversationEventKind.Conversation"/>).</param>
/// <param name="Query">Consulta a reintentar tras cargar (en eventos <see cref="ConversationEventKind.Categories"/>).</param>
/// <param name="Categories">Categorías recomendadas (en eventos <see cref="ConversationEventKind.Categories"/>).</param>
/// <param name="Objective">Objetivo acumulado (en eventos <see cref="ConversationEventKind.Objective"/>).</param>
/// <param name="Table">Artefacto de tabla (en eventos <see cref="ConversationEventKind.Table"/>).</param>
/// <param name="Chart">Artefacto de gráfico (en eventos <see cref="ConversationEventKind.Chart"/>).</param>
/// <param name="Interactions">Interacciones crudas del turno (en eventos <see cref="ConversationEventKind.Audit"/>).</param>
public sealed record ConversationEvent(
    ConversationEventKind Kind,
    string? Agent = null,
    string? Token = null,
    IReadOnlyList<Citation>? Sources = null,
    string? ConversationId = null,
    string? Query = null,
    IReadOnlyList<CategoryRecommendation>? Categories = null,
    string? Objective = null,
    TableArtifact? Table = null,
    ChartArtifact? Chart = null,
    IReadOnlyList<AgentInteraction>? Interactions = null)
{
    /// <summary>Crea un evento que anuncia el agente que atiende.</summary>
    public static ConversationEvent ForAgent(string agent) => new(ConversationEventKind.Agent, Agent: agent);

    /// <summary>Crea un evento con las fuentes citadas.</summary>
    public static ConversationEvent ForSources(IReadOnlyList<Citation> sources) =>
        new(ConversationEventKind.Sources, Sources: sources);

    /// <summary>Crea un evento con categorías recomendadas para cargar y la consulta a reintentar.</summary>
    public static ConversationEvent ForCategories(string query, IReadOnlyList<CategoryRecommendation> categories) =>
        new(ConversationEventKind.Categories, Query: query, Categories: categories);

    /// <summary>Crea un evento con un fragmento de texto.</summary>
    public static ConversationEvent ForToken(string token) => new(ConversationEventKind.Token, Token: token);

    /// <summary>Crea un evento con el id del hilo de conversación.</summary>
    public static ConversationEvent ForConversation(string conversationId) =>
        new(ConversationEventKind.Conversation, ConversationId: conversationId);

    /// <summary>Crea un evento con el objetivo acumulado de la conversación (memoria).</summary>
    public static ConversationEvent ForObjective(string objective) =>
        new(ConversationEventKind.Objective, Objective: objective);

    /// <summary>Crea un evento con un artefacto de tabla.</summary>
    public static ConversationEvent ForTable(TableArtifact table) =>
        new(ConversationEventKind.Table, Table: table);

    /// <summary>Crea un evento con un artefacto de gráfico.</summary>
    public static ConversationEvent ForChart(ChartArtifact chart) =>
        new(ConversationEventKind.Chart, Chart: chart);

    /// <summary>Crea un evento con las interacciones crudas del turno (auditoría).</summary>
    public static ConversationEvent ForAudit(IReadOnlyList<AgentInteraction> interactions) =>
        new(ConversationEventKind.Audit, Interactions: interactions);

    /// <summary>Evento de fin de respuesta.</summary>
    public static ConversationEvent Completed() => new(ConversationEventKind.Done);
}
