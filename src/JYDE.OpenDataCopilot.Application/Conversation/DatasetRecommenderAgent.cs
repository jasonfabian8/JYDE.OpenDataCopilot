using System.Runtime.CompilerServices;
using System.Text.Json;
using JYDE.OpenDataCopilot.Application.Search;

namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Agente especializado en recomendar datasets: recupera candidatos (Search) y pide al LLM una
/// respuesta estructurada (JSON) con el texto para el ciudadano y una relevancia RECALCULADA por
/// cada candidato. Con esa relevancia decide qué citar: solo los que superan el umbral, evitando
/// citar datasets que —aunque cercanos por embedding— no vienen al caso. Mantiene el hilo (threading).
/// </summary>
public sealed class DatasetRecommenderAgent : IConversationAgent
{
    /// <summary>Relevancia mínima (0-1, recalculada por el LLM) para citar un candidato.</summary>
    public const double DefaultRelevanceThreshold = 0.5;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IEmbeddingGenerator _embeddings;
    private readonly IDatasetSearchIndex _index;
    private readonly IChatCompletion _chat;
    private readonly double _relevanceThreshold;

    /// <summary>Crea el agente recomendador.</summary>
    /// <param name="embeddings">Generador de embeddings.</param>
    /// <param name="index">Índice de búsqueda de datasets.</param>
    /// <param name="chat">Modelo de chat (LLM).</param>
    /// <param name="relevanceThreshold">Relevancia mínima (recalculada por el LLM) para citar.</param>
    /// <exception cref="ArgumentNullException">Si alguna dependencia es nula.</exception>
    public DatasetRecommenderAgent(
        IEmbeddingGenerator embeddings,
        IDatasetSearchIndex index,
        IChatCompletion chat,
        double relevanceThreshold = DefaultRelevanceThreshold)
    {
        ArgumentNullException.ThrowIfNull(embeddings);
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(chat);
        _embeddings = embeddings;
        _index = index;
        _chat = chat;
        _relevanceThreshold = relevanceThreshold;
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

        // Componemos el input (consulta + candidatos con su id) pidiendo una respuesta JSON con la
        // relevancia recalculada; mantenemos el hilo con el id de respuesta anterior.
        ChatPrompt prompt = new(Name, BuildInput(context.Question, hits), context.PreviousResponseId);
        ChatResult result = await _chat.CompleteAsync(prompt, cancellationToken);

        (string answer, IReadOnlyList<Citation> citations) = Interpret(result.Text, hits);

        // Las fuentes se emiten DESPUÉS del LLM: solo se citan las que él marcó como relevantes.
        if (citations.Count > 0)
        {
            yield return ConversationEvent.ForSources(citations);
        }

        foreach (string chunk in Chunk(answer))
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

    /// <summary>
    /// Interpreta la respuesta del LLM: extrae el texto y las relevancias recalculadas, y construye
    /// las citas de los candidatos que superan el umbral. Si el JSON no se puede leer, degrada a
    /// devolver el texto tal cual sin citar (evita citar candidatos sin relevancia validada).
    /// </summary>
    private (string Answer, IReadOnlyList<Citation> Citations) Interpret(
        string text,
        IReadOnlyList<DatasetSearchHit> hits)
    {
        RecommenderReply? reply = TryParseReply(text);
        if (reply is null)
        {
            return (text, []);
        }

        string answer = string.IsNullOrWhiteSpace(reply.Respuesta) ? text : reply.Respuesta;

        Dictionary<string, double> relevanceById = new(StringComparer.OrdinalIgnoreCase);
        foreach (RecommenderDatasetScore score in reply.Datasets ?? [])
        {
            if (!string.IsNullOrWhiteSpace(score.Id))
            {
                relevanceById[score.Id] = score.Relevancia;
            }
        }

        IReadOnlyList<Citation> citations =
        [
            .. hits
                .Where(hit => relevanceById.TryGetValue(hit.Id, out double relevance) && relevance >= _relevanceThreshold)
                .Select(hit => new Citation(hit.Id, hit.Name, hit.SourceUrl, relevanceById[hit.Id]))
                .OrderByDescending(citation => citation.Score)
        ];

        return (answer, citations);
    }

    private static RecommenderReply? TryParseReply(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        // El modelo puede envolver el JSON en prosa o en vallas ```; tomamos del primer '{' al último '}'.
        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<RecommenderReply>(text[start..(end + 1)], JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string BuildInput(string question, IReadOnlyList<DatasetSearchHit> hits)
    {
        // El user prompt lleva SOLO datos; las reglas, la rúbrica y el esquema JSON viven en las
        // instrucciones del agente en Foundry (versionadas), para no inflar el contexto.
        string nl = Environment.NewLine;

        if (hits.Count == 0)
        {
            return
                $"Consulta del ciudadano: {question}{nl}{nl}" +
                "Candidatos recuperados del índice: (ninguno para esta consulta)";
        }

        string candidates = string.Join(
            nl,
            hits.Select((hit, index) =>
                $"{index + 1}. [id={hit.Id}] {hit.Name} " +
                $"(categoría: {hit.Category ?? "n/d"}; fuente: {hit.SourceUrl ?? "n/d"})"));

        return
            $"Consulta del ciudadano: {question}{nl}{nl}" +
            $"Candidatos recuperados del índice (id | nombre | categoría | fuente):{nl}{candidates}";
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
