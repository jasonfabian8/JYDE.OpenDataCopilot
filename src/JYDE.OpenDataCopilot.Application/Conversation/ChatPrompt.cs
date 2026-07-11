namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Solicitud al modelo de chat: el código del agente especializado a invocar, el contenido de
/// entrada (consulta del usuario + contexto recuperado) y el identificador de respuesta previo para
/// continuar el hilo. Las instrucciones del agente viven en el proveedor (agente publicado en Foundry).
/// </summary>
/// <param name="Agent">Código lógico del agente a invocar (p. ej. <c>dataset-recommender-agent</c>).</param>
/// <param name="Input">Contenido de entrada para el agente.</param>
/// <param name="PreviousResponseId">Identificador de la respuesta anterior (hilo); nulo si es nuevo.</param>
public sealed record ChatPrompt(string Agent, string Input, string? PreviousResponseId = null);
