namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>Resultado de una invocación al modelo de chat.</summary>
/// <param name="Text">Texto de la respuesta.</param>
/// <param name="ResponseId">
/// Identificador de la respuesta del proveedor; permite continuar el hilo (threading) en el
/// siguiente turno. Puede ser nulo si el proveedor no soporta hilos.
/// </param>
public sealed record ChatResult(string Text, string? ResponseId);
