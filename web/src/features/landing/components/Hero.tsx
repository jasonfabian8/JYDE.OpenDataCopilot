import type { ReactElement } from "react";
import { SectionLabel } from "../../../shared/ui/SectionLabel.tsx";
import { DemoConsole } from "./DemoConsole.tsx";

/** Sección principal: titular editorial + consola de demostración en vivo. */
export function Hero(): ReactElement {
  return (
    <section
      id="inicio"
      className="relative overflow-hidden border-b border-line"
    >
      {/* Retícula sutil de fondo (acento técnico) */}
      <div className="hero-grid" aria-hidden="true" />

      <div className="relative mx-auto grid max-w-6xl items-center gap-12 px-6 py-20 lg:grid-cols-[1.05fr_1fr] lg:py-28">
        <div>
          <div className="animate-rise" style={{ animationDelay: "0.05s" }}>
            <SectionLabel>Copiloto conversacional · datos.gov.co</SectionLabel>
          </div>

          <h1
            className="animate-rise mt-6 font-display text-5xl font-medium leading-[1.04] tracking-tight text-ink sm:text-6xl lg:text-7xl"
            style={{ animationDelay: "0.12s" }}
          >
            Pregúntale a los{" "}
            <span className="italic text-cobalt">datos abiertos</span> de
            Colombia.
          </h1>

          <p
            className="animate-rise mt-6 max-w-xl text-lg leading-relaxed text-ink-soft"
            style={{ animationDelay: "0.2s" }}
          >
            Decenas de miles de datasets públicos están ahí, pero enterrados tras
            portales y lenguajes de consulta. OpenData Copilot descubre el dataset
            correcto, lo consulta y te responde en lenguaje natural —{" "}
            <strong className="font-semibold text-ink">
              siempre citando la fuente oficial
            </strong>.
          </p>

          <div
            className="animate-rise mt-9 flex flex-wrap items-center gap-4"
            style={{ animationDelay: "0.28s" }}
          >
            <a
              href="/copilot/"
              className="rounded-full bg-cobalt px-6 py-3 text-sm font-medium text-white transition-transform hover:-translate-y-0.5"
            >
              Abrir Copilot
            </a>
            <a
              href="#demo"
              className="text-sm font-medium text-ink-soft underline-offset-4 transition-colors hover:text-ink hover:underline"
            >
              Ver cómo responde →
            </a>
          </div>

          <dl
            className="animate-rise mt-12 flex flex-wrap gap-x-10 gap-y-4"
            style={{ animationDelay: "0.36s" }}
          >
            <div>
              <dt className="font-mono text-xs uppercase tracking-wider text-muted">
                Catálogo
              </dt>
              <dd className="font-display text-2xl text-ink">datos.gov.co</dd>
            </div>
            <div>
              <dt className="font-mono text-xs uppercase tracking-wider text-muted">
                Acceso
              </dt>
              <dd className="font-display text-2xl text-ink">API · SoQL</dd>
            </div>
            <div>
              <dt className="font-mono text-xs uppercase tracking-wider text-muted">
                Cada respuesta
              </dt>
              <dd className="font-display text-2xl text-verde">Citada</dd>
            </div>
          </dl>
        </div>

        <div
          id="demo"
          className="animate-enter scroll-mt-24"
          style={{ animationDelay: "0.18s" }}
        >
          <DemoConsole />
        </div>
      </div>
    </section>
  );
}
