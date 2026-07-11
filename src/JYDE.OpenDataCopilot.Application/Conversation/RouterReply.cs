namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>Respuesta estructurada (JSON) del enrutador LLM: el nombre del agente elegido.</summary>
/// <param name="Agente">Nombre exacto del agente que debe atender la consulta.</param>
internal sealed record RouterReply(string? Agente);
