import type { ReactElement } from "react";

/** Pie de página con atribución de datos y nota del proyecto. */
export function SiteFooter(): ReactElement {
  return (
    <footer className="bg-paper">
      <div className="mx-auto max-w-6xl px-6 py-14">
        <div className="flex flex-col gap-8 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <div className="flex items-center gap-2.5">
              <span
                className="flex h-7 w-7 items-center justify-center rounded-md bg-ink font-mono text-sm font-bold text-paper"
                aria-hidden="true"
              >
                ◆
              </span>
              <span className="font-display text-lg font-medium tracking-tight">
                OpenData <span className="text-cobalt">Copilot</span>
              </span>
            </div>
            <p className="mt-4 max-w-md text-sm leading-relaxed text-muted">
              Datos por{" "}
              <a
                href="https://www.datos.gov.co"
                target="_blank"
                rel="noopener noreferrer"
                className="text-ink-soft underline-offset-4 hover:underline"
              >
                datos.gov.co
              </a>{" "}
              vía la API de Socrata. Las cifras de los ejemplos son ilustrativas.
            </p>
          </div>

          <p className="font-mono text-xs uppercase tracking-wider text-muted">
            Hecho con datos públicos · Colombia
          </p>
        </div>

        <div className="mt-10 border-t border-line pt-6 text-xs text-muted">
          © {new Date().getFullYear()} OpenData Copilot.
        </div>
      </div>
    </footer>
  );
}
