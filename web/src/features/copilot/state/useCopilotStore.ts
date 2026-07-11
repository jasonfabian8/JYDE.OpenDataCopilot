import { create } from "zustand";
import { chatApi, type ChatEvent, type ChatSource } from "../../../shared/api/client.ts";

/** Mensaje de una conversación del Copilot. */
export interface CopilotMessage {
  /** Identificador estable del mensaje (clave de render). */
  readonly id: string;
  readonly role: "user" | "assistant";
  readonly content: string;
  /** Agente que respondió (solo en mensajes del asistente). */
  readonly agent?: string;
  /** Fuentes citadas (solo en mensajes del asistente). */
  readonly sources?: ReadonlyArray<ChatSource>;
}

/** Una conversación (hilo) mantenida en memoria durante la sesión actual. */
export interface Conversation {
  /** Id local de la conversación (no se persiste; el refresco reinicia todo). */
  readonly id: string;
  /** Título mostrado en la barra lateral (derivado del primer mensaje). */
  readonly title: string;
  /** Turnos de la conversación. */
  readonly messages: ReadonlyArray<CopilotMessage>;
  /** Id del hilo de Foundry para conservar la memoria (previous_response_id); null si es nuevo. */
  readonly threadId: string | null;
}

/** Estado del flujo de chat. */
export type CopilotStatus = "idle" | "streaming" | "error";

/** Estado del Copilot: varias conversaciones en memoria (ver ADR-0008 y ADR-0015). */
interface CopilotState {
  /** Todas las conversaciones de la sesión; la más reciente primero. */
  readonly conversations: ReadonlyArray<Conversation>;
  /** Id de la conversación activa. */
  readonly activeId: string;
  /** Texto del cuadro de entrada. */
  readonly input: string;
  /** Estado del flujo. */
  readonly status: CopilotStatus;
  /** Agente que atiende (durante el streaming). */
  readonly agent: string | null;
  /** Respuesta parcial en transmisión. */
  readonly streamingAnswer: string;
  /** Fuentes citadas del turno en curso. */
  readonly streamingSources: ReadonlyArray<ChatSource> | null;
  /** Último error, si lo hubo. */
  readonly error: string | null;
  /** Actualiza el texto de entrada. */
  readonly setInput: (value: string) => void;
  /** Inicia una conversación nueva (hilo vacío) y la activa. */
  readonly newConversation: () => void;
  /** Cambia la conversación activa. */
  readonly selectConversation: (id: string) => void;
  /** Envía la pregunta actual y transmite la respuesta. */
  readonly send: () => Promise<void>;
}

function newConversationRecord(): Conversation {
  return { id: crypto.randomUUID(), title: "Nuevo chat", messages: [], threadId: null };
}

/**
 * Estado inicial: una conversación vacía y activa. Se exporta para reiniciar el store en pruebas.
 */
export function initialCopilotState(): Pick<
  CopilotState,
  | "conversations"
  | "activeId"
  | "input"
  | "status"
  | "agent"
  | "streamingAnswer"
  | "streamingSources"
  | "error"
> {
  const conversation: Conversation = newConversationRecord();
  return {
    conversations: [conversation],
    activeId: conversation.id,
    input: "",
    status: "idle",
    agent: null,
    streamingAnswer: "",
    streamingSources: null,
    error: null,
  };
}

function deriveTitle(question: string): string {
  const clean: string = question.replace(/\s+/g, " ").trim();
  return clean.length <= 48 ? clean : `${clean.slice(0, 47)}…`;
}

function describe(error: unknown): string {
  return error instanceof Error ? error.message : "Ocurrió un error inesperado.";
}

export const useCopilotStore = create<CopilotState>((set, get) => {
  const patchConversation = (id: string, updater: (conversation: Conversation) => Conversation): void =>
    set({ conversations: get().conversations.map((c) => (c.id === id ? updater(c) : c)) });

  return {
    ...initialCopilotState(),

    setInput: (value: string): void => set({ input: value }),

    newConversation: (): void => {
      const active: Conversation | undefined = get().conversations.find((c) => c.id === get().activeId);
      if (active !== undefined && active.messages.length === 0) {
        // Ya estamos en un hilo vacío: no acumulamos conversaciones en blanco.
        set({ input: "" });
        return;
      }
      const fresh: Conversation = newConversationRecord();
      set({
        conversations: [fresh, ...get().conversations],
        activeId: fresh.id,
        input: "",
        status: "idle",
        agent: null,
        streamingAnswer: "",
        streamingSources: null,
        error: null,
      });
    },

    selectConversation: (id: string): void => {
      if (get().status === "streaming") {
        return;
      }
      set({
        activeId: id,
        input: "",
        status: "idle",
        agent: null,
        streamingAnswer: "",
        streamingSources: null,
        error: null,
      });
    },

    send: async (): Promise<void> => {
      const question: string = get().input.trim();
      if (question.length === 0 || get().status === "streaming") {
        return;
      }

      const activeId: string = get().activeId;
      const userMessage: CopilotMessage = { id: crypto.randomUUID(), role: "user", content: question };
      patchConversation(activeId, (conversation) => ({
        ...conversation,
        title: conversation.messages.length === 0 ? deriveTitle(question) : conversation.title,
        messages: [...conversation.messages, userMessage],
      }));
      set({ input: "", status: "streaming", agent: null, streamingAnswer: "", streamingSources: null, error: null });

      const threadId: string | null =
        get().conversations.find((c) => c.id === activeId)?.threadId ?? null;
      const controller: AbortController = new AbortController();
      try {
        for await (const event of chatApi.stream(question, threadId, controller.signal)) {
          applyEvent(event);
        }

        const answer: CopilotMessage = {
          id: crypto.randomUUID(),
          role: "assistant",
          content: get().streamingAnswer,
          agent: get().agent ?? undefined,
          sources: get().streamingSources ?? undefined,
        };
        patchConversation(activeId, (conversation) => ({
          ...conversation,
          messages: [...conversation.messages, answer],
        }));
        set({ status: "idle", agent: null, streamingAnswer: "", streamingSources: null });
      } catch (error: unknown) {
        set({ status: "error", error: describe(error) });
      }

      function applyEvent(event: ChatEvent): void {
        switch (event.kind) {
          case "agent":
            set({ agent: event.agent });
            break;
          case "sources":
            set({ streamingSources: event.sources });
            break;
          case "token":
            set({ streamingAnswer: get().streamingAnswer + event.text });
            break;
          case "conversation":
            patchConversation(activeId, (conversation) => ({ ...conversation, threadId: event.conversationId }));
            break;
          case "done":
            break;
        }
      }
    },
  };
});
