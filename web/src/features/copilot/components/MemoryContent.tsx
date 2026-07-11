import type { ReactElement } from "react";
import { useCopilotStore, type SelectedDataset } from "../state/useCopilotStore.ts";
import { CloseIcon } from "./icons.tsx";

/**
 * Contenido del panel de memoria: el objetivo acumulado (editable) y los datasets seleccionados
 * (removibles). Se muestra dentro del dock derecho; ayuda a no perder el hilo en conversaciones largas.
 */
export function MemoryContent(): ReactElement {
  const objective: string = useCopilotStore((state) => state.objective);
  const selectedDatasets: ReadonlyArray<SelectedDataset> = useCopilotStore((state) => state.selectedDatasets);
  const setObjective = useCopilotStore((state) => state.setObjective);
  const unpinDataset = useCopilotStore((state) => state.unpinDataset);
  const clearMemory = useCopilotStore((state) => state.clearMemory);

  return (
    <div className="space-y-6">
      <section>
        <label className="font-mono text-[11px] uppercase tracking-[0.18em] text-night-muted" htmlFor="memory-objective">
          Objetivo de la conversación
        </label>
        <textarea
          id="memory-objective"
          value={objective}
          onChange={(event): void => setObjective(event.target.value)}
          rows={5}
          placeholder="Aún no hay objetivo. Se irá actualizando con la conversación (editable)."
          className="mt-2 w-full resize-none rounded-lg border border-night-line bg-night-3 px-3 py-2 text-sm leading-relaxed text-night-ink placeholder:text-night-muted focus:border-sky/50 focus:outline-none"
        />
      </section>

      <section>
        <p className="font-mono text-[11px] uppercase tracking-[0.18em] text-night-muted">
          Datasets seleccionados · {selectedDatasets.length}
        </p>
        {selectedDatasets.length === 0 ? (
          <p className="mt-2 text-sm text-night-muted">
            Fija datasets desde las «Fuentes citadas» para mantenerlos presentes.
          </p>
        ) : (
          <ul className="mt-2 space-y-1.5">
            {selectedDatasets.map((dataset) => (
              <li
                key={dataset.id}
                className="flex items-center justify-between gap-2 rounded-lg border border-night-line bg-night-3 px-3 py-2 text-sm"
              >
                <span className="truncate text-night-ink">{dataset.name}</span>
                <button
                  type="button"
                  onClick={(): void => unpinDataset(dataset.id)}
                  aria-label={`Quitar ${dataset.name}`}
                  className="shrink-0 rounded p-1 text-night-muted transition hover:bg-night-2 hover:text-night-ink"
                >
                  <CloseIcon className="h-4 w-4" />
                </button>
              </li>
            ))}
          </ul>
        )}
      </section>

      {(objective.length > 0 || selectedDatasets.length > 0) && (
        <button
          type="button"
          onClick={(): void => clearMemory()}
          className="w-full rounded-lg border border-night-line px-4 py-2.5 text-sm font-medium text-night-soft transition hover:border-amber/50 hover:text-night-ink"
        >
          Limpiar memoria
        </button>
      )}
    </div>
  );
}
