import type { ReactElement } from "react";
import { SectionLabel } from "../../../shared/ui/SectionLabel.tsx";

/** Principio de diseño del producto. */
interface Principle {
  readonly title: string;
  readonly body: string;
}

const principles: readonly Principle[] = [
  {
    title: "Citado, siempre",
    body: "Toda respuesta basada en datos enlaza su dataset de origen. La verificación no es opcional.",
  },
  {
    title: "Honesto con los límites",
    body: "Si los datos no soportan la respuesta, lo declaramos. Nunca se inventan cifras.",
  },
  {
    title: "Fuente oficial, sin scraping",
    body: "Solo la API de Socrata (catálogo + SoQL). Datos íntegros, directo del origen público.",
  },
  {
    title: "Costo eficiente por diseño",
    body: "Recursos propios y restricción de costo dura: metadatos amplios y cache selectivo en vez de cargarlo todo.",
  },
];

/** Sección de principios que sostienen la confianza en las respuestas. */
export function Principles(): ReactElement {
  return (
    <section id="principios" className="border-b border-line bg-ink text-paper">
      <div className="mx-auto max-w-6xl px-6 py-20 lg:py-28">
        <SectionLabel>Principios</SectionLabel>
        <h2 className="mt-6 max-w-2xl font-display text-4xl font-medium tracking-tight text-paper sm:text-5xl">
          La confianza es el producto.
        </h2>

        <div className="mt-14 grid gap-px overflow-hidden rounded-2xl border border-white/10 bg-white/10 sm:grid-cols-2">
          {principles.map((principle) => (
            <div key={principle.title} className="bg-ink p-8">
              <h3 className="font-display text-xl font-medium text-paper">
                {principle.title}
              </h3>
              <p className="mt-3 leading-relaxed text-paper/65">
                {principle.body}
              </p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
