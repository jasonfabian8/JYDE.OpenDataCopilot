import type { ReactElement } from "react";
import { SectionLabel } from "../../../shared/ui/SectionLabel.tsx";

/** Un paso del flujo de trabajo del copiloto. */
interface Step {
  readonly num: string;
  readonly title: string;
  readonly body: string;
}

const steps: readonly Step[] = [
  {
    num: "01",
    title: "Descubre",
    body: "Busca en el catálogo de datos.gov.co el dataset que mejor responde tu pregunta, usando metadatos amplios y búsqueda semántica.",
  },
  {
    num: "02",
    title: "Consulta",
    body: "Traduce tu pregunta a SoQL y la ejecuta contra la API de Socrata. Sin scraping: solo el canal oficial de datos.",
  },
  {
    num: "03",
    title: "Responde citando",
    body: "Sintetiza una respuesta clara y la entrega con la fuente enlazada. Si los datos no la respaldan, lo declara.",
  },
];

/** Sección que explica el flujo de tres pasos del copiloto. */
export function HowItWorks(): ReactElement {
  return (
    <section id="como-funciona" className="border-b border-line">
      <div className="mx-auto max-w-6xl px-6 py-20 lg:py-28">
        <SectionLabel>Cómo funciona</SectionLabel>
        <h2 className="mt-6 max-w-2xl font-display text-4xl font-medium tracking-tight text-ink sm:text-5xl">
          De la pregunta a la respuesta citada, en tres pasos.
        </h2>

        <ol className="mt-14 grid gap-px overflow-hidden rounded-2xl border border-line bg-line md:grid-cols-3">
          {steps.map((step) => (
            <li key={step.num} className="bg-card p-8">
              <span className="font-mono text-sm text-cobalt">{step.num}</span>
              <h3 className="mt-4 font-display text-2xl font-medium text-ink">
                {step.title}
              </h3>
              <p className="mt-3 leading-relaxed text-ink-soft">{step.body}</p>
            </li>
          ))}
        </ol>
      </div>
    </section>
  );
}
