using JYDE.OpenDataCopilot.Domain.Catalog;

namespace JYDE.OpenDataCopilot.Application.Catalog;

/// <summary>DTO de aplicación para una columna de dataset (sin exponer el dominio).</summary>
/// <param name="Name">Nombre legible.</param>
/// <param name="FieldName">Nombre técnico del campo (SoQL).</param>
/// <param name="DataType">Tipo de dato.</param>
/// <param name="Description">Descripción opcional.</param>
public sealed record DatasetColumnDto(string Name, string FieldName, string DataType, string? Description)
{
    /// <summary>Crea el DTO a partir de la columna de dominio.</summary>
    /// <param name="column">Columna de dominio.</param>
    public static DatasetColumnDto From(DatasetColumn column)
        => new(column.Name, column.FieldName, column.DataType, column.Description);
}
