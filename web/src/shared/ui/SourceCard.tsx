import type { ReactElement } from "react";
import type { DatasetSource } from "../../features/landing/model/DatasetSource.ts";

/** Props de {@link SourceCard}. */
interface SourceCardProps {
  /** Fuente oficial a mostrar. */
  readonly source: DatasetSource;
}

/**
 * Tarjeta que cita la fuente oficial de una respuesta. Materializa el guardrail
 * del producto: toda respuesta basada en datos muestra su origen verificable.
 */
export function SourceCard({ source }: SourceCardProps): ReactElement {
  return (
    <a
      href={source.url}
      target="_blank"
      rel="noopener noreferrer"
      className="group flex items-start gap-3 rounded-lg border border-verde/30 bg-verde-soft/60 px-4 py-3 transition-colors hover:border-verde/60"
    >
      <span
        className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-verde text-[11px] font-bold text-white"
        aria-hidden="true"
      >
        ✓
      </span>
      <span className="min-w-0">
        <span className="block font-mono text-[11px] uppercase tracking-wider text-verde">
          Fuente citada · datos.gov.co
        </span>
        <span className="block truncate font-medium text-ink">
          {source.dataset}
        </span>
        <span className="block truncate text-sm text-muted">
          {source.entidad}
        </span>
      </span>
      <span
        className="ml-auto self-center text-muted transition-transform group-hover:translate-x-0.5"
        aria-hidden="true"
      >
        ↗
      </span>
    </a>
  );
}
