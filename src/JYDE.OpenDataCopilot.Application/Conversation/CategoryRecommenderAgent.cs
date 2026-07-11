using System.Runtime.CompilerServices;
using System.Text.Json;
using JYDE.OpenDataCopilot.Application.Catalog;

namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Agente especializado en recomendar qué categorías del catálogo de datos.gov.co conviene cargar
/// según la necesidad del usuario. Recibe la lista completa de categorías (con su conteo) y cuáles
/// ya están cargadas, y pide al LLM una respuesta estructurada (JSON) con la relevancia de cada una.
/// Emite las categorías relevantes como acciones (botones) y una consulta a reintentar tras cargar.
/// </summary>
public sealed class CategoryRecommenderAgent : IConversationAgent
{
    /// <summary>Relevancia mínima (0-1) para recomendar una categoría.</summary>
    public const double DefaultRelevanceThreshold = 0.5;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ICatalogSource _source;
    private readonly ICatalogRepository _repository;
    private readonly IChatCompletion _chat;
    private readonly double _relevanceThreshold;

    /// <summary>Crea el agente de categorías.</summary>
    /// <param name="source">Fuente del catálogo (lista de categorías disponibles).</param>
    /// <param name="repository">Repositorio (categorías ya cargadas).</param>
    /// <param name="chat">Modelo de chat (LLM).</param>
    /// <param name="relevanceThreshold">Relevancia mínima para recomendar una categoría.</param>
    /// <exception cref="ArgumentNullException">Si alguna dependencia es nula.</exception>
    public CategoryRecommenderAgent(
        ICatalogSource source,
        ICatalogRepository repository,
        IChatCompletion chat,
        double relevanceThreshold = DefaultRelevanceThreshold)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(chat);
        _source = source;
        _repository = repository;
        _chat = chat;
        _relevanceThreshold = relevanceThreshold;
    }

    /// <inheritdoc />
    public string Name => "category-recommender-agent";

    /// <inheritdoc />
    public string Description =>
        "Recomienda qué categorías de datos.gov.co cargar cuando el catálogo actual no cubre la necesidad del usuario.";

    /// <inheritdoc />
    public bool CanHandle(string question)
    {
        string lowered = question.ToLowerInvariant();
        return lowered.Contains("categor") || lowered.Contains("carg") || lowered.Contains("descarg");
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

        IReadOnlyList<CatalogCategory> all = await _source.GetCategoriesAsync(cancellationToken);
        IReadOnlyList<string> loaded = await _repository.GetLoadedCategoriesAsync(cancellationToken);
        HashSet<string> loadedSet = new(loaded, StringComparer.OrdinalIgnoreCase);

        ChatPrompt prompt = new(Name, BuildInput(context.Question, all, loadedSet), context.PreviousResponseId);
        ChatResult result = await _chat.CompleteAsync(prompt, cancellationToken);

        (string answer, string query, IReadOnlyList<CategoryRecommendation> recommendations) =
            Interpret(result.Text, context.Question, all, loadedSet);

        if (recommendations.Count > 0)
        {
            yield return ConversationEvent.ForCategories(query, recommendations);
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

    private (string Answer, string Query, IReadOnlyList<CategoryRecommendation> Recommendations) Interpret(
        string text,
        string question,
        IReadOnlyList<CatalogCategory> all,
        HashSet<string> loadedSet)
    {
        CategoryRecommenderReply? reply = TryParseReply(text);
        if (reply is null)
        {
            return (text, question, []);
        }

        string answer = string.IsNullOrWhiteSpace(reply.Respuesta) ? text : reply.Respuesta;
        string query = string.IsNullOrWhiteSpace(reply.Consulta) ? question : reply.Consulta;

        // La fuente puede traer nombres que solo difieren en mayúsculas (p. ej. "Participación
        // ciudadana" / "Participación Ciudadana"): indexamos sin distinguir mayúsculas y, ante colisión,
        // conservamos la de mayor conteo (evita ArgumentException por clave duplicada).
        Dictionary<string, CatalogCategory> catalogByName = new(StringComparer.OrdinalIgnoreCase);
        foreach (CatalogCategory category in all)
        {
            if (!catalogByName.TryGetValue(category.Name, out CatalogCategory? existing) || category.Count > existing.Count)
            {
                catalogByName[category.Name] = category;
            }
        }

        IReadOnlyList<CategoryRecommendation> recommendations =
        [
            .. (reply.Categorias ?? [])
                .Where(scored => scored.Relevancia >= _relevanceThreshold
                    && !string.IsNullOrWhiteSpace(scored.Nombre)
                    && catalogByName.ContainsKey(scored.Nombre!))
                .Select(scored => new CategoryRecommendation(
                    catalogByName[scored.Nombre!].Name,
                    catalogByName[scored.Nombre!].Count,
                    loadedSet.Contains(scored.Nombre!),
                    scored.Relevancia))
                .OrderByDescending(recommendation => recommendation.Relevance)
                .DistinctBy(recommendation => recommendation.Name, StringComparer.OrdinalIgnoreCase)
        ];

        return (answer, query, recommendations);
    }

    private static CategoryRecommenderReply? TryParseReply(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CategoryRecommenderReply>(text[start..(end + 1)], JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string BuildInput(string question, IReadOnlyList<CatalogCategory> all, HashSet<string> loadedSet)
    {
        string nl = Environment.NewLine;
        string catalog = all.Count == 0
            ? "(no se pudo obtener la lista de categorías)"
            : string.Join(
                nl,
                all.Select(category =>
                    $"- {category.Name} | {category.Count} datasets | " +
                    $"{(loadedSet.Contains(category.Name) ? "CARGADA" : "sin cargar")}"));

        return
            $"Necesidad del ciudadano: {question}{nl}{nl}" +
            $"Categorías del catálogo de datos.gov.co (nombre | datasets | estado):{nl}{catalog}{nl}{nl}" +
            $"Responde ÚNICAMENTE con el JSON acordado (sin texto adicional, sin vallas de código):{nl}" +
            "{\"respuesta\": \"<en español: qué categorías cargar y por qué; si ya hay cargadas útiles, " +
            "menciónalo; no inventes>\", \"consulta\": \"<tema a buscar tras cargar, en pocas palabras>\", " +
            "\"categorias\": [{\"nombre\": \"<nombre EXACTO de la lista>\", \"relevancia\": <0.0-1.0>}]}";
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
