import type { ReactElement } from "react";
import type { ChatSource } from "../../../shared/api/client.ts";
import { useCopilotStore } from "../state/useCopilotStore.ts";

/** Fuentes citadas (datasets) de una respuesta; se pueden «fijar» a la memoria de la conversación. */
export function Sources({ sources }: { readonly sources: ReadonlyArray<ChatSource> }): ReactElement {
  const selectedDatasets = useCopilotStore((state) => state.selectedDatasets);
  const pinDataset = useCopilotStore((state) => state.pinDataset);
  const unpinDataset = useCopilotStore((state) => state.unpinDataset);

  const isPinned = (id: string): boolean => selectedDatasets.some((selected) => selected.id === id);

  return (
    <div className="rounded-xl border border-night-line bg-night-2 p-3">
      <p className="mb-2 font-mono text-[11px] uppercase tracking-[0.18em] text-night-muted">Fuentes citadas</p>
      <ul className="space-y-1.5">
        {sources.map((source) => {
          const pinned: boolean = isPinned(source.datasetId);
          return (
            <li key={source.datasetId} className="flex items-center justify-between gap-3 text-sm">
              <div className="flex min-w-0 items-center gap-2">
                {source.sourceUrl === null ? (
                  <span className="truncate text-night-soft">{source.name}</span>
                ) : (
                  <a
                    href={source.sourceUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="truncate text-sky hover:underline"
                  >
                    {source.name}
                  </a>
                )}
                <span className="shrink-0 font-mono text-xs text-night-muted">{(source.score * 100).toFixed(0)}%</span>
              </div>
              <button
                type="button"
                onClick={(): void =>
                  pinned ? unpinDataset(source.datasetId) : pinDataset({ id: source.datasetId, name: source.name })
                }
                aria-label={pinned ? `Quitar ${source.name} de la memoria` : `Fijar ${source.name} en la memoria`}
                className={`shrink-0 rounded-full border px-2.5 py-1 text-xs transition ${
                  pinned
                    ? "border-sky/50 bg-sky/10 text-sky"
                    : "border-night-line text-night-muted hover:border-sky/50 hover:text-night-ink"
                }`}
              >
                {pinned ? "✓ Fijado" : "Fijar"}
              </button>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
