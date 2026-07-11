using System.Runtime.CompilerServices;
using System.Text.Json;
using JYDE.OpenDataCopilot.Application.Catalog;
using JYDE.OpenDataCopilot.Application.Search;
using JYDE.OpenDataCopilot.Domain.Catalog;

namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Agente analista de datos: entiende los datasets desde sus metadatos (columnas) ya almacenados en
/// el catálogo. Resuelve el/los dataset(s) de la consulta (búsqueda semántica), trae su esquema
/// completo del repositorio y pide al LLM que (a) describa sus columnas o (b) evalúe si dos datasets
/// pueden cruzarse/correlacionarse por columnas comunes (municipio, año, etc.). Cita los datasets
/// usados y mantiene el hilo (threading). No consulta datos reales (eso es el agente de cifras/SoQL).
/// </summary>
public sealed class DatasetAnalystAgent : IConversationAgent
{
    /// <summary>Relevancia mínima (0-1, recalculada por el LLM) para citar un dataset.</summary>
    public const double DefaultRelevanceThreshold = 0.5;

    /// <summary>Cantidad mínima de candidatos a considerar (más contexto para evaluar cruces).</summary>
    public const int CandidateCount = 8;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IEmbeddingGenerator _embeddings;
    private readonly IDatasetSearchIndex _index;
    private readonly ICatalogRepository _repository;
    private readonly IChatCompletion _chat;
    private readonly double _relevanceThreshold;

    /// <summary>Crea el agente analista.</summary>
    /// <param name="embeddings">Generador de embeddings.</param>
    /// <param name="index">Índice de búsqueda de datasets.</param>
    /// <param name="repository">Repositorio del catálogo (metadatos completos con columnas).</param>
    /// <param name="chat">Modelo de chat (LLM).</param>
    /// <param name="relevanceThreshold">Relevancia mínima (recalculada por el LLM) para citar.</param>
    /// <exception cref="ArgumentNullException">Si alguna dependencia es nula.</exception>
    public DatasetAnalystAgent(
        IEmbeddingGenerator embeddings,
        IDatasetSearchIndex index,
        ICatalogRepository repository,
        IChatCompletion chat,
        double relevanceThreshold = DefaultRelevanceThreshold)
    {
        ArgumentNullException.ThrowIfNull(embeddings);
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(chat);
        _embeddings = embeddings;
        _index = index;
        _repository = repository;
        _chat = chat;
        _relevanceThreshold = relevanceThreshold;
    }

    /// <inheritdoc />
    public string Name => "dataset-analyst-agent";

    /// <inheritdoc />
    public string Description =>
        "Explica las columnas/esquema de un dataset y evalúa si dos datasets pueden cruzarse o " +
        "correlacionarse (por columnas comunes), a partir de sus metadatos.";

    /// <inheritdoc />
    public bool CanHandle(string question)
    {
        string lowered = question.ToLowerInvariant();
        return lowered.Contains("columna")
            || lowered.Contains("campo")
            || lowered.Contains("esquema")
            || lowered.Contains("estructura")
            || lowered.Contains("atributo")
            || lowered.Contains("variable")
            || lowered.Contains("cruz")
            || lowered.Contains("correlacion")
            || lowered.Contains("correlación")
            || lowered.Contains("combinar")
            || lowered.Contains("relacionar");
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ConversationEvent> HandleAsync(
        ConversationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return HandleIteratorAsync(context, cancellationToken);
    }

    private async IAsyncEnumerable<ConversationEvent> HandleIteratorAsync(
        ConversationContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return ConversationEvent.ForAgent(Name);

        int candidates = Math.Max(context.TopK, CandidateCount);
        IReadOnlyList<float> queryEmbedding = await _embeddings.GenerateAsync(context.Question, cancellationToken);
        IReadOnlyList<DatasetSearchHit> hits = await _index.SearchAsync(queryEmbedding, candidates, cancellationToken);

        // Traemos el esquema COMPLETO (columnas) del repositorio para cada candidato.
        List<Dataset> datasets = [];
        foreach (DatasetSearchHit hit in hits)
        {
            DatasetId? id = TryCreateId(hit.Id);
            if (id is null)
            {
                continue;
            }

            Dataset? dataset = await _repository.GetByIdAsync(id, cancellationToken);
            if (dataset is not null)
            {
                datasets.Add(dataset);
            }
        }

        ChatPrompt prompt = new(Name, BuildInput(context.Question, datasets), context.PreviousResponseId);
        ChatResult result = await _chat.CompleteAsync(prompt, cancellationToken);

        (string answer, IReadOnlyList<Citation> citations) = Interpret(result.Text, datasets);

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

    private (string Answer, IReadOnlyList<Citation> Citations) Interpret(string text, IReadOnlyList<Dataset> datasets)
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
            .. datasets
                .Where(dataset => relevanceById.TryGetValue(dataset.Id.Value, out double relevance) && relevance >= _relevanceThreshold)
                .Select(dataset => new Citation(
                    dataset.Id.Value,
                    dataset.Name,
                    dataset.SourceUrl?.ToString(),
                    relevanceById[dataset.Id.Value]))
                .OrderByDescending(citation => citation.Score)
        ];

        return (answer, citations);
    }

    private static RecommenderReply? TryParseReply(string text)
    {
        string? json = JsonText.FirstJsonObject(text);
        if (json is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<RecommenderReply>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static DatasetId? TryCreateId(string value)
    {
        try
        {
            return new DatasetId(value);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string BuildInput(string question, IReadOnlyList<Dataset> datasets)
    {
        // Solo datos: las reglas y el esquema JSON viven en las instrucciones del agente en Foundry.
        string nl = Environment.NewLine;

        if (datasets.Count == 0)
        {
            return
                $"Consulta del ciudadano: {question}{nl}{nl}" +
                "Datasets disponibles con sus columnas: (ninguno encontrado para esta consulta)";
        }

        string blocks = string.Join(
            nl,
            datasets.Select(dataset =>
                $"[id={dataset.Id.Value}] {dataset.Name} (categoría: {dataset.Category ?? "n/d"}; " +
                $"fuente: {dataset.SourceUrl?.ToString() ?? "n/d"}){nl}" +
                $"  columnas: {DescribeColumns(dataset.Columns)}"));

        return
            $"Consulta del ciudadano: {question}{nl}{nl}" +
            $"Datasets disponibles con sus columnas:{nl}{blocks}";
    }

    private static string DescribeColumns(IReadOnlyList<DatasetColumn> columns)
    {
        if (columns.Count == 0)
        {
            return "(el catálogo no expone columnas para este dataset)";
        }

        return string.Join(
            "; ",
            columns.Select(column =>
                $"{column.Name} ({column.DataType}) — " +
                $"{(string.IsNullOrWhiteSpace(column.Description) ? "sin descripción" : column.Description)}"));
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
