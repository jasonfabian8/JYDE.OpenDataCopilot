namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>Tipo de evento emitido durante una conversación (para el streaming SSE).</summary>
public enum ConversationEventKind
{
    /// <summary>Indica qué agente atiende la consulta.</summary>
    Agent,

    /// <summary>Fuentes citadas (datasets) que sustentan la respuesta.</summary>
    Sources,

    /// <summary>Fragmento de texto (token) de la respuesta.</summary>
    Token,

    /// <summary>Identificador del hilo de conversación (para continuar en el siguiente turno).</summary>
    Conversation,

    /// <summary>Fin de la respuesta.</summary>
    Done,
}
