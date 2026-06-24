using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JYDE.OpenDataCopilot.Application.Catalog;
using JYDE.OpenDataCopilot.Domain.Catalog;
using MongoDB.Driver;

namespace JYDE.OpenDataCopilot.Infrastructure.Mongo;

/// <summary>
/// Adaptador de <see cref="ICatalogRepository"/> sobre MongoDB (Atlas). Persiste los metadatos del
/// catálogo. Es un adaptador de E/S externa: se excluye de la cobertura unitaria y se valida con
/// pruebas e2e/integración contra el clúster real (el mapeo se prueba en <see cref="DatasetDocument"/>).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class MongoCatalogRepository : ICatalogRepository
{
    private readonly IMongoCollection<DatasetDocument> _collection;

    /// <summary>Crea el repositorio Mongo usando el cliente compartido.</summary>
    /// <param name="context">Contexto de Mongo (cliente y base de datos compartidos).</param>
    /// <param name="options">Opciones (nombre de colección).</param>
    /// <exception cref="ArgumentNullException">Si algún argumento es nulo.</exception>
    public MongoCatalogRepository(MongoContext context, MongoOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        _collection = context.Database.GetCollection<DatasetDocument>(options.CatalogCollection);
    }

    /// <inheritdoc />
    public async Task SaveAsync(IReadOnlyCollection<Dataset> datasets, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(datasets);
        if (datasets.Count == 0)
        {
            return;
        }

        List<WriteModel<DatasetDocument>> writes = [];
        foreach (Dataset dataset in datasets)
        {
            DatasetDocument document = DatasetDocument.FromDomain(dataset);
            FilterDefinition<DatasetDocument> filter =
                Builders<DatasetDocument>.Filter.Eq(existing => existing.Id, document.Id);
            writes.Add(new ReplaceOneModel<DatasetDocument>(filter, document) { IsUpsert = true });
        }

        await _collection.BulkWriteAsync(writes, new BulkWriteOptions { IsOrdered = false }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Dataset?> GetByIdAsync(DatasetId id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        IAsyncCursor<DatasetDocument> cursor = await _collection.FindAsync(
            document => document.Id == id.Value,
            cancellationToken: cancellationToken);
        DatasetDocument? found = await cursor.FirstOrDefaultAsync(cancellationToken);
        return found?.ToDomain();
    }

    /// <inheritdoc />
    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        long count = await _collection.CountDocumentsAsync(
            FilterDefinition<DatasetDocument>.Empty,
            cancellationToken: cancellationToken);
        return (int)count;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Dataset> GetAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using IAsyncCursor<DatasetDocument> cursor = await _collection.FindAsync(
            FilterDefinition<DatasetDocument>.Empty,
            cancellationToken: cancellationToken);

        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (DatasetDocument document in cursor.Current)
            {
                yield return document.ToDomain();
            }
        }
    }
}
