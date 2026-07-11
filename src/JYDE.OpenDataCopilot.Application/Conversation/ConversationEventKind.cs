namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>Tipo de evento emitido durante una conversación (para el streaming SSE).</summary>
public enum ConversationEventKind
{
    /// <summary>Indica qué agente atiende la consulta.</summary>
    Agent,

    /// <summary>Fuentes citadas (datasets) que sustentan la respuesta.</summary>
    Sources,

    /// <summary>Categorías recomendadas para cargar (acciones sugeridas al usuario).</summary>
    Categories,

    /// <summary>Artefacto de tabla (datos tabulados) para el panel de artefactos.</summary>
    Table,

    /// <summary>Artefacto de gráfico para el panel de artefactos.</summary>
    Chart,

    /// <summary>Fragmento de texto (token) de la respuesta.</summary>
    Token,

    /// <summary>Identificador del hilo de conversación (para continuar en el siguiente turno).</summary>
    Conversation,

    /// <summary>Objetivo acumulado de la conversación (memoria), actualizado tras el turno.</summary>
    Objective,

    /// <summary>Interacciones crudas del turno con los agentes (auditoría).</summary>
    Audit,

    /// <summary>Fin de la respuesta.</summary>
    Done,
}
