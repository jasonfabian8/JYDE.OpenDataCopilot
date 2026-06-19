namespace JYDE.OpenDataCopilot.Domain.Catalog;

/// <summary>
/// Describe una columna de un dataset (metadato). Value object inmutable.
/// </summary>
/// <param name="Name">Nombre legible de la columna.</param>
/// <param name="FieldName">Nombre técnico del campo en la API (SoQL).</param>
/// <param name="DataType">Tipo de dato declarado por Socrata (p. ej. <c>text</c>, <c>number</c>).</param>
/// <param name="Description">Descripción opcional de la columna.</param>
public sealed record DatasetColumn(string Name, string FieldName, string DataType, string? Description = null);
