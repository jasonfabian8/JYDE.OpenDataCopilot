using JYDE.OpenDataCopilot.Application.Catalog;
using JYDE.OpenDataCopilot.Application.Search;
using JYDE.OpenDataCopilot.Domain.Catalog;

namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Resuelve los datasets candidatos de una consulta y trae su esquema completo (columnas) del
/// catálogo. Prioriza los datasets que el usuario mantiene FIJADOS (por id): van primero y SIEMPRE
/// se incluyen, aunque la búsqueda semántica no los devuelva; luego agrega los mejores por búsqueda
/// semántica. Deduplica por id conservando ese orden de prioridad. Así el analista/cifras siempre
/// dispone del dataset que el usuario eligió, no solo de lo que la consulta (a veces vacía) recupere.
/// </summary>
internal static class DatasetCandidates
{
    /// <summary>Resuelve los candidatos (fijados primero, luego semánticos), con su esquema completo.</summary>
    /// <param name="context">Contexto de la conversación (consulta, TopK y datasets fijados).</param>
    /// <param name="embeddings">Generador de embeddings.</param>
    /// <param name="index">Índice de búsqueda de datasets.</param>
    /// <param name="repository">Repositorio del catálogo (metadatos completos con columnas).</param>
    /// <param name="candidateCount">Mínimo de candidatos semánticos a considerar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    public static async Task<IReadOnlyList<Dataset>> ResolveAsync(
        ConversationContext context,
        IEmbeddingGenerator embeddings,
        IDatasetSearchIndex index,
        ICatalogRepository repository,
        int candidateCount,
        CancellationToken cancellationToken)
    {
        int candidates = Math.Max(context.TopK, candidateCount);
        IReadOnlyList<float> queryEmbedding = await embeddings.GenerateAsync(context.Question, cancellationToken);
        IReadOnlyList<DatasetSearchHit> hits = await index.SearchAsync(queryEmbedding, candidates, cancellationToken);

        List<string> orderedIds = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        // 1) Datasets fijados por el usuario: prioridad máxima (van primero y siempre se incluyen).
        foreach (SelectedDataset pinned in context.SelectedDatasets ?? [])
        {
            string id = pinned.Id?.Trim() ?? string.Empty;
            if (id.Length > 0 && seen.Add(id))
            {
                orderedIds.Add(id);
            }
        }

        // 2) Mejores candidatos por búsqueda semántica (sin repetir los ya fijados).
        foreach (DatasetSearchHit hit in hits)
        {
            if (seen.Add(hit.Id))
            {
                orderedIds.Add(hit.Id);
            }
        }

        List<Dataset> datasets = [];
        foreach (string rawId in orderedIds)
        {
            DatasetId? id = TryCreateId(rawId);
            if (id is null)
            {
                continue;
            }

            Dataset? dataset = await repository.GetByIdAsync(id, cancellationToken);
            if (dataset is not null)
            {
                datasets.Add(dataset);
            }
        }

        return datasets;
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
}
