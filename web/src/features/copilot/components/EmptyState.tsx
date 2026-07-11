import type { ReactElement } from "react";
import { useCopilotStore } from "../state/useCopilotStore.ts";
import { Composer } from "./Composer.tsx";
import { SparkIcon } from "./icons.tsx";

const suggestions: readonly string[] = [
  "¿Qué datasets hay sobre accidentalidad vial?",
  "Cobertura de salud por municipio",
  "Datos de calidad del aire",
  "Deserción escolar en Colombia",
];

/** Pantalla inicial del Copilot: saludo centrado, entrada y sugerencias (estilo ChatGPT/Claude). */
export function EmptyState(): ReactElement {
  const setInput = useCopilotStore((state) => state.setInput);
  const send = useCopilotStore((state) => state.send);

  function ask(text: string): void {
    setInput(text);
    void send();
  }

  return (
    <div className="flex flex-1 items-center overflow-y-auto px-4">
      <div className="mx-auto w-full max-w-2xl py-16">
        <div className="text-center">
          <SparkIcon className="mx-auto mb-4 h-8 w-8 text-amber" />
          <h1 className="font-display text-3xl font-medium tracking-tight text-night-ink sm:text-4xl">
            ¿Qué <span className="italic text-sky">datos abiertos</span> exploramos?
          </h1>
          <p className="mx-auto mt-4 max-w-lg leading-relaxed text-night-soft">
            Pregunta en lenguaje natural. El Copilot busca en datos.gov.co y te recomienda los
            conjuntos de datos relevantes, citando la fuente.
          </p>
        </div>

        <div className="mt-8">
          <Composer variant="centered" />
        </div>

        <div className="mt-6 flex flex-wrap justify-center gap-2">
          {suggestions.map((text) => (
            <button
              key={text}
              type="button"
              onClick={(): void => ask(text)}
              className="rounded-full border border-night-line bg-night-2 px-4 py-2 text-sm text-night-soft transition hover:border-sky/50 hover:text-night-ink"
            >
              {text}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}
