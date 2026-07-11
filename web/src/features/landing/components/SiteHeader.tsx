import type { ReactElement } from "react";

/** Encabezado fijo del sitio público con la marca y navegación de anclas. */
export function SiteHeader(): ReactElement {
  return (
    <header className="sticky top-0 z-50 border-b border-line/70 bg-paper/80 backdrop-blur">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
        <a href="#inicio" className="flex items-center gap-2.5">
          <span
            className="flex h-7 w-7 items-center justify-center rounded-md bg-ink font-mono text-sm font-bold text-paper"
            aria-hidden="true"
          >
            ◆
          </span>
          <span className="font-display text-lg font-medium tracking-tight">
            OpenData <span className="text-cobalt">Copilot</span>
          </span>
        </a>

        <nav className="hidden items-center gap-8 text-sm text-ink-soft md:flex">
          <a href="#mision" className="transition-colors hover:text-ink">
            Misión
          </a>
          <a href="#como-funciona" className="transition-colors hover:text-ink">
            Cómo funciona
          </a>
          <a href="#principios" className="transition-colors hover:text-ink">
            Principios
          </a>
        </nav>

        <a
          href="/copilot/"
          className="rounded-full bg-ink px-4 py-2 text-sm font-medium text-paper transition-transform hover:-translate-y-0.5"
        >
          Abrir Copilot
        </a>
      </div>
    </header>
  );
}
