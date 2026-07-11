import type { ReactElement } from "react";

/** Marca de OpenData Copilot (badge + wordmark) para la app del Copilot (tema oscuro). */
export function BrandMark(): ReactElement {
  return (
    <span className="flex items-center gap-2.5">
      <span
        className="flex h-7 w-7 items-center justify-center rounded-md bg-sky/15 font-mono text-sm font-bold text-sky"
        aria-hidden="true"
      >
        ◆
      </span>
      <span className="font-display text-lg font-medium tracking-tight text-night-ink">
        OpenData <span className="text-sky">Copilot</span>
      </span>
    </span>
  );
}
