namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>Resumen de una conversación persistida (para listar en la barra lateral sin traer todo el contenido).</summary>
/// <param name="Id">Identificador de la conversación.</param>
/// <param name="Title">Título mostrado.</param>
/// <param name="UpdatedAtUtc">Marca de última actualización (para ordenar por reciente).</param>
public sealed record ConversationSummary(string Id, string Title, DateTimeOffset UpdatedAtUtc);
