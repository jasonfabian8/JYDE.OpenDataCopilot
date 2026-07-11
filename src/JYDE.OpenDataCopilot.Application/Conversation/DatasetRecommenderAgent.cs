using System.Runtime.CompilerServices;
using JYDE.OpenDataCopilot.Application.Search;

namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Agente especializado en recomendar datasets: recupera los más relevantes (Search) y pide al LLM
/// una recomendación clara y citada, manteniendo el hilo de conversación (threading).
/// </summary>
public sealed class DatasetRecommenderAgent : IConversationAgent
{
    private readonly IEmbeddingGenerator _embeddings;
    private readonly IDatasetSearchIndex _index;
    private readonly IChatCompletion _chat;

    /// <summary>Crea el agente recomendador.</summary>
    /// <param name="embeddings">Generador de embeddings.</param>
    /// <param name="index">Índice de búsqueda de datasets.</param>
    /// <param name="chat">Modelo de chat (LLM).</param>
    /// <exception cref="ArgumentNullException">Si alguna dependencia es nula.</exception>
    public DatasetRecommenderAgent(IEmbeddingGenerator embeddings, IDatasetSearchIndex index, IChatCompletion chat)
    {
        ArgumentNullException.ThrowIfNull(embeddings);
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(chat);
        _embeddings = embeddings;
        _index = index;
        _chat = chat;
    }

    /// <inheritdoc />
    public string Name => "dataset-recommender-agent";

    /// <inheritdoc />
    public string Description =>
        "Recomienda conjuntos de datos abiertos relevantes de datos.gov.co para una consulta, citando la fuente.";

    /// <inheritdoc />
    public bool CanHandle(string question) => true;

    /// <inheritdoc />
    public IAsyncEnumerable<ConversationEvent> HandleAsync(
        ConversationContext context,
        CancellationToken cancellationToken = default)
    {
        // Validación temprana (eager); la iteración perezosa vive en el método iterador privado.
        ArgumentNullException.ThrowIfNull(context);
        return HandleIteratorAsync(context, cancellationToken);
    }

    private async IAsyncEnumerable<ConversationEvent> HandleIteratorAsync(
        ConversationContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return ConversationEvent.ForAgent(Name);

        IReadOnlyList<float> queryEmbedding = await _embeddings.GenerateAsync(context.Question, cancellationToken);
        IReadOnlyList<DatasetSearchHit> hits = await _index.SearchAsync(queryEmbedding, context.TopK, cancellationToken);

        if (hits.Count > 0)
        {
            IReadOnlyList<Citation> citations =
                [.. hits.Select(hit => new Citation(hit.Id, hit.Name, hit.SourceUrl, hit.Score))];
            yield return ConversationEvent.ForSources(citations);
        }

        // Las instrucciones del agente viven en Foundry; aquí componemos el input (consulta + candidatos)
        // y mantenemos el hilo con el id de respuesta anterior.
        ChatPrompt prompt = new(Name, BuildInput(context.Question, hits), context.PreviousResponseId);
        ChatResult result = await _chat.CompleteAsync(prompt, cancellationToken);

        foreach (string chunk in Chunk(result.Text))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return ConversationEvent.ForToken(chunk);
        }

        if (!string.IsNullOrEmpty(result.ResponseId))
        {
            yield return ConversationEvent.ForConversation(result.ResponseId);
        }

        yield return ConversationEvent.Completed();
    }

    private static string BuildInput(string question, IReadOnlyList<DatasetSearchHit> hits)
    {
        string candidates = hits.Count == 0
            ? "(no se encontraron datasets candidatos para esta consulta)"
            : string.Join(
                Environment.NewLine,
                hits.Select((hit, index) =>
                    $"{index + 1}. {hit.Name} (categoría: {hit.Category ?? "n/d"}; fuente: {hit.SourceUrl ?? "n/d"})"));

        return
            $"Consulta del ciudadano: {question}{Environment.NewLine}{Environment.NewLine}" +
            $"Datasets candidatos:{Environment.NewLine}{candidates}{Environment.NewLine}{Environment.NewLine}" +
            "Recomienda cuáles le sirven y por qué.";
    }

    /// <summary>Trocea el texto en fragmentos (por palabras) para dar sensación de streaming.</summary>
    private static IEnumerable<string> Chunk(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        string[] words = text.Split(' ');
        foreach (string word in words)
        {
            yield return word + " ";
        }
    }
}
