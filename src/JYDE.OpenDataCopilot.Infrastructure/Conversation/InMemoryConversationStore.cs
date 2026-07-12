using System.Collections.Concurrent;
using JYDE.OpenDataCopilot.Application.Conversation;

namespace JYDE.OpenDataCopilot.Infrastructure.Conversation;

/// <summary>
/// Adaptador en memoria de <see cref="IConversationStore"/> (por defecto, $0). Guarda las
/// conversaciones en un diccionario concurrente durante la vida del proceso. Útil en desarrollo y
/// pruebas; en producción se usa el adaptador Mongo (ver ADR-0017).
/// </summary>
public sealed class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<string, ConversationRecord> _conversations = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task SaveAsync(ConversationRecord conversation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        _conversations[conversation.Id] = conversation;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ConversationRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        _conversations.TryGetValue(id, out ConversationRecord? conversation);
        return Task.FromResult(conversation);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ConversationSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ConversationSummary> summaries =
        [
            .. _conversations.Values
                .OrderByDescending(conversation => conversation.UpdatedAtUtc)
                .Select(conversation => new ConversationSummary(conversation.Id, conversation.Title, conversation.UpdatedAtUtc))
        ];
        return Task.FromResult(summaries);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        _conversations.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
