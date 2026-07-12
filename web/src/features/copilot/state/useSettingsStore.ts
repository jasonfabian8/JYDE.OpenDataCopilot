import { create } from "zustand";
import { catalogApi, searchApi, type CatalogCategory } from "../../../shared/api/client.ts";

/** Las 5 áreas del concurso (Datos al Ecosistema 2026): preseleccionadas por defecto. */
export const FOCUS_CATEGORIES: readonly string[] = [
  "Salud y Protección Social",
  "Seguridad y Defensa",
  "Transporte",
  "Educación",
  "Ambiente y Desarrollo Sostenible",
];

/** Fase de una operación (ingesta / reindexado). */
export type OperationPhase = "idle" | "running" | "ok" | "error";

/** Estado del panel de Configuración (operación del catálogo y del índice). */
interface SettingsState {
  /** Si el modal de configuración está abierto. */
  readonly open: boolean;
  /** Datasets en el catálogo (null si aún no se consulta). */
  readonly count: number | null;
  /** Categorías disponibles en la fuente (con su conteo). */
  readonly categories: ReadonlyArray<CatalogCategory>;
  /** Categorías seleccionadas para ingerir. */
  readonly selected: ReadonlySet<string>;
  /** Si se están cargando las categorías. */
  readonly loadingCategories: boolean;
  /** Fase de la última operación. */
  readonly phase: OperationPhase;
  /** Último mensaje de resultado o error. */
  readonly message: string | null;
  /** Abre el modal (y carga categorías/conteo si hace falta). */
  readonly openSettings: () => void;
  /** Cierra el modal. */
  readonly closeSettings: () => void;
  /** Consulta el conteo de datasets. */
  readonly refreshCount: () => Promise<void>;
  /** Carga las categorías y preselecciona las de foco. */
  readonly loadCategories: () => Promise<void>;
  /** Alterna una categoría en la selección. */
  readonly toggleCategory: (name: string) => void;
  /** Selecciona solo las 5 categorías de foco disponibles. */
  readonly selectFocus: () => void;
  /** Selecciona todas las categorías. */
  readonly selectAll: () => void;
  /** Limpia la selección. */
  readonly clearSelection: () => void;
  /** Ingiere las categorías seleccionadas. */
  readonly ingestSelected: () => Promise<void>;
  /** Ingiere el catálogo completo (sin filtro de categoría). */
  readonly ingestAll: () => Promise<void>;
  /** Reconstruye el índice de búsqueda. */
  readonly rebuildIndex: () => Promise<void>;
}

function describe(error: unknown): string {
  return error instanceof Error ? error.message : "Ocurrió un error inesperado.";
}

function focusOf(categories: ReadonlyArray<CatalogCategory>): ReadonlySet<string> {
  return new Set(categories.filter((category) => FOCUS_CATEGORIES.includes(category.name)).map((category) => category.name));
}

export const useSettingsStore = create<SettingsState>((set, get) => ({
  open: false,
  count: null,
  categories: [],
  selected: new Set(),
  loadingCategories: false,
  phase: "idle",
  message: null,

  openSettings: (): void => {
    set({ open: true });
    if (get().categories.length === 0) {
      get().loadCategories();
    }
    get().refreshCount();
  },

  closeSettings: (): void => set({ open: false }),

  refreshCount: async (): Promise<void> => {
    try {
      const result = await catalogApi.count();
      set({ count: result.count });
    } catch (error: unknown) {
      set({ phase: "error", message: describe(error) });
    }
  },

  loadCategories: async (): Promise<void> => {
    set({ loadingCategories: true });
    try {
      const categories = await catalogApi.categories();
      set({ categories, selected: focusOf(categories), loadingCategories: false });
    } catch (error: unknown) {
      set({ loadingCategories: false, phase: "error", message: describe(error) });
    }
  },

  toggleCategory: (name: string): void => {
    const next = new Set(get().selected);
    if (next.has(name)) {
      next.delete(name);
    } else {
      next.add(name);
    }
    set({ selected: next });
  },

  selectFocus: (): void => set({ selected: focusOf(get().categories) }),

  selectAll: (): void => set({ selected: new Set(get().categories.map((category) => category.name)) }),

  clearSelection: (): void => set({ selected: new Set() }),

  ingestSelected: async (): Promise<void> => {
    const categories: string[] = [...get().selected];
    if (categories.length === 0) {
      set({ phase: "error", message: "Selecciona al menos una categoría." });
      return;
    }
    set({ phase: "running", message: null });
    try {
      const result = await catalogApi.ingest({ categories });
      set({
        phase: "ok",
        message: `Ingeridos ${result.datasetsIngested} datasets de ${categories.length} categoría(s). Reconstruye el índice para que la búsqueda los use.`,
      });
      await get().refreshCount();
    } catch (error: unknown) {
      set({ phase: "error", message: describe(error) });
    }
  },

  ingestAll: async (): Promise<void> => {
    set({ phase: "running", message: null });
    try {
      const result = await catalogApi.ingest({});
      set({
        phase: "ok",
        message: `Ingeridos ${result.datasetsIngested} datasets (catálogo completo). Reconstruye el índice para que la búsqueda los use.`,
      });
      await get().refreshCount();
    } catch (error: unknown) {
      set({ phase: "error", message: describe(error) });
    }
  },

  rebuildIndex: async (): Promise<void> => {
    set({ phase: "running", message: null });
    try {
      const result = await searchApi.buildIndex();
      set({ phase: "ok", message: `Índice reconstruido: ${result.indexed} datasets.` });
    } catch (error: unknown) {
      set({ phase: "error", message: describe(error) });
    }
  },
}));
