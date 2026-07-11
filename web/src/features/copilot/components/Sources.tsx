import type { ReactElement } from "react";
import type { ChatSource } from "../../../shared/api/client.ts";

/** Fuentes citadas (datasets) de una respuesta del Copilot. */
export function Sources({ sources }: { readonly sources: ReadonlyArray<ChatSource> }): ReactElement {
  return (
    <div className="rounded-xl border border-night-line bg-night-2 p-3">
      <p className="mb-2 font-mono text-[11px] uppercase tracking-[0.18em] text-night-muted">Fuentes citadas</p>
      <ul className="space-y-1.5">
        {sources.map((source) => (
          <li key={source.datasetId} className="flex items-center justify-between gap-3 text-sm">
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
          </li>
        ))}
      </ul>
    </div>
  );
}
