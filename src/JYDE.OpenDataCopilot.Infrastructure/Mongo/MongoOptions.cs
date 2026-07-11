namespace JYDE.OpenDataCopilot.Infrastructure.Mongo;

/// <summary>Opciones de conexión a MongoDB (Atlas).</summary>
public sealed class MongoOptions
{
    /// <summary>Clave de configuración asociada a estas opciones.</summary>
    public const string SectionName = "Mongo";

    /// <summary>Cadena de conexión (secreto; se inyecta fuera del repositorio versionado).</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Nombre de la base de datos.</summary>
    public string Database { get; set; } = "odc_BD";

    /// <summary>Colección donde se almacena el catálogo de datasets.</summary>
    public string CatalogCollection { get; set; } = "datasets";

    /// <summary>Colección donde se almacenan los vectores para la búsqueda.</summary>
    public string SearchCollection { get; set; } = "dataset_vectors";

    /// <summary>Nombre del índice de Atlas Vector Search.</summary>
    public string VectorIndexName { get; set; } = "dataset_vector_index";

    /// <summary>Dimensión de los embeddings (debe coincidir con el generador configurado).</summary>
    public int VectorDimensions { get; set; } = 256;

    /// <summary>Número de candidatos a considerar en la búsqueda vectorial aproximada.</summary>
    public int VectorNumCandidates { get; set; } = 100;
}
