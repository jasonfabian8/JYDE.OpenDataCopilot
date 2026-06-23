using JYDE.OpenDataCopilot.Domain.Catalog;

namespace JYDE.OpenDataCopilot.Infrastructure.Mongo;

/// <summary>Modelo de persistencia (MongoDB) de una columna de dataset.</summary>
internal sealed class DatasetColumnDocument
{
    /// <summary>Nombre legible.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Nombre técnico del campo.</summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>Tipo de dato.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Descripción opcional.</summary>
    public string? Description { get; set; }

    /// <summary>Crea el documento a partir de la columna de dominio.</summary>
    public static DatasetColumnDocument FromDomain(DatasetColumn column) => new()
    {
        Name = column.Name,
        FieldName = column.FieldName,
        DataType = column.DataType,
        Description = column.Description,
    };

    /// <summary>Reconstruye la columna de dominio.</summary>
    public DatasetColumn ToDomain() => new(Name, FieldName, DataType, Description);
}
