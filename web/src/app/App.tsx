import type { ReactElement } from "react";
import { SiteHeader } from "../features/landing/components/SiteHeader.tsx";
import { Hero } from "../features/landing/components/Hero.tsx";
import { Mission } from "../features/landing/components/Mission.tsx";
import { HowItWorks } from "../features/landing/components/HowItWorks.tsx";
import { Principles } from "../features/landing/components/Principles.tsx";
import { SiteFooter } from "../features/landing/components/SiteFooter.tsx";
import { OperationsPanel } from "../features/operations/components/OperationsPanel.tsx";

/** Raíz de la landing pública (informativa): compone las secciones del sitio.
 *  El chat conversacional vive en la app independiente del Copilot (`/copilot/`). */
export function App(): ReactElement {
  return (
    <>
      <SiteHeader />
      <main>
        <Hero />
        <Mission />
        <HowItWorks />
        <Principles />
        <OperationsPanel />
      </main>
      <SiteFooter />
    </>
  );
}
