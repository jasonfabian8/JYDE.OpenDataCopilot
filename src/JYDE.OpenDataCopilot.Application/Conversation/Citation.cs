namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>Fuente citada en una respuesta: el dataset usado y su enlace público.</summary>
/// <param name="DatasetId">Identificador del dataset.</param>
/// <param name="Name">Nombre del dataset.</param>
/// <param name="SourceUrl">URL pública del dataset (la cita).</param>
/// <param name="Score">Relevancia del dataset para la consulta.</param>
public sealed record Citation(string DatasetId, string Name, string? SourceUrl, double Score);
