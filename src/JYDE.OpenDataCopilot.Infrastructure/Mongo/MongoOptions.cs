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
}
