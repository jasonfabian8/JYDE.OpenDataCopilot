import type { ReactElement } from "react";
import { useDemoStore } from "../state/useDemoStore.ts";
import { demoExchanges } from "../model/demoExchanges.ts";
import { SourceCard } from "../../../shared/ui/SourceCard.tsx";

/**
 * Consola interactiva que demuestra el flujo del copiloto: pregunta en lenguaje
 * natural → consulta SoQL → respuesta citada. El estado se maneja con Zustand.
 */
export function DemoConsole(): ReactElement {
  const active = useDemoStore((state) => state.active);
  const activeIndex = useDemoStore((state) => state.activeIndex);
  const select = useDemoStore((state) => state.select);

  return (
    <div className="overflow-hidden rounded-2xl border border-line bg-card shadow-[0_24px_60px_-30px_rgba(11,12,14,0.35)]">
      {/* Barra de ventana estilo terminal/IDE */}
      <div className="flex items-center gap-2 border-b border-line bg-paper-2/70 px-4 py-3">
        <span className="h-3 w-3 rounded-full bg-line" aria-hidden="true" />
        <span className="h-3 w-3 rounded-full bg-line" aria-hidden="true" />
        <span className="h-3 w-3 rounded-full bg-line" aria-hidden="true" />
        <span className="ml-3 font-mono text-xs text-muted">
          opendata-copilot · consulta en vivo
        </span>
        <span className="ml-auto flex items-center gap-1.5 font-mono text-[11px] text-verde">
          <span className="h-1.5 w-1.5 rounded-full bg-verde" aria-hidden="true" />
          <span>conectado a Socrata</span>
        </span>
      </div>

      <div className="space-y-5 p-5 sm:p-6">
        {/* Pregunta del usuario */}
        <div className="flex justify-end">
          <p className="max-w-[85%] rounded-2xl rounded-br-sm bg-ink px-4 py-3 text-sm text-paper">
            {active.question}
          </p>
        </div>

        {/* Paso 1: consulta SoQL generada */}
        <div>
          <p className="mb-2 font-mono text-[11px] uppercase tracking-wider text-muted">
            Consulta generada · SoQL
          </p>
          <pre className="overflow-x-auto rounded-lg border border-line bg-paper-2/60 p-4 font-mono text-[13px] leading-relaxed text-ink-soft">
            <code>{active.soql}</code>
          </pre>
        </div>

        {/* Paso 2: respuesta sintetizada */}
        <div className="flex gap-3">
          <span
            className="mt-1 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-cobalt-soft font-mono text-xs font-semibold text-cobalt"
            aria-hidden="true"
          >
            ◆
          </span>
          <p className="text-[15px] leading-relaxed text-ink">{active.answer}</p>
        </div>

        {/* Paso 3: fuentes citadas */}
        <div className="space-y-2">
          {active.sources.map((source) => (
            <SourceCard key={source.url} source={source} />
          ))}
        </div>
      </div>

      {/* Selector de preguntas de ejemplo */}
      <div className="flex flex-wrap gap-2 border-t border-line bg-paper-2/40 px-5 py-4">
        <span className="mr-1 self-center font-mono text-[11px] uppercase tracking-wider text-muted">
          Prueba:
        </span>
        {demoExchanges.map((exchange, index) => (
          <button
            key={exchange.id}
            type="button"
            onClick={() => select(index)}
            aria-pressed={index === activeIndex}
            className={
              index === activeIndex
                ? "rounded-full bg-cobalt px-3 py-1.5 text-xs font-medium text-white"
                : "rounded-full border border-line px-3 py-1.5 text-xs font-medium text-ink-soft transition-colors hover:border-cobalt/50 hover:text-cobalt"
            }
          >
            {exchange.id.split("-")[0]}
          </button>
        ))}
      </div>
    </div>
  );
}
