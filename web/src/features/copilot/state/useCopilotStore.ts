import { create } from "zustand";
import {
  catalogApi,
  chatApi,
  searchApi,
  type ChatCategory,
  type ChatEvent,
  type ChatSource,
  type ChatTable,
} from "../../../shared/api/client.ts";

/** Artefacto de tabla en el panel de artefactos. */
export interface TableArtifact {
  readonly id: string;
  readonly kind: "table";
  readonly title: string;
  readonly columns: ReadonlyArray<string>;
  readonly rows: ReadonlyArray<ReadonlyArray<string>>;
}

/** Artefacto de gráfico (lleva su propia copia de los datos para dibujarse). */
export interface ChartArtifact {
  readonly id: string;
  readonly kind: "chart";
  readonly title: string;
  readonly type: string;
  readonly xColumn: string;
  readonly yColumn: string;
  readonly columns: ReadonlyArray<string>;
  readonly rows: ReadonlyArray<ReadonlyArray<string>>;
}

/** Artefacto mostrado en el panel lateral (tabla o gráfico). */
export type Artifact = TableArtifact | ChartArtifact;

/** Panel derecho activo (acoplado; excluyentes). "none" = ninguno. */
export type RightPanel = "none" | "memory" | "artifacts" | "audit";

/** Interacción cruda con un agente (auditoría). */
export interface AuditInteraction {
  readonly agent: string;
  readonly request: string;
  readonly response: string;
}

/** Entrada de auditoría de un turno: el mensaje del usuario y las interacciones de los agentes. */
export interface AuditEntry {
  readonly id: string;
  readonly userMessage: string;
  readonly interactions: ReadonlyArray<AuditInteraction>;
}

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
  /** Categorías recomendadas para cargar (acciones sugeridas). */
  readonly categories?: ReadonlyArray<ChatCategory>;
  /** Consulta a reintentar tras cargar una categoría. */
  readonly query?: string;
}

/** Dataset que el usuario mantiene seleccionado (memoria de la conversación). */
export interface SelectedDataset {
  readonly id: string;
  readonly name: string;
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
  /** Categorías recomendadas del turno en curso. */
  readonly streamingCategories: ReadonlyArray<ChatCategory> | null;
  /** Consulta a reintentar del turno en curso. */
  readonly streamingQuery: string | null;
  /** Categoría que se está cargando (ingesta + reindexado) desde un botón; null si ninguna. */
  readonly loadingCategory: string | null;
  /** Categorías cargadas durante la sesión (para deshabilitar botones ya cargados). */
  readonly loadedCategories: ReadonlyArray<string>;
  /** Objetivo acumulado de la conversación (memoria); lo actualiza el backend y es editable. */
  readonly objective: string;
  /** Datasets que el usuario mantiene seleccionados (memoria). */
  readonly selectedDatasets: ReadonlyArray<SelectedDataset>;
  /** Artefactos generados (tablas y gráficos) para el panel lateral. */
  readonly artifacts: ReadonlyArray<Artifact>;
  /** Bitácora de auditoría: interacciones crudas por turno. */
  readonly auditLog: ReadonlyArray<AuditEntry>;
  /** Panel derecho acoplado activo (memoria / artefactos / auditoría / ninguno). */
  readonly rightPanel: RightPanel;
  /** Última tabla del turno en curso (para asociarle un gráfico). */
  readonly streamingTable: ChatTable | null;
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
  /** Carga una categoría (ingesta + reindexado) y reintenta la consulta original. */
  readonly loadCategoryAndRetry: (categoryName: string, query: string) => Promise<void>;
  /** Edita manualmente el objetivo (memoria). */
  readonly setObjective: (value: string) => void;
  /** Agrega un dataset a los seleccionados (si no está ya). */
  readonly pinDataset: (dataset: SelectedDataset) => void;
  /** Quita un dataset de los seleccionados. */
  readonly unpinDataset: (id: string) => void;
  /** Limpia la memoria (objetivo y datasets seleccionados). */
  readonly clearMemory: () => void;
  /** Alterna el panel derecho indicado (si ya está abierto, lo cierra). */
  readonly togglePanel: (panel: RightPanel) => void;
  /** Cierra el panel derecho. */
  readonly closePanel: () => void;
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
  | "streamingCategories"
  | "streamingQuery"
  | "loadingCategory"
  | "loadedCategories"
  | "objective"
  | "selectedDatasets"
  | "artifacts"
  | "auditLog"
  | "rightPanel"
  | "streamingTable"
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
    streamingCategories: null,
    streamingQuery: null,
    loadingCategory: null,
    loadedCategories: [],
    objective: "",
    selectedDatasets: [],
    artifacts: [],
    auditLog: [],
    rightPanel: "none",
    streamingTable: null,
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

/** Agentes que trabajan sobre un dataset concreto: al citarlo, se auto-fija en la memoria. */
const ANALYSIS_AGENTS: readonly string[] = ["dataset-analyst-agent", "figures-agent"];

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
      // loadedCategories refleja el estado del catálogo (global), no del hilo: se conserva.
      set({
        ...initialCopilotState(),
        conversations: [fresh, ...get().conversations],
        activeId: fresh.id,
        loadedCategories: get().loadedCategories,
      });
    },

    selectConversation: (id: string): void => {
      if (get().status === "streaming") {
        return;
      }
      set({
        ...initialCopilotState(),
        conversations: get().conversations,
        activeId: id,
        loadedCategories: get().loadedCategories,
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
      set({
        input: "",
        status: "streaming",
        agent: null,
        streamingAnswer: "",
        streamingSources: null,
        streamingCategories: null,
        streamingQuery: null,
        streamingTable: null,
        error: null,
      });

      const threadId: string | null =
        get().conversations.find((c) => c.id === activeId)?.threadId ?? null;
      const controller: AbortController = new AbortController();
      // Datasets fijados (id + nombre): el backend los antepone como candidatos con su esquema.
      const pinnedDatasets: ReadonlyArray<SelectedDataset> = get().selectedDatasets;
      // Contexto de enrutamiento: la respuesta anterior del Copilot (para desambiguar un "sí").
      const priorMessages: ReadonlyArray<CopilotMessage> =
        get().conversations.find((c) => c.id === activeId)?.messages ?? [];
      const routeContext: string =
        [...priorMessages].reverse().find((message) => message.role === "assistant")?.content.slice(0, 800) ?? "";
      try {
        for await (const event of chatApi.stream(
          question, threadId, controller.signal, get().objective, pinnedDatasets, routeContext,
        )) {
          applyEvent(event);
        }

        const answer: CopilotMessage = {
          id: crypto.randomUUID(),
          role: "assistant",
          content: get().streamingAnswer,
          agent: get().agent ?? undefined,
          sources: get().streamingSources ?? undefined,
          categories: get().streamingCategories ?? undefined,
          query: get().streamingQuery ?? undefined,
        };
        patchConversation(activeId, (conversation) => ({
          ...conversation,
          messages: [...conversation.messages, answer],
        }));
        set({
          status: "idle",
          agent: null,
          streamingAnswer: "",
          streamingSources: null,
          streamingCategories: null,
          streamingQuery: null,
        });
      } catch (error: unknown) {
        set({ status: "error", error: describe(error) });
      }

      function applyEvent(event: ChatEvent): void {
        switch (event.kind) {
          case "agent":
            set({ agent: event.agent });
            break;
          case "sources": {
            set({ streamingSources: event.sources });
            // Auto-fija el dataset analizado: si el agente en curso analiza/consulta un dataset
            // concreto, el usuario está trabajando sobre él → lo mantenemos en memoria (idempotente).
            const currentAgent: string | null = get().agent;
            const top = event.sources[0];
            if (top !== undefined && currentAgent !== null && ANALYSIS_AGENTS.includes(currentAgent)) {
              get().pinDataset({ id: top.datasetId, name: top.name });
            }
            break;
          }
          case "categories":
            set({ streamingCategories: event.categories, streamingQuery: event.query });
            break;
          case "objective":
            set({ objective: event.objective });
            break;
          case "table": {
            const table: TableArtifact = {
              id: crypto.randomUUID(),
              kind: "table",
              title: event.table.title,
              columns: event.table.columns,
              rows: event.table.rows,
            };
            set({ streamingTable: event.table, artifacts: [...get().artifacts, table], rightPanel: "artifacts" });
            break;
          }
          case "chart": {
            const source: ChatTable | null = get().streamingTable;
            const chart: ChartArtifact = {
              id: crypto.randomUUID(),
              kind: "chart",
              title: event.chart.title,
              type: event.chart.type,
              xColumn: event.chart.xColumn,
              yColumn: event.chart.yColumn,
              columns: source?.columns ?? [],
              rows: source?.rows ?? [],
            };
            set({ artifacts: [...get().artifacts, chart], rightPanel: "artifacts" });
            break;
          }
          case "audit":
            set({
              auditLog: [
                ...get().auditLog,
                { id: crypto.randomUUID(), userMessage: question, interactions: event.interactions },
              ],
            });
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

    loadCategoryAndRetry: async (categoryName: string, query: string): Promise<void> => {
      if (get().loadingCategory !== null || get().status === "streaming") {
        return;
      }
      set({ loadingCategory: categoryName, error: null });
      try {
        await catalogApi.ingest({ categories: [categoryName] });
        await searchApi.buildIndex();
        const alreadyTracked: boolean = get().loadedCategories.includes(categoryName);
        set({
          loadingCategory: null,
          input: query,
          loadedCategories: alreadyTracked ? get().loadedCategories : [...get().loadedCategories, categoryName],
        });
        await get().send();
      } catch (error: unknown) {
        set({ loadingCategory: null, status: "error", error: describe(error) });
      }
    },

    setObjective: (value: string): void => set({ objective: value }),

    pinDataset: (dataset: SelectedDataset): void => {
      if (get().selectedDatasets.some((selected) => selected.id === dataset.id)) {
        return;
      }
      set({ selectedDatasets: [...get().selectedDatasets, dataset] });
    },

    unpinDataset: (id: string): void =>
      set({ selectedDatasets: get().selectedDatasets.filter((selected) => selected.id !== id) }),

    clearMemory: (): void => set({ objective: "", selectedDatasets: [] }),

    togglePanel: (panel: RightPanel): void => set({ rightPanel: get().rightPanel === panel ? "none" : panel }),

    closePanel: (): void => set({ rightPanel: "none" }),
  };
});
