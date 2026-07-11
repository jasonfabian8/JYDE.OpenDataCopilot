import type { ReactElement } from "react";
import type { ChatCategory } from "../../../shared/api/client.ts";
import { useCopilotStore } from "../state/useCopilotStore.ts";

/** Categorías recomendadas como botones: al hacer clic se cargan (ingesta + reindexado) y se reintenta. */
export function CategoryActions({
  categories,
  query,
}: {
  readonly categories: ReadonlyArray<ChatCategory>;
  readonly query: string;
}): ReactElement {
  const loadingCategory: string | null = useCopilotStore((state) => state.loadingCategory);
  const status: string = useCopilotStore((state) => state.status);
  const loadedCategories: ReadonlyArray<string> = useCopilotStore((state) => state.loadedCategories);
  const loadCategoryAndRetry = useCopilotStore((state) => state.loadCategoryAndRetry);

  const busy: boolean = loadingCategory !== null || status === "streaming";
  const isLoaded = (name: string): boolean =>
    loadedCategories.some((loaded) => loaded.toLowerCase() === name.toLowerCase());

  return (
    <div className="rounded-xl border border-night-line bg-night-2 p-3">
      <p className="mb-2 font-mono text-[11px] uppercase tracking-[0.18em] text-night-muted">
        Categorías sugeridas · clic para cargar
      </p>
      <div className="flex flex-wrap gap-2">
        {categories.map((category) =>
          category.loaded || isLoaded(category.name) ? (
            <span
              key={category.name}
              className="inline-flex items-center gap-1.5 rounded-full border border-verde/40 bg-verde/10 px-3 py-1.5 text-sm text-night-soft"
            >
              ✓ {category.name} <span className="text-night-muted">(cargada)</span>
            </span>
          ) : (
            <button
              key={category.name}
              type="button"
              disabled={busy}
              onClick={(): void => { void loadCategoryAndRetry(category.name, query); }}
              className="inline-flex items-center gap-2 rounded-full border border-night-line bg-night-3 px-3 py-1.5 text-sm text-night-ink transition hover:border-sky/50 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {loadingCategory === category.name ? (
                "Cargando…"
              ) : (
                <>
                  <span>{category.name}</span>
                  <span className="font-mono text-xs text-night-muted">
                    {category.count.toLocaleString("es-CO")}
                  </span>
                </>
              )}
            </button>
          ),
        )}
      </div>
      {loadingCategory !== null && (
        <p className="mt-2 text-xs text-night-muted">
          Cargando «{loadingCategory}» y reconstruyendo el índice… vuelvo a buscar al terminar.
        </p>
      )}
    </div>
  );
}
