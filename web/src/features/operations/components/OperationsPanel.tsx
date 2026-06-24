import { useEffect, type ReactElement } from "react";
import { SectionLabel } from "../../../shared/ui/SectionLabel.tsx";
import { useOperationsStore, type ActionPhase } from "../state/useOperationsStore.ts";

function phaseLabel(phase: ActionPhase, idle: string, running: string): string {
  return phase === "running" ? running : idle;
}

/** Sección con acciones de ejecución (operación del catálogo y del índice de búsqueda). */
export function OperationsPanel(): ReactElement {
  const count: number | null = useOperationsStore((state) => state.count);
  const ingestLimit: number = useOperationsStore((state) => state.ingestLimit);
  const catalogPhase: ActionPhase = useOperationsStore((state) => state.catalogPhase);
  const indexPhase: ActionPhase = useOperationsStore((state) => state.indexPhase);
  const message: string | null = useOperationsStore((state) => state.message);
  const setIngestLimit = useOperationsStore((state) => state.setIngestLimit);
  const refreshCount = useOperationsStore((state) => state.refreshCount);
  const updateCatalog = useOperationsStore((state) => state.updateCatalog);
  const rebuildIndex = useOperationsStore((state) => state.rebuildIndex);

  useEffect((): void => {
    void refreshCount();
  }, [refreshCount]);

  const busy: boolean = catalogPhase === "running" || indexPhase === "running";

  return (
    <section id="operaciones" className="border-b border-line bg-paper-2">
      <div className="mx-auto max-w-6xl px-6 py-20 lg:py-28">
        <SectionLabel>Operaciones</SectionLabel>
        <h2 className="mt-6 max-w-2xl font-display text-4xl font-medium tracking-tight sm:text-5xl">
          Consola de ejecución
        </h2>
        <p className="mt-4 max-w-2xl leading-relaxed text-ink-soft">
          Acciones para mantener el catálogo y el índice de búsqueda actualizados desde la fuente
          oficial (datos.gov.co).
        </p>

        <div className="mt-12 grid gap-6 lg:grid-cols-3">
          {/* Estado */}
          <div className="rounded-2xl border border-line bg-card p-6">
            <p className="font-mono text-xs uppercase tracking-[0.2em] text-muted">Datasets en catálogo</p>
            <p className="mt-3 font-display text-5xl font-medium tabular-nums">
              {count === null ? "—" : count.toLocaleString("es-CO")}
            </p>
            <button
              type="button"
              onClick={(): void => void refreshCount()}
              className="mt-4 font-mono text-xs uppercase tracking-[0.15em] text-cobalt hover:underline"
            >
              Refrescar
            </button>
          </div>

          {/* Actualizar catálogo */}
          <div className="rounded-2xl border border-line bg-card p-6">
            <h3 className="font-display text-xl font-medium">Actualizar catálogo</h3>
            <p className="mt-2 text-sm leading-relaxed text-ink-soft">
              Ingiere metadatos de datasets desde la API de Socrata.
            </p>
            <label className="mt-4 block font-mono text-xs uppercase tracking-[0.15em] text-muted">
              Límite
              <input
                type="number"
                min={1}
                value={ingestLimit}
                disabled={busy}
                onChange={(event): void => setIngestLimit(Number(event.target.value))}
                className="mt-1 w-full rounded-lg border border-line bg-paper px-3 py-2 font-sans text-base text-ink outline-none focus:border-cobalt"
              />
            </label>
            <button
              type="button"
              disabled={busy}
              onClick={(): void => void updateCatalog()}
              className="mt-4 w-full rounded-lg bg-cobalt px-4 py-2.5 font-medium text-white transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {phaseLabel(catalogPhase, "Actualizar catálogo", "Actualizando…")}
            </button>
          </div>

          {/* Reconstruir índice */}
          <div className="rounded-2xl border border-line bg-card p-6">
            <h3 className="font-display text-xl font-medium">Reconstruir índice</h3>
            <p className="mt-2 text-sm leading-relaxed text-ink-soft">
              Vuelve a indexar el catálogo para la búsqueda semántica.
            </p>
            <button
              type="button"
              disabled={busy}
              onClick={(): void => void rebuildIndex()}
              className="mt-4 w-full rounded-lg border border-ink px-4 py-2.5 font-medium text-ink transition hover:bg-ink hover:text-paper disabled:cursor-not-allowed disabled:opacity-50"
            >
              {phaseLabel(indexPhase, "Reconstruir índice", "Indexando…")}
            </button>
          </div>
        </div>

        {message !== null && (
          <p
            role="status"
            className={`mt-8 rounded-lg border px-4 py-3 text-sm ${
              catalogPhase === "error" || indexPhase === "error"
                ? "border-amber/40 bg-amber/10 text-ink"
                : "border-verde/40 bg-verde-soft text-ink"
            }`}
          >
            {message}
          </p>
        )}
      </div>
    </section>
  );
}
