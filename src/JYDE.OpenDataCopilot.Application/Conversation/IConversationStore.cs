namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Puerto de salida: persistencia de conversaciones completas (transcripción + memoria + artefactos +
/// auditoría). Los adaptadores viven en Infrastructure (InMemory local, Mongo en producción) y se
/// eligen por configuración (ver ADR-0003 y ADR-0017).
/// </summary>
public interface IConversationStore
{
    /// <summary>Guarda (inserta o reemplaza) una conversación por su id.</summary>
    /// <param name="conversation">Conversación completa a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task SaveAsync(ConversationRecord conversation, CancellationToken cancellationToken = default);

    /// <summary>Obtiene una conversación por su id, o <c>null</c> si no existe.</summary>
    /// <param name="id">Identificador de la conversación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<ConversationRecord?> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Lista los resúmenes de las conversaciones almacenadas (más reciente primero).</summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<ConversationSummary>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Elimina una conversación por su id (idempotente: no falla si no existe).</summary>
    /// <param name="id">Identificador de la conversación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
