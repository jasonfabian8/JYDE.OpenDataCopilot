import { create } from "zustand";
import { chatApi, type ChatEvent, type ChatSource } from "../../../shared/api/client.ts";

/** Mensaje de la conversación. */
export interface ChatMessage {
  /** Identificador estable del mensaje (clave de render). */
  readonly id: string;
  readonly role: "user" | "assistant";
  readonly content: string;
  readonly agent?: string;
  readonly sources?: ReadonlyArray<ChatSource>;
}

/** Estado del chat con el Copilot (ver ADR-0008 y ADR-0015). */
interface ChatState {
  /** Turnos finalizados. */
  readonly messages: ReadonlyArray<ChatMessage>;
  /** Texto en el cuadro de entrada. */
  readonly input: string;
  /** Estado del flujo. */
  readonly status: "idle" | "streaming" | "error";
  /** Agente que está atendiendo (durante el streaming). */
  readonly agent: string | null;
  /** Respuesta parcial que se está transmitiendo. */
  readonly streamingAnswer: string;
  /** Fuentes citadas del turno en curso. */
  readonly sources: ReadonlyArray<ChatSource> | null;
  /** Id del hilo de conversación con Foundry (memoria); null si es nuevo. */
  readonly conversationId: string | null;
  /** Último error, si lo hubo. */
  readonly error: string | null;
  /** Actualiza el texto de entrada. */
  readonly setInput: (value: string) => void;
  /** Envía la pregunta actual y transmite la respuesta. */
  readonly send: () => Promise<void>;
  /** Reinicia la conversación (nuevo hilo). */
  readonly newConversation: () => void;
}

function describe(error: unknown): string {
  return error instanceof Error ? error.message : "Ocurrió un error inesperado.";
}

export const useChatStore = create<ChatState>((set, get) => ({
  messages: [],
  input: "",
  status: "idle",
  agent: null,
  streamingAnswer: "",
  sources: null,
  conversationId: null,
  error: null,

  setInput: (value: string): void => set({ input: value }),

  newConversation: (): void =>
    set({
      messages: [],
      input: "",
      status: "idle",
      agent: null,
      streamingAnswer: "",
      sources: null,
      conversationId: null,
      error: null,
    }),

  send: async (): Promise<void> => {
    const question: string = get().input.trim();
    if (question.length === 0 || get().status === "streaming") {
      return;
    }

    const userMessage: ChatMessage = { id: crypto.randomUUID(), role: "user", content: question };
    set({
      messages: [...get().messages, userMessage],
      input: "",
      status: "streaming",
      agent: null,
      streamingAnswer: "",
      sources: null,
      error: null,
    });

    const controller: AbortController = new AbortController();
    try {
      for await (const event of chatApi.stream(question, get().conversationId, controller.signal)) {
        applyEvent(event);
      }

      const answer: ChatMessage = {
        id: crypto.randomUUID(),
        role: "assistant",
        content: get().streamingAnswer,
        agent: get().agent ?? undefined,
        sources: get().sources ?? undefined,
      };
      set({
        messages: [...get().messages, answer],
        status: "idle",
        agent: null,
        streamingAnswer: "",
        sources: null,
      });
    } catch (error: unknown) {
      set({ status: "error", error: describe(error) });
    }

    function applyEvent(event: ChatEvent): void {
      switch (event.kind) {
        case "agent":
          set({ agent: event.agent });
          break;
        case "sources":
          set({ sources: event.sources });
          break;
        case "token":
          set({ streamingAnswer: get().streamingAnswer + event.text });
          break;
        case "conversation":
          set({ conversationId: event.conversationId });
          break;
        case "done":
          break;
      }
    }
  },
}));
