using System.Diagnostics.CodeAnalysis;
using MongoDB.Driver;

namespace JYDE.OpenDataCopilot.Infrastructure.Mongo;

/// <summary>
/// Contexto de conexión a MongoDB. Encapsula un <see cref="IMongoClient"/> <b>único</b> (el cliente
/// gestiona el pool de conexiones y debe ser singleton) y lo comparte entre los adaptadores Mongo.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class MongoContext
{
    /// <summary>Cliente de MongoDB compartido.</summary>
    public IMongoClient Client { get; }

    /// <summary>Base de datos configurada.</summary>
    public IMongoDatabase Database { get; }

    /// <summary>Crea el contexto a partir de las opciones de conexión.</summary>
    /// <param name="options">Opciones de conexión (cadena y base de datos).</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="options"/> es nulo.</exception>
    public MongoContext(MongoOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Client = new MongoClient(options.ConnectionString);
        Database = Client.GetDatabase(options.Database);
    }
}
