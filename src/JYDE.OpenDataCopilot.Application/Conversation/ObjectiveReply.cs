namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>Respuesta estructurada (JSON) del rastreador de objetivo.</summary>
/// <param name="Objetivo">Objetivo acumulado y actualizado de la conversación.</param>
internal sealed record ObjectiveReply(string? Objetivo);
