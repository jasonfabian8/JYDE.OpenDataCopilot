using System.Diagnostics.CodeAnalysis;
using JYDE.OpenDataCopilot.Application.Search;
using MongoDB.Bson;
using MongoDB.Driver;

namespace JYDE.OpenDataCopilot.Infrastructure.Mongo;

/// <summary>
/// Adaptador de <see cref="IDatasetSearchIndex"/> sobre MongoDB Atlas Vector Search. Persiste los
/// vectores y recupera por similitud con la etapa de agregación <c>$vectorSearch</c>. Es un
/// adaptador de E/S externa: se excluye de la cobertura unitaria y se valida e2e contra Atlas.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class MongoDatasetSearchIndex : IDatasetSearchIndex
{
    private readonly IMongoCollection<DatasetVectorDocument> _collection;
    private readonly MongoOptions _options;

    /// <summary>Crea el índice (cliente compartido) y asegura el índice de Atlas Vector Search.</summary>
    /// <param name="context">Contexto de Mongo (cliente y base de datos compartidos).</param>
    /// <param name="options">Opciones de vector search.</param>
    /// <exception cref="ArgumentNullException">Si algún argumento es nulo.</exception>
    public MongoDatasetSearchIndex(MongoContext context, MongoOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
        _collection = context.Database.GetCollection<DatasetVectorDocument>(options.SearchCollection);

        EnsureVectorIndex();
    }

    /// <inheritdoc />
    public async Task IndexAsync(IReadOnlyCollection<DatasetVector> datasets, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(datasets);
        if (datasets.Count == 0)
        {
            return;
        }

        List<WriteModel<DatasetVectorDocument>> writes = [];
        foreach (DatasetVector dataset in datasets)
        {
            DatasetVectorDocument document = new()
            {
                Id = dataset.Id,
                Name = dataset.Name,
                Category = dataset.Category,
                SourceUrl = dataset.SourceUrl,
                Embedding = [.. dataset.Embedding],
            };
            FilterDefinition<DatasetVectorDocument> filter =
                Builders<DatasetVectorDocument>.Filter.Eq(existing => existing.Id, document.Id);
            writes.Add(new ReplaceOneModel<DatasetVectorDocument>(filter, document) { IsUpsert = true });
        }

        await _collection.BulkWriteAsync(writes, new BulkWriteOptions { IsOrdered = false }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DatasetSearchHit>> SearchAsync(
        IReadOnlyList<float> queryEmbedding,
        int topK,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryEmbedding);
        ArgumentOutOfRangeException.ThrowIfLessThan(topK, 1);

        BsonDocument vectorSearch = new("$vectorSearch", new BsonDocument
        {
            { "index", _options.VectorIndexName },
            { "path", "embedding" },
            { "queryVector", new BsonArray(queryEmbedding.Select(value => (double)value)) },
            { "numCandidates", _options.VectorNumCandidates },
            { "limit", topK },
        });

        BsonDocument project = new("$project", new BsonDocument
        {
            { "name", 1 },
            { "category", 1 },
            { "sourceUrl", 1 },
            { "score", new BsonDocument("$meta", "vectorSearchScore") },
        });

        BsonDocument[] pipeline = [vectorSearch, project];
        using IAsyncCursor<BsonDocument> cursor =
            await _collection.AggregateAsync<BsonDocument>(pipeline, cancellationToken: cancellationToken);
        List<BsonDocument> documents = await cursor.ToListAsync(cancellationToken);

        return [.. documents.Select(ToHit)];
    }

    private static DatasetSearchHit ToHit(BsonDocument document) => new(
        document.GetValue("_id", BsonNull.Value).AsString,
        document.GetValue("name", string.Empty).AsString,
        document.TryGetValue("category", out BsonValue category) && !category.IsBsonNull ? category.AsString : null,
        document.TryGetValue("sourceUrl", out BsonValue sourceUrl) && !sourceUrl.IsBsonNull ? sourceUrl.AsString : null,
        document.GetValue("score", 0d).ToDouble());

    /// <summary>
    /// Intenta crear el índice de Atlas Vector Search si no existe. Best-effort: en tiers donde la
    /// creación por driver no esté disponible, debe crearse desde la consola de Atlas.
    /// </summary>
    private void EnsureVectorIndex()
    {
        try
        {
            bool alreadyExists = _collection.SearchIndexes.List().ToList()
                .Any(definition => definition.GetValue("name", string.Empty).AsString == _options.VectorIndexName);
            if (alreadyExists)
            {
                return;
            }

            BsonDocument vectorField = new()
            {
                { "type", "vector" },
                { "path", "embedding" },
                { "numDimensions", _options.VectorDimensions },
                { "similarity", "cosine" },
            };
            BsonDocument definition = new() { { "fields", new BsonArray { vectorField } } };

            CreateSearchIndexModel model = new(_options.VectorIndexName, SearchIndexType.VectorSearch, definition);
            _collection.SearchIndexes.CreateOne(model);
        }
        catch (MongoCommandException)
        {
            // El tier no permite gestionar el índice por driver: créalo en la consola de Atlas
            // (Vector Search, campo "embedding", coseno, dimensiones según VectorDimensions).
        }
    }
}
