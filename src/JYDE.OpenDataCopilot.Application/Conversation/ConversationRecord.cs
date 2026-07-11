namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Conversación completa persistida: transcripción (mensajes), memoria (objetivo + datasets fijados),
/// artefactos (tablas/gráficos) y auditoría. Es la unidad que se guarda/recupera/elimina en la BD.
/// </summary>
/// <param name="Id">Identificador de la conversación.</param>
/// <param name="Title">Título mostrado en la barra lateral.</param>
/// <param name="ThreadId">Id del hilo del proveedor de chat (para continuar el contexto); nulo si es nuevo.</param>
/// <param name="Messages">Turnos de la conversación.</param>
/// <param name="Objective">Objetivo acumulado (memoria).</param>
/// <param name="SelectedDatasets">Datasets fijados (memoria).</param>
/// <param name="Artifacts">Tablas y gráficos generados.</param>
/// <param name="AuditLog">Bitácora de auditoría (interacciones crudas por turno).</param>
/// <param name="UpdatedAtUtc">Marca de última actualización (la sella el servidor al guardar).</param>
public sealed record ConversationRecord(
    string Id,
    string Title,
    string? ThreadId,
    IReadOnlyList<ConversationMessageRecord> Messages,
    string Objective,
    IReadOnlyList<SelectedDataset> SelectedDatasets,
    IReadOnlyList<ConversationArtifactRecord> Artifacts,
    IReadOnlyList<ConversationAuditEntryRecord> AuditLog,
    DateTimeOffset UpdatedAtUtc = default);
