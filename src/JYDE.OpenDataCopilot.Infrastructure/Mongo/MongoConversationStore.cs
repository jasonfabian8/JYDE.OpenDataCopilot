using System.Diagnostics.CodeAnalysis;
using JYDE.OpenDataCopilot.Application.Conversation;
using MongoDB.Driver;

namespace JYDE.OpenDataCopilot.Infrastructure.Mongo;

/// <summary>
/// Adaptador de <see cref="IConversationStore"/> sobre MongoDB (Atlas). Persiste conversaciones
/// completas (ver <see cref="ConversationDocument"/>). Adaptador de E/S externa: se excluye de la
/// cobertura unitaria y se valida con pruebas de integración (el mapeo se prueba en el documento).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class MongoConversationStore : IConversationStore
{
    private readonly IMongoCollection<ConversationDocument> _collection;

    /// <summary>Crea el adaptador Mongo usando el cliente compartido.</summary>
    /// <param name="context">Contexto de Mongo (cliente y base de datos compartidos).</param>
    /// <param name="options">Opciones (nombre de colección).</param>
    /// <exception cref="ArgumentNullException">Si algún argumento es nulo.</exception>
    public MongoConversationStore(MongoContext context, MongoOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        _collection = context.Database.GetCollection<ConversationDocument>(options.ConversationCollection);
    }

    /// <inheritdoc />
    public async Task SaveAsync(ConversationRecord conversation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        ConversationDocument document = ConversationDocument.FromRecord(conversation);
        FilterDefinition<ConversationDocument> filter =
            Builders<ConversationDocument>.Filter.Eq(existing => existing.Id, document.Id);
        await _collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConversationRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        IAsyncCursor<ConversationDocument> cursor =
            await _collection.FindAsync(document => document.Id == id, cancellationToken: cancellationToken);
        ConversationDocument? found = await cursor.FirstOrDefaultAsync(cancellationToken);
        return found?.ToRecord();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConversationSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        // Excluimos el payload: para la lista solo se necesitan id, título y fecha.
        FindOptions<ConversationDocument> options = new()
        {
            Sort = Builders<ConversationDocument>.Sort.Descending(document => document.UpdatedAtUtc),
            Projection = Builders<ConversationDocument>.Projection.Exclude(document => document.Payload),
        };
        using IAsyncCursor<ConversationDocument> cursor =
            await _collection.FindAsync(FilterDefinition<ConversationDocument>.Empty, options, cancellationToken);
        List<ConversationDocument> documents = await cursor.ToListAsync(cancellationToken);
        return [.. documents.Select(document => document.ToSummary())];
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        await _collection.DeleteOneAsync(document => document.Id == id, cancellationToken);
    }
}
