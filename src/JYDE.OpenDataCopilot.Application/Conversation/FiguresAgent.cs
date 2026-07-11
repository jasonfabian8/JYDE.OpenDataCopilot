using System.Runtime.CompilerServices;
using System.Text.Json;
using JYDE.OpenDataCopilot.Application.Catalog;
using JYDE.OpenDataCopilot.Application.Figures;
using JYDE.OpenDataCopilot.Application.Search;
using JYDE.OpenDataCopilot.Domain.Catalog;

namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Agente de cifras: consulta DATOS REALES de un dataset vía SoQL (SODA) para tabular y graficar
/// (conteos, sumas, tendencias). Resuelve el dataset, pide al LLM una consulta SoQL, la ejecuta a
/// través de <see cref="IDataQuery"/> y emite artefactos de tabla y (opcional) gráfico. Si la consulta
/// falla, lo explica con honestidad y no inventa cifras.
/// </summary>
public sealed class FiguresAgent : IConversationAgent
{
    /// <summary>Cantidad de datasets candidatos que se ofrecen al LLM para elegir.</summary>
    public const int CandidateCount = 3;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] ValidChartTypes = ["bar", "line"];

    private readonly IEmbeddingGenerator _embeddings;
    private readonly IDatasetSearchIndex _index;
    private readonly ICatalogRepository _repository;
    private readonly IChatCompletion _chat;
    private readonly IDataQuery _dataQuery;

    /// <summary>Crea el agente de cifras.</summary>
    /// <param name="embeddings">Generador de embeddings.</param>
    /// <param name="index">Índice de búsqueda de datasets.</param>
    /// <param name="repository">Repositorio del catálogo (metadatos con columnas).</param>
    /// <param name="chat">Modelo de chat (LLM).</param>
    /// <param name="dataQuery">Puerto de consulta de datos reales (SoQL).</param>
    /// <exception cref="ArgumentNullException">Si alguna dependencia es nula.</exception>
    public FiguresAgent(
        IEmbeddingGenerator embeddings,
        IDatasetSearchIndex index,
        ICatalogRepository repository,
        IChatCompletion chat,
        IDataQuery dataQuery)
    {
        ArgumentNullException.ThrowIfNull(embeddings);
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(chat);
        ArgumentNullException.ThrowIfNull(dataQuery);
        _embeddings = embeddings;
        _index = index;
        _repository = repository;
        _chat = chat;
        _dataQuery = dataQuery;
    }

    /// <inheritdoc />
    public string Name => "figures-agent";

    /// <inheritdoc />
    public string Description =>
        "Consulta datos reales de un dataset (SoQL) para tabular y graficar cifras: conteos, sumas, promedios y tendencias.";

    /// <inheritdoc />
    public bool CanHandle(string question)
    {
        string lowered = question.ToLowerInvariant();
        return lowered.Contains("cuánt")
            || lowered.Contains("cuant")
            || lowered.Contains("cifra")
            || lowered.Contains("total")
            || lowered.Contains("suma")
            || lowered.Contains("promedio")
            || lowered.Contains("gráfic")
            || lowered.Contains("grafic")
            || lowered.Contains("tabla")
            || lowered.Contains("tabular")
            || lowered.Contains("estadística")
            || lowered.Contains("estadistica")
            || lowered.Contains("tendencia")
            || lowered.Contains("ranking");
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

        // Candidatos con su esquema: PRIMERO los datasets fijados por el usuario, luego los mejores
        // por búsqueda semántica (así las cifras/gráficos contemplan el dataset que el usuario eligió).
        IReadOnlyList<Dataset> datasets =
            await DatasetCandidates.ResolveAsync(context, _embeddings, _index, _repository, CandidateCount, cancellationToken);
        if (datasets.Count == 0)
        {
            foreach (ConversationEvent noData in EmitText("No encontré un dataset con datos para consultar esta cifra. Prueba a describir mejor el tema o cargar la categoría correspondiente."))
            {
                yield return noData;
            }
            yield return ConversationEvent.Completed();
            yield break;
        }

        ChatPrompt prompt = new(Name, ContextHeader.For(context) + BuildInput(context.Question, datasets), context.PreviousResponseId);
        ChatResult result = await _chat.CompleteAsync(prompt, cancellationToken);
        FiguresReply? reply = TryParseReply(result.Text);

        if (reply is null || string.IsNullOrWhiteSpace(reply.Soql))
        {
            string fallback = reply?.Explicacion is { Length: > 0 } explanation
                ? explanation
                : "No pude construir una consulta para esa cifra. Reformula indicando qué quieres contar/sumar y por qué variable.";
            foreach (ConversationEvent chunk in EmitText(fallback))
            {
                yield return chunk;
            }
            if (!string.IsNullOrEmpty(result.ResponseId))
            {
                yield return ConversationEvent.ForConversation(result.ResponseId);
            }
            yield return ConversationEvent.Completed();
            yield break;
        }

        Dataset dataset = ChooseDataset(datasets, reply.DatasetId);
        (DataQueryResult? data, string? queryError) = await ExecuteAsync(dataset.Id.Value, reply.Soql, cancellationToken);

        foreach (ConversationEvent artifact in EmitData(dataset, data, reply.Chart))
        {
            yield return artifact;
        }

        foreach (ConversationEvent chunk in EmitText(BuildAnswer(data, dataset, reply, queryError)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return chunk;
        }

        if (!string.IsNullOrEmpty(result.ResponseId))
        {
            yield return ConversationEvent.ForConversation(result.ResponseId);
        }

        yield return ConversationEvent.Completed();
    }

    /// <summary>Emite las fuentes, la tabla y (si es válido) el gráfico cuando la consulta devolvió datos.</summary>
    /// <param name="dataset">Dataset consultado (para citarlo y titular la tabla).</param>
    /// <param name="data">Resultado de la consulta; <c>null</c> si falló (no emite nada).</param>
    /// <param name="chart">Especificación de gráfico propuesta por el LLM (puede ser inválida).</param>
    private static IEnumerable<ConversationEvent> EmitData(Dataset dataset, DataQueryResult? data, FiguresChart? chart)
    {
        if (data is null)
        {
            yield break;
        }

        yield return ConversationEvent.ForSources(
            [new Citation(dataset.Id.Value, dataset.Name, dataset.SourceUrl?.ToString(), 1.0)]);
        yield return ConversationEvent.ForTable(new TableArtifact(dataset.Name, data.Columns, data.Rows));

        ChartArtifact? built = BuildChart(dataset.Name, chart, data.Columns);
        if (built is not null)
        {
            yield return ConversationEvent.ForChart(built);
        }
    }

    /// <summary>
    /// Texto de cierre del turno: un error honesto (sin inventar cifras) si la consulta falló, o la
    /// explicación del LLM (o un texto por defecto si vino vacía) cuando sí hubo datos.
    /// </summary>
    /// <param name="data">Resultado de la consulta; <c>null</c> si falló.</param>
    /// <param name="dataset">Dataset consultado (para nombrarlo en el mensaje de error).</param>
    /// <param name="reply">Respuesta del LLM (SoQL y explicación).</param>
    /// <param name="queryError">Motivo del fallo de la consulta, si lo hubo.</param>
    private static string BuildAnswer(DataQueryResult? data, Dataset dataset, FiguresReply reply, string? queryError)
    {
        if (data is null)
        {
            return $"No pude ejecutar la consulta sobre «{dataset.Name}» ({queryError}). SoQL intentado: {reply.Soql}. Verifica que las columnas existan o reformula.";
        }

        return string.IsNullOrWhiteSpace(reply.Explicacion) ? "Aquí están los datos solicitados." : reply.Explicacion;
    }

    private async Task<(DataQueryResult? Data, string? Error)> ExecuteAsync(string datasetId, string soql, CancellationToken cancellationToken)
    {
        try
        {
            DataQueryResult data = await _dataQuery.QueryAsync(datasetId, soql, cancellationToken);
            return (data, null);
        }
        catch (HttpRequestException error)
        {
            return (null, error.StatusCode?.ToString() ?? "error de red");
        }
    }

    private static Dataset ChooseDataset(IReadOnlyList<Dataset> datasets, string? datasetId)
    {
        string wanted = (datasetId ?? string.Empty).Replace("id=", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        return datasets.FirstOrDefault(dataset => string.Equals(dataset.Id.Value, wanted, StringComparison.OrdinalIgnoreCase))
            ?? datasets[0];
    }

    private static ChartArtifact? BuildChart(string title, FiguresChart? chart, IReadOnlyList<string> columns)
    {
        if (chart is null || string.IsNullOrWhiteSpace(chart.Tipo) || string.IsNullOrWhiteSpace(chart.X) || string.IsNullOrWhiteSpace(chart.Y))
        {
            return null;
        }

        string type = chart.Tipo.Trim().ToLowerInvariant();
        if (!ValidChartTypes.Contains(type) || !HasColumn(columns, chart.X) || !HasColumn(columns, chart.Y))
        {
            return null;
        }

        return new ChartArtifact(title, type, chart.X.Trim(), chart.Y.Trim());
    }

    private static bool HasColumn(IReadOnlyList<string> columns, string name) =>
        columns.Any(column => string.Equals(column, name.Trim(), StringComparison.OrdinalIgnoreCase));

    private static FiguresReply? TryParseReply(string text)
    {
        string? json = JsonText.FirstJsonObject(text);
        if (json is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<FiguresReply>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string BuildInput(string question, IReadOnlyList<Dataset> datasets)
    {
        // Solo datos: reglas y esquema JSON viven en la instrucción del agente en Foundry.
        string nl = Environment.NewLine;
        string blocks = string.Join(
            nl,
            datasets.Select(dataset =>
                $"[id={dataset.Id.Value}] {dataset.Name}{nl}  columnas: {DescribeColumns(dataset.Columns)}"));

        return
            $"Consulta del ciudadano: {question}{nl}{nl}" +
            $"Datasets disponibles (elige uno y escribe SoQL para sus columnas):{nl}{blocks}";
    }

    private static string DescribeColumns(IReadOnlyList<DatasetColumn> columns)
    {
        if (columns.Count == 0)
        {
            return "(sin columnas conocidas)";
        }

        return string.Join(", ", columns.Select(column => $"{column.FieldName} ({column.DataType})"));
    }

    private static IEnumerable<ConversationEvent> EmitText(string text)
    {
        foreach (string word in text.Split(' '))
        {
            yield return ConversationEvent.ForToken(word + " ");
        }
    }
}
