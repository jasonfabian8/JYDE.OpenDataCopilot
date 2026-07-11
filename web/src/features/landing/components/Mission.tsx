import type { ReactElement } from "react";
import { SectionLabel } from "../../../shared/ui/SectionLabel.tsx";

/** Sección de misión: el problema que resuelve y el porqué del producto. */
export function Mission(): ReactElement {
  return (
    <section id="mision" className="border-b border-line bg-paper-2/40">
      <div className="mx-auto max-w-6xl px-6 py-20 lg:py-28">
        <SectionLabel>La misión</SectionLabel>

        <div className="mt-8 grid gap-12 lg:grid-cols-[1.1fr_0.9fr]">
          <p className="font-display text-3xl font-medium leading-[1.25] tracking-tight text-ink sm:text-4xl">
            El dato público ya es de todos. Falta que cualquiera pueda{" "}
            <span className="text-cobalt">obtener una respuesta</span> con solo
            preguntar.
          </p>

          <div className="space-y-5 text-lg leading-relaxed text-ink-soft">
            <p>
              Colombia publica miles de datasets en datos.gov.co, pero usarlos
              exige saber dónde buscar, entender su estructura y escribir
              consultas técnicas. La transparencia existe; el acceso real, no
              tanto.
            </p>
            <p>
              OpenData Copilot cierra esa brecha: convierte una pregunta en
              lenguaje natural en una respuesta verificable, con su fuente al
              lado. Y cuando los datos no alcanzan para responder,{" "}
              <strong className="font-semibold text-ink">lo dice</strong> — no
              inventa cifras.
            </p>
          </div>
        </div>
      </div>
    </section>
  );
}
