using System.Text.Json;
using JYDE.OpenDataCopilot.Application.Conversation;
using MongoDB.Bson.Serialization.Attributes;

namespace JYDE.OpenDataCopilot.Infrastructure.Mongo;

/// <summary>
/// Documento Mongo de una conversación. Guarda campos consultables (id, título, fecha) para listar sin
/// deserializar, y la conversación completa como JSON (<see cref="Payload"/>) para no mapear en BSON
/// estructuras inmutables anidadas y con variantes (tablas/gráficos). El mapeo se prueba con round-trip.
/// </summary>
public sealed class ConversationDocument
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Id de la conversación (clave primaria del documento).</summary>
    [BsonId]
    public string Id { get; set; } = string.Empty;

    /// <summary>Título (consultable, para listar sin abrir el payload).</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Marca de última actualización en UTC (para ordenar la lista por reciente).</summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>Conversación completa serializada como JSON.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Crea el documento a partir del registro de aplicación.</summary>
    /// <param name="conversation">Conversación completa.</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="conversation"/> es nulo.</exception>
    public static ConversationDocument FromRecord(ConversationRecord conversation)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        return new ConversationDocument
        {
            Id = conversation.Id,
            Title = conversation.Title,
            UpdatedAtUtc = conversation.UpdatedAtUtc.UtcDateTime,
            Payload = JsonSerializer.Serialize(conversation, JsonOptions),
        };
    }

    /// <summary>Reconstruye el registro completo desde el JSON persistido.</summary>
    /// <exception cref="InvalidOperationException">Si el payload es inválido/nulo.</exception>
    public ConversationRecord ToRecord()
        => JsonSerializer.Deserialize<ConversationRecord>(Payload, JsonOptions)
           ?? throw new InvalidOperationException($"La conversación '{Id}' tiene un payload inválido.");

    /// <summary>Proyecta el resumen (sin deserializar el payload).</summary>
    public ConversationSummary ToSummary()
        => new(Id, Title, new DateTimeOffset(DateTime.SpecifyKind(UpdatedAtUtc, DateTimeKind.Utc)));
}
