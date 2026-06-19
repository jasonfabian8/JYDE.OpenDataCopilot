import type { ReactElement } from "react";

/** Props de {@link SectionLabel}. */
interface SectionLabelProps {
  /** Texto corto que rotula la sección. */
  readonly children: string;
}

/** Etiqueta monoespaciada en mayúsculas que encabeza una sección (acento técnico). */
export function SectionLabel({ children }: SectionLabelProps): ReactElement {
  return (
    <span className="inline-flex items-center gap-2 font-mono text-xs uppercase tracking-[0.2em] text-muted">
      <span className="h-1.5 w-1.5 rounded-full bg-cobalt" aria-hidden="true" />
      {children}
    </span>
  );
}
