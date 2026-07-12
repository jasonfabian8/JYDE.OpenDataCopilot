namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Caso de uso de archivo de conversaciones: guardar, recuperar, listar y eliminar conversaciones
/// completas (transcripción + memoria + artefactos + auditoría) a través del puerto de persistencia.
/// Sella la marca de actualización en el servidor al guardar (ordena la lista por reciente).
/// </summary>
public sealed class ConversationArchiveService
{
    private readonly IConversationStore _store;
    private readonly TimeProvider _clock;

    /// <summary>Crea el servicio de archivo de conversaciones.</summary>
    /// <param name="store">Puerto de persistencia de conversaciones.</param>
    /// <param name="clock">Reloj (para sellar la marca de actualización); por defecto, el del sistema.</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="store"/> es nulo.</exception>
    public ConversationArchiveService(IConversationStore store, TimeProvider? clock = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        _clock = clock ?? TimeProvider.System;
    }

    /// <summary>Guarda (inserta o reemplaza) la conversación, sellando la marca de actualización.</summary>
    /// <param name="conversation">Conversación completa a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="conversation"/> es nulo.</exception>
    /// <exception cref="ArgumentException">Si la conversación no tiene id.</exception>
    public Task SaveAsync(ConversationRecord conversation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        if (string.IsNullOrWhiteSpace(conversation.Id))
        {
            throw new ArgumentException("La conversación debe tener id.", nameof(conversation));
        }

        ConversationRecord stamped = conversation with { UpdatedAtUtc = _clock.GetUtcNow() };
        return _store.SaveAsync(stamped, cancellationToken);
    }

    /// <summary>Obtiene una conversación por su id, o <c>null</c> si no existe.</summary>
    /// <param name="id">Identificador de la conversación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <exception cref="ArgumentException">Si <paramref name="id"/> es nulo o vacío.</exception>
    public Task<ConversationRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return _store.GetAsync(id, cancellationToken);
    }

    /// <summary>Lista los resúmenes de las conversaciones (más reciente primero).</summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    public Task<IReadOnlyList<ConversationSummary>> ListAsync(CancellationToken cancellationToken = default)
        => _store.ListAsync(cancellationToken);

    /// <summary>Elimina una conversación por su id (idempotente).</summary>
    /// <param name="id">Identificador de la conversación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <exception cref="ArgumentException">Si <paramref name="id"/> es nulo o vacío.</exception>
    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return _store.DeleteAsync(id, cancellationToken);
    }
}
