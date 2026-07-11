import { useEffect, type ReactElement } from "react";
import { useSettingsStore } from "../state/useSettingsStore.ts";
import type { CatalogCategory } from "../../../shared/api/client.ts";
import { CloseIcon } from "./icons.tsx";

/** Modal de Configuración: operación del catálogo (ingesta por categorías) y del índice. */
export function SettingsModal(): ReactElement | null {
  const open: boolean = useSettingsStore((state) => state.open);
  const count: number | null = useSettingsStore((state) => state.count);
  const categories: ReadonlyArray<CatalogCategory> = useSettingsStore((state) => state.categories);
  const selected: ReadonlySet<string> = useSettingsStore((state) => state.selected);
  const loadingCategories: boolean = useSettingsStore((state) => state.loadingCategories);
  const phase: string = useSettingsStore((state) => state.phase);
  const message: string | null = useSettingsStore((state) => state.message);
  const closeSettings = useSettingsStore((state) => state.closeSettings);
  const refreshCount = useSettingsStore((state) => state.refreshCount);
  const toggleCategory = useSettingsStore((state) => state.toggleCategory);
  const selectFocus = useSettingsStore((state) => state.selectFocus);
  const selectAll = useSettingsStore((state) => state.selectAll);
  const clearSelection = useSettingsStore((state) => state.clearSelection);
  const ingestSelected = useSettingsStore((state) => state.ingestSelected);
  const ingestAll = useSettingsStore((state) => state.ingestAll);
  const rebuildIndex = useSettingsStore((state) => state.rebuildIndex);

  useEffect((): (() => void) | undefined => {
    if (!open) {
      return undefined;
    }
    function onKey(event: KeyboardEvent): void {
      if (event.key === "Escape") {
        closeSettings();
      }
    }
    window.addEventListener("keydown", onKey);
    return (): void => window.removeEventListener("keydown", onKey);
  }, [open, closeSettings]);

  if (!open) {
    return null;
  }

  const running: boolean = phase === "running";

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <button type="button" aria-label="Cerrar" onClick={closeSettings} className="absolute inset-0 bg-black/60" />

      <div
        role="dialog"
        aria-modal="true"
        aria-label="Configuración"
        className="relative z-10 flex max-h-[85vh] w-full max-w-2xl flex-col overflow-hidden rounded-2xl border border-night-line bg-night-2 shadow-2xl"
      >
        <div className="flex shrink-0 items-center justify-between border-b border-night-line px-5 py-4">
          <h2 className="font-display text-xl font-medium text-night-ink">Configuración</h2>
          <button
            type="button"
            onClick={closeSettings}
            aria-label="Cerrar"
            className="rounded-md p-1.5 text-night-soft transition hover:bg-night-3 hover:text-night-ink"
          >
            <CloseIcon className="h-5 w-5" />
          </button>
        </div>

        <div className="flex-1 space-y-6 overflow-y-auto px-5 py-5">
          {/* Estado */}
          <section className="flex items-center justify-between">
            <div>
              <p className="font-mono text-[11px] uppercase tracking-[0.18em] text-night-muted">Datasets en el catálogo</p>
              <p className="mt-1 font-display text-3xl tabular-nums text-night-ink">
                {count === null ? "—" : count.toLocaleString("es-CO")}
              </p>
            </div>
            <button
              type="button"
              onClick={(): void => { void refreshCount(); }}
              className="font-mono text-xs uppercase tracking-[0.15em] text-sky hover:underline"
            >
              Refrescar
            </button>
          </section>

          {/* Cargar catálogo */}
          <section className="space-y-3">
            <h3 className="font-medium text-night-ink">Cargar catálogo</h3>
            <p className="text-sm leading-relaxed text-night-soft">
              La ingesta es <strong className="text-night-ink">acumulativa</strong> (agrega y actualiza por id;
              no borra lo existente). Tras cargar, <strong className="text-night-ink">reconstruye el índice</strong>.
            </p>

            <button
              type="button"
              onClick={(): void => { void ingestAll(); }}
              disabled={running}
              className="w-full rounded-lg bg-sky px-4 py-2.5 font-medium text-night transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {running ? "Trabajando…" : "Cargar todo el catálogo"}
            </button>

            <div className="flex items-center justify-between pt-1">
              <p className="font-mono text-[11px] uppercase tracking-[0.18em] text-night-muted">
                O por categoría · {selected.size} seleccionada(s)
              </p>
              <div className="flex gap-3 text-xs">
                <button type="button" onClick={selectFocus} className="text-sky hover:underline">Foco (5)</button>
                <button type="button" onClick={selectAll} className="text-night-soft hover:underline">Todas</button>
                <button type="button" onClick={clearSelection} className="text-night-soft hover:underline">Ninguna</button>
              </div>
            </div>

            <div className="max-h-56 overflow-y-auto rounded-lg border border-night-line">
              {loadingCategories ? (
                <p className="px-3 py-3 text-sm text-night-muted">Cargando categorías…</p>
              ) : categories.length === 0 ? (
                <p className="px-3 py-3 text-sm text-night-muted">No hay categorías (¿backend arriba?).</p>
              ) : (
                <ul className="divide-y divide-night-line">
                  {categories.map((category) => (
                    <li key={category.name}>
                      <label className="flex cursor-pointer items-center gap-3 px-3 py-2 text-sm transition hover:bg-night-3/60">
                        <input
                          type="checkbox"
                          checked={selected.has(category.name)}
                          onChange={(): void => toggleCategory(category.name)}
                          className="h-4 w-4 accent-sky"
                        />
                        <span className="flex-1 text-night-ink">{category.name}</span>
                        <span className="font-mono text-xs tabular-nums text-night-muted">
                          {category.count.toLocaleString("es-CO")}
                        </span>
                      </label>
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <button
              type="button"
              onClick={(): void => { void ingestSelected(); }}
              disabled={running || selected.size === 0}
              className="w-full rounded-lg border border-night-line bg-night-3 px-4 py-2.5 font-medium text-night-ink transition hover:border-sky/50 disabled:cursor-not-allowed disabled:opacity-50"
            >
              Cargar {selected.size} categoría(s) seleccionada(s)
            </button>
          </section>

          {/* Índice */}
          <section className="space-y-3 border-t border-night-line pt-5">
            <h3 className="font-medium text-night-ink">Índice de búsqueda</h3>
            <p className="text-sm leading-relaxed text-night-soft">
              Regenera los embeddings de todo lo almacenado (por lote) para que la búsqueda semántica los use.
            </p>
            <button
              type="button"
              onClick={(): void => { void rebuildIndex(); }}
              disabled={running}
              className="w-full rounded-lg border border-night-line px-4 py-2.5 font-medium text-night-ink transition hover:border-sky/50 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {running ? "Trabajando…" : "Reconstruir índice"}
            </button>
          </section>

          {message !== null && (
            <p
              role="status"
              className={`rounded-lg border px-4 py-3 text-sm ${
                phase === "error"
                  ? "border-amber/40 bg-amber/10 text-night-ink"
                  : "border-sky/40 bg-sky/10 text-night-ink"
              }`}
            >
              {message}
            </p>
          )}
        </div>
      </div>
    </div>
  );
}
