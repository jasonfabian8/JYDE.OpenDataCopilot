import { create } from "zustand";
import { catalogApi, searchApi } from "../../../shared/api/client.ts";

/** Fase de una acción de ejecución. */
export type ActionPhase = "idle" | "running" | "ok" | "error";

/** Estado de la consola de operaciones (ver ADR-0008). */
interface OperationsState {
  /** Cantidad de datasets en el catálogo (null si aún no se consulta). */
  readonly count: number | null;
  /** Límite de datasets a ingerir en "Actualizar catálogo". */
  readonly ingestLimit: number;
  /** Fase de la acción "Actualizar catálogo". */
  readonly catalogPhase: ActionPhase;
  /** Fase de la acción "Reconstruir índice". */
  readonly indexPhase: ActionPhase;
  /** Último mensaje de resultado o error. */
  readonly message: string | null;
  /** Ajusta el límite de ingesta. */
  readonly setIngestLimit: (limit: number) => void;
  /** Consulta y refresca el conteo de datasets. */
  readonly refreshCount: () => Promise<void>;
  /** Ejecuta la ingesta del catálogo y refresca el conteo. */
  readonly updateCatalog: () => Promise<void>;
  /** Reconstruye el índice de búsqueda. */
  readonly rebuildIndex: () => Promise<void>;
}

function describe(error: unknown): string {
  return error instanceof Error ? error.message : "Ocurrió un error inesperado.";
}

export const useOperationsStore = create<OperationsState>((set, get) => ({
  count: null,
  ingestLimit: 1000,
  catalogPhase: "idle",
  indexPhase: "idle",
  message: null,

  setIngestLimit: (limit: number): void => set({ ingestLimit: Math.max(1, limit) }),

  refreshCount: async (): Promise<void> => {
    try {
      const result = await catalogApi.count();
      set({ count: result.count });
    } catch (error: unknown) {
      set({ message: describe(error) });
    }
  },

  updateCatalog: async (): Promise<void> => {
    set({ catalogPhase: "running", message: null });
    try {
      const result = await catalogApi.ingest(get().ingestLimit);
      set({
        catalogPhase: "ok",
        message: `Catálogo actualizado: ${result.datasetsIngested} datasets ingeridos.`,
      });
      await get().refreshCount();
    } catch (error: unknown) {
      set({ catalogPhase: "error", message: describe(error) });
    }
  },

  rebuildIndex: async (): Promise<void> => {
    set({ indexPhase: "running", message: null });
    try {
      const result = await searchApi.buildIndex();
      set({ indexPhase: "ok", message: `Índice reconstruido: ${result.indexed} datasets.` });
    } catch (error: unknown) {
      set({ indexPhase: "error", message: describe(error) });
    }
  },
}));
